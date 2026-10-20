using System.Net;
using System.Text.Json;
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
/// </summary>
public class CartService : ICartService
{
    private readonly CartRepository _cartRepository;
    private readonly HttpClient _productsClient;
    private readonly ILogger<CartService> _logger;

    public CartService(CartRepository cartRepository, IHttpClientFactory httpClientFactory, ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _productsClient = httpClientFactory.CreateClient("ProductsAPI");
        _logger = logger;
    }

    // ---------------------------------------------------------------
    // GET
    // ---------------------------------------------------------------

    public List<CartItemResponse> ObtenerCarrito(Guid usuarioId)
    {
        var carrito = _cartRepository.ObtenerPorUsuario(usuarioId);

        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        var response = new List<CartItemResponse>();
        foreach (var item in carrito.Items)
        {
            response.Add(new CartItemResponse
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad
            });
        }
        return response;
    }

    // ---------------------------------------------------------------
    // POST – agregar ítem
    // ---------------------------------------------------------------

    public async Task AgregarItemAsync(Guid usuarioId, CartItemRequest request)
    {
        if (request.Cantidad <= 0)
        {
            throw new ValidationException(ErrorCodes.CRT_004,
                "La cantidad debe ser mayor a 0.");
        }

        // Validar contra Products.API
        var producto = await ObtenerProductoOExceptionAsync(request.ProductoId);

        if (producto.Stock < request.Cantidad)
        {
            throw new InsufficientStockException(ErrorCodes.CRT_003,
                $"Stock insuficiente para el producto '{request.ProductoId}'. " +
                $"Stock disponible: {producto.Stock}, solicitado: {request.Cantidad}.");
        }

        var carrito = _cartRepository.ObtenerPorUsuario(usuarioId);

        if (carrito == null)
        {
            // El usuario no tiene carrito aún: lo creamos
            carrito = new ShoppingCart { UsuarioId = usuarioId };
            _cartRepository.Agregar(carrito);
        }

        // Si el producto ya está en el carrito, sumamos la cantidad
        CartItem? itemExistente = null;
        foreach (var item in carrito.Items)
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
            carrito.Items.Add(new CartItem
            {
                ProductoId = request.ProductoId,
                Cantidad = request.Cantidad
            });
        }

        carrito.FechaActualizacion = DateTime.UtcNow;

        _logger.LogInformation("Ítem {ProductoId} agregado al carrito del usuario {UsuarioId}",
            request.ProductoId, usuarioId);
    }

    // ---------------------------------------------------------------
    // PUT – actualizar cantidad
    // ---------------------------------------------------------------

    public async Task ActualizarItemAsync(Guid usuarioId, Guid productoId, UpdateItemQuantityRequest request)
    {
        if (request.Cantidad <= 0)
        {
            throw new ValidationException(ErrorCodes.CRT_004,
                "La cantidad debe ser mayor a 0.");
        }

        var carrito = _cartRepository.ObtenerPorUsuario(usuarioId);
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        CartItem? item = null;
        foreach (var i in carrito.Items)
        {
            if (i.ProductoId == productoId)
            {
                item = i;
                break;
            }
        }

        if (item == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no está en el carrito.");
        }

        // Verificar stock actual
        var producto = await ObtenerProductoOExceptionAsync(productoId);
        if (producto.Stock < request.Cantidad)
        {
            throw new InsufficientStockException(ErrorCodes.CRT_003,
                $"Stock insuficiente para el producto '{productoId}'. " +
                $"Stock disponible: {producto.Stock}, solicitado: {request.Cantidad}.");
        }

        item.Cantidad = request.Cantidad;
        carrito.FechaActualizacion = DateTime.UtcNow;

        _logger.LogInformation("Cantidad del producto {ProductoId} actualizada a {Cantidad} en el carrito de {UsuarioId}",
            productoId, request.Cantidad, usuarioId);
    }

    // ---------------------------------------------------------------
    // DELETE – eliminar ítem
    // ---------------------------------------------------------------

    public void EliminarItem(Guid usuarioId, Guid productoId)
    {
        var carrito = _cartRepository.ObtenerPorUsuario(usuarioId);
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        CartItem? item = null;
        foreach (var i in carrito.Items)
        {
            if (i.ProductoId == productoId)
            {
                item = i;
                break;
            }
        }

        if (item == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no está en el carrito.");
        }

        carrito.Items.Remove(item);
        carrito.FechaActualizacion = DateTime.UtcNow;

        _logger.LogInformation("Producto {ProductoId} eliminado del carrito de {UsuarioId}", productoId, usuarioId);
    }

    // ---------------------------------------------------------------
    // DELETE – vaciar carrito
    // ---------------------------------------------------------------

    public void VaciarCarrito(Guid usuarioId)
    {
        var carrito = _cartRepository.ObtenerPorUsuario(usuarioId);
        if (carrito == null)
        {
            throw new NotFoundException(ErrorCodes.CRT_001,
                $"El usuario '{usuarioId}' no tiene un carrito activo.");
        }

        _cartRepository.Eliminar(carrito);
        _logger.LogInformation("Carrito del usuario {UsuarioId} vaciado.", usuarioId);
    }

    // ---------------------------------------------------------------
    // Helpers privados
    // ---------------------------------------------------------------

    /// <summary>
    /// Consulta Products.API. Si el producto no existe lanza CRT-002.
    /// </summary>
    private async Task<ProductoDto> ObtenerProductoOExceptionAsync(Guid productoId)
    {
        var response = await _productsClient.GetAsync($"api/products/{productoId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.CRT_002,
                $"El producto '{productoId}' no existe en el catálogo.");
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var producto = JsonSerializer.Deserialize<ProductoDto>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
