using System.Net;
using Cart.API.Common;
using Cart.API.Data;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Models;

namespace Cart.API.Services;

/// <summary>
/// Implementación del servicio del carrito de compras.
/// Valida contra Products.API (HttpClient) la existencia y el stock del producto
/// antes de agregar o actualizar ítems.
/// Los carritos viven en SQLite: cada vez que se modifica uno, hay que llamar
/// al repositorio para guardarlo (cambiar el objeto en memoria no alcanza).
/// </summary>
public class CartService : ICartService
{
    private readonly CartRepository _cartRepository;
    private readonly HttpClient _productsClient;
    private readonly ILogger<CartService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public CartService(CartRepository cartRepository, IHttpClientFactory httpClientFactory, ILogger<CartService> logger,
        IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _cartRepository = cartRepository;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;

        // Cliente HTTP para llamar a Products.API: URL base desde appsettings.json y timeout de 5 segundos
        string? urlProductos = _configuration["Servicios:ProductsApi"];
        if (urlProductos == null)
        {
            urlProductos = "http://localhost:5001";
        }

        HttpClient client = httpClientFactory.CreateClient("ProductsAPI");
        client.BaseAddress = new Uri(urlProductos);
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        _productsClient = client;
    }

    // ---------------------------------------------------------------
    // GET
    // ---------------------------------------------------------------

    public CartResponse ObtenerCarrito(Guid usuarioId)
    {
        ShoppingCart? carrito = _cartRepository.ObtenerPorUsuario(usuarioId);

        // CRT-001: el usuario no tiene carrito
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        return ConvertirAResponse(carrito);
    }

    // ---------------------------------------------------------------
    // POST – agregar ítem
    // ---------------------------------------------------------------

    public async Task<CartResponse> AgregarItemAsync(Guid usuarioId, CartItemRequest request)
    {
        // CRT-004: si el body no trae productoId, llega como Guid vacío
        if (request.ProductoId == Guid.Empty)
        {
            throw new ValidationException(ErrorCodes.CRT_004,
                "El productoId es obligatorio.");
        }

        if (request.Cantidad <= 0)
        {
            throw new ValidationException(ErrorCodes.CRT_004,
                "La cantidad debe ser mayor a 0.");
        }

        // Validar contra Products.API
        ProductoDto producto = await ObtenerProductoOExceptionAsync(request.ProductoId);

        if (producto.Stock < request.Cantidad)
        {
            throw new InsufficientStockException(ErrorCodes.CRT_003,
                $"Stock insuficiente para el producto '{request.ProductoId}'. " +
                $"Stock disponible: {producto.Stock}, solicitado: {request.Cantidad}.");
        }

        ShoppingCart? carrito = _cartRepository.ObtenerPorUsuario(usuarioId);

        if (carrito == null)
        {
            // El usuario no tiene carrito aún: creamos el objeto, pero todavía NO lo guardamos.
            // Se guarda al final, así si falla alguna validación no queda un carrito vacío en la base.
            carrito = new ShoppingCart();
            carrito.UsuarioId = usuarioId;
        }

        // Si el producto ya está en el carrito, sumamos la cantidad
        CartItem? itemExistente = null;
        foreach (CartItem item in carrito.Items)
        {
            if (item.ProductoId == request.ProductoId)
            {
                itemExistente = item;
                break;
            }
        }

        if (itemExistente != null)
        {
            int totalDeseado = itemExistente.Cantidad + request.Cantidad;
            if (producto.Stock < totalDeseado)
            {
                throw new InsufficientStockException(ErrorCodes.CRT_003,
                    $"Stock insuficiente para el producto '{request.ProductoId}'. " +
                    $"Stock disponible: {producto.Stock}, total solicitado: {totalDeseado}.");
            }
            itemExistente.Cantidad = totalDeseado;
        }
        else
        {
            CartItem itemNuevo = new CartItem();
            itemNuevo.ProductoId = request.ProductoId;
            itemNuevo.Cantidad = request.Cantidad;
            carrito.Items.Add(itemNuevo);
        }

        carrito.FechaActualizacion = DateTime.UtcNow;

        // Recién ahora, con todas las validaciones pasadas, guardamos en la base
        _cartRepository.Guardar(carrito);

        _logger.LogInformation("Ítem {ProductoId} agregado al carrito del usuario {UsuarioId}",
            request.ProductoId, usuarioId);

        // Devolvemos el carrito completo, ya actualizado
        return ConvertirAResponse(carrito);
    }

    // ---------------------------------------------------------------
    // PUT – actualizar cantidad
    // ---------------------------------------------------------------

    public async Task<CartResponse> ActualizarItemAsync(Guid usuarioId, Guid productoId, UpdateItemQuantityRequest request)
    {
        if (request.Cantidad <= 0)
        {
            throw new ValidationException(ErrorCodes.CRT_004,
                "La cantidad debe ser mayor a 0.");
        }

        ShoppingCart? carrito = _cartRepository.ObtenerPorUsuario(usuarioId);
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        CartItem? item = BuscarItem(carrito, productoId);
        if (item == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no está en el carrito.");
        }

        // Verificar stock actual
        ProductoDto producto = await ObtenerProductoOExceptionAsync(productoId);
        if (producto.Stock < request.Cantidad)
        {
            throw new InsufficientStockException(ErrorCodes.CRT_003,
                $"Stock insuficiente para el producto '{productoId}'. " +
                $"Stock disponible: {producto.Stock}, solicitado: {request.Cantidad}.");
        }

        item.Cantidad = request.Cantidad;
        carrito.FechaActualizacion = DateTime.UtcNow;

        // Guardamos el cambio en la base
        _cartRepository.Guardar(carrito);

        _logger.LogInformation("Cantidad del producto {ProductoId} actualizada a {Cantidad} en el carrito de {UsuarioId}",
            productoId, request.Cantidad, usuarioId);

        // Devolvemos el carrito completo, ya actualizado
        return ConvertirAResponse(carrito);
    }

    // ---------------------------------------------------------------
    // DELETE – eliminar ítem
    // ---------------------------------------------------------------

    public void EliminarItem(Guid usuarioId, Guid productoId)
    {
        ShoppingCart? carrito = _cartRepository.ObtenerPorUsuario(usuarioId);
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        CartItem? item = BuscarItem(carrito, productoId);
        if (item == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no está en el carrito.");
        }

        carrito.Items.Remove(item);
        carrito.FechaActualizacion = DateTime.UtcNow;

        // Guardamos el carrito sin ese producto
        _cartRepository.Guardar(carrito);

        _logger.LogInformation("Producto {ProductoId} eliminado del carrito de {UsuarioId}", productoId, usuarioId);
    }

    // ---------------------------------------------------------------
    // DELETE – vaciar carrito
    // ---------------------------------------------------------------

    public void VaciarCarrito(Guid usuarioId)
    {
        ShoppingCart? carrito = _cartRepository.ObtenerPorUsuario(usuarioId);
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        _cartRepository.Eliminar(usuarioId);
        _logger.LogInformation("Carrito del usuario {UsuarioId} vaciado.", usuarioId);
    }

    // ---------------------------------------------------------------
    // Helpers privados
    // ---------------------------------------------------------------

    /// <summary>
    /// Busca un producto dentro del carrito. Devuelve null si no está.
    /// </summary>
    private static CartItem? BuscarItem(ShoppingCart carrito, Guid productoId)
    {
        foreach (CartItem item in carrito.Items)
        {
            if (item.ProductoId == productoId)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Pasa el carrito del modelo (ShoppingCart) al DTO que devuelve la API (CartResponse).
    /// </summary>
    private static CartResponse ConvertirAResponse(ShoppingCart carrito)
    {
        List<CartItemResponse> items = new List<CartItemResponse>();
        foreach (CartItem item in carrito.Items)
        {
            items.Add(new CartItemResponse
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad
            });
        }

        return new CartResponse
        {
            UsuarioId = carrito.UsuarioId,
            Items = items,
            FechaActualizacion = carrito.FechaActualizacion
        };
    }

    /// <summary>
    /// Consulta Products.API. Si el producto no existe lanza CRT-002.
    /// </summary>
    private async Task<ProductoDto> ObtenerProductoOExceptionAsync(Guid productoId)
    {
        // Armamos el request manualmente para poder propagar el X-Correlation-Id
        // al log de Products.API y mantener la trazabilidad distribuida (sección 5.5).
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"api/products/{productoId}");

        // El Correlation ID lo guardó el CorrelationIdMiddleware al inicio del request
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            string? correlationId = httpContext.Items["CorrelationId"] as string;
            if (correlationId != null)
            {
                request.Headers.Add("X-Correlation-Id", correlationId);
            }
        }

        HttpResponseMessage response = await _productsClient.SendAsync(request);

        // Si Products.API dice 404, el producto no existe (CRT-002)
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no existe en el catálogo.");
        }

        // Cualquier otro error de Products.API es inesperado (lo atrapa el GlobalExceptionHandler)
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Products.API respondió {(int)response.StatusCode} al consultar el producto {productoId}.");
        }

        // Leemos el JSON de la respuesta y lo convertimos a ProductoDto
        ProductoDto? producto = await response.Content.ReadFromJsonAsync<ProductoDto>();

        if (producto == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no existe en el catálogo.");
        }

        return producto;
    }

    /// <summary>
    /// DTO mínimo del producto que nos devuelve Products.API.
    /// </summary>
    private class ProductoDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}
