using System.Net.Http.Json;
using Orders.API.Common;
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;

namespace Orders.API.Services;

/// <summary>
/// Lógica de negocio de Orders.API. No tiene try/catch para los errores de
/// dominio: los lanza como excepciones propias y los responden los
/// IExceptionHandler. Las fallas de comunicación con Users.API/Products.API
/// (servicio caído, timeout) se dejan propagar como excepción genérica,
/// que atrapa GlobalExceptionHandler como ORD-007.
/// </summary>
public class OrderService : IOrderService
{
    private readonly OrderRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderRepository repository,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    // Devuelve todas las órdenes; si viene usuarioId, solo las de ese usuario.
    public List<OrderResponse> ObtenerTodos(Guid? usuarioId)
    {
        List<OrderResponse> resultado = new List<OrderResponse>();

        foreach (Order orden in _repository.ObtenerTodos())
        {
            // Si pidieron filtrar por usuario y esta orden es de otro, la salteamos
            if (usuarioId.HasValue && orden.UsuarioId != usuarioId.Value)
            {
                continue;
            }

            resultado.Add(ConvertirAResponse(orden));
        }

        _logger.LogInformation("Se encontraron {Cantidad} órdenes", resultado.Count);
        return resultado;
    }

    // Devuelve una orden por Id (si no existe, BuscarOrdenOFallar lanza ORD-001).
    public OrderResponse ObtenerPorId(Guid id)
    {
        Order orden = BuscarOrdenOFallar(id);
        return ConvertirAResponse(orden);
    }

    // Crea una orden: valida el usuario, suma productos repetidos, valida cada producto y su stock.
    public async Task<OrderResponse> CrearAsync(CrearOrdenRequest request)
    {
        // 1) El usuario tiene que existir en Users.API (si no, ORD-003)
        await ValidarUsuarioExisteAsync(request.UsuarioId);

        // 2) Si un producto vino repetido, juntamos sus cantidades en un solo item
        List<OrderItemRequest> itemsSumados = SumarCantidadesRepetidas(request.Items);

        List<OrderItem> items = new List<OrderItem>();
        decimal total = 0;

        // 3) Por cada producto distinto: existe (ORD-004) y alcanza el stock (ORD-005)
        foreach (OrderItemRequest itemRequest in itemsSumados)
        {
            ProductoInfo producto = await ObtenerProductoOFallarAsync(itemRequest.ProductoId);

            if (producto.Stock < itemRequest.Cantidad)
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.ORD_005,
                    $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, solicitado: {itemRequest.Cantidad}.",
                    "No se puede procesar la solicitud.");
            }

            OrderItem item = new OrderItem();
            item.ProductoId = producto.Id;
            item.Cantidad = itemRequest.Cantidad;
            item.PrecioUnitario = producto.Precio;
            items.Add(item);

            total += producto.Precio * itemRequest.Cantidad;
        }

        // 4) Armamos la orden con estado inicial Pendiente y la guardamos
        Order orden = new Order();
        orden.Id = Guid.NewGuid();
        orden.UsuarioId = request.UsuarioId;
        orden.Items = items;
        orden.Total = total;
        orden.Estado = "Pendiente";
        orden.FechaCreacion = DateTime.UtcNow;

        _repository.Agregar(orden);
        _logger.LogInformation("Orden {OrdenId} creada para el usuario {UsuarioId}", orden.Id, orden.UsuarioId);

        return ConvertirAResponse(orden);
    }

    // Cambia el estado de una orden si el estado existe (ORD-002) y la transición es válida (ORD-006).
    public CambiarEstadoResponse CambiarEstado(Guid id, CambiarEstadoRequest request)
    {
        Order orden = BuscarOrdenOFallar(id);

        // Estado que escribió la persona (puede venir en minúsculas)
        string estadoEscrito = request.Estado;

        // El estado tiene que ser uno de los 5 conocidos
        if (!EsEstadoValido(estadoEscrito))
        {
            throw new ValidationException(
                ErrorCodes.ORD_002,
                $"'{estadoEscrito}' no es un estado válido. Los valores permitidos son: Pendiente, Confirmada, Enviada, Entregada, Cancelada.");
        }

        // Lo guardamos siempre con la mayúscula correcta ("enviada" -> "Enviada")
        string nuevoEstado = NormalizarEstado(estadoEscrito);

        // La transición tiene que estar permitida desde el estado actual
        if (!EsTransicionValida(orden.Estado, nuevoEstado))
        {
            throw new BusinessRuleException(
                ErrorCodes.ORD_006,
                $"Una orden en estado '{orden.Estado}' no puede pasar a '{nuevoEstado}'.",
                "No se puede modificar el estado.");
        }

        // Guardamos la fecha en una variable para usar exactamente la misma en la orden y en la respuesta
        DateTime ahora = DateTime.UtcNow;
        orden.Estado = nuevoEstado;
        orden.FechaActualizacion = ahora;
        _repository.ActualizarEstado(orden);

        _logger.LogInformation("Orden {OrdenId} cambió a estado {Estado}", orden.Id, orden.Estado);

        CambiarEstadoResponse respuesta = new CambiarEstadoResponse();
        respuesta.Id = orden.Id;
        respuesta.Estado = orden.Estado;
        respuesta.FechaActualizacion = ahora;
        return respuesta;
    }

    // Devuelve true si el texto es uno de los 5 estados (sin importar mayúsculas).
    private bool EsEstadoValido(string estado)
    {
        return NormalizarEstado(estado) != "";
    }

    // Devuelve el estado con su mayúscula correcta, o "" si no es un estado conocido.
    private string NormalizarEstado(string estado)
    {
        string minusculas = estado.Trim().ToLower();

        if (minusculas == "pendiente")
        {
            return "Pendiente";
        }

        if (minusculas == "confirmada")
        {
            return "Confirmada";
        }

        if (minusculas == "enviada")
        {
            return "Enviada";
        }

        if (minusculas == "entregada")
        {
            return "Entregada";
        }

        if (minusculas == "cancelada")
        {
            return "Cancelada";
        }

        return "";
    }

    // Reglas del flujo: Pendiente -> Confirmada -> Enviada -> Entregada, y Cancelada desde Pendiente o Confirmada.
    private bool EsTransicionValida(string actual, string nuevo)
    {
        if (actual == "Pendiente")
        {
            return nuevo == "Confirmada" || nuevo == "Cancelada";
        }

        if (actual == "Confirmada")
        {
            return nuevo == "Enviada" || nuevo == "Cancelada";
        }

        if (actual == "Enviada")
        {
            return nuevo == "Entregada";
        }

        // Entregada y Cancelada son estados finales: no se puede salir de ahí
        return false;
    }

    // Junta en un solo item los productos repetidos, sumando sus cantidades.
    // Ej: [A x6, A x6] queda [A x12]. No modifica la lista original.
    private List<OrderItemRequest> SumarCantidadesRepetidas(List<OrderItemRequest> items)
    {
        List<OrderItemRequest> resultado = new List<OrderItemRequest>();

        foreach (OrderItemRequest item in items)
        {
            bool yaEstaba = false;

            // Buscamos si ese producto ya está en la lista nueva
            foreach (OrderItemRequest existente in resultado)
            {
                if (existente.ProductoId == item.ProductoId)
                {
                    existente.Cantidad = existente.Cantidad + item.Cantidad;
                    yaEstaba = true;
                }
            }

            // Si no estaba, lo agregamos (como copia, para no tocar el request original)
            if (!yaEstaba)
            {
                OrderItemRequest copia = new OrderItemRequest();
                copia.ProductoId = item.ProductoId;
                copia.Cantidad = item.Cantidad;
                resultado.Add(copia);
            }
        }

        return resultado;
    }

    // Busca una orden por Id; si no existe lanza NotFoundException (ORD-001 -> 404).
    private Order BuscarOrdenOFallar(Guid id)
    {
        Order? orden = _repository.ObtenerPorId(id);

        if (orden == null)
        {
            throw new NotFoundException(ErrorCodes.ORD_001, "Orden no encontrada.");
        }

        return orden;
    }

    // Le pregunta a Users.API si el usuario existe. Si responde 404 -> ORD-003.
    private async Task ValidarUsuarioExisteAsync(Guid usuarioId)
    {
        HttpClient cliente = CrearCliente("UsersApi", "http://localhost:5002");
        AgregarCorrelationId(cliente);

        HttpResponseMessage respuesta = await cliente.GetAsync($"/api/users/{usuarioId}");

        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.ORD_003, "Usuario no encontrado al crear la orden.");
        }

        // Cualquier otra respuesta inesperada de Users.API se trata como error
        // interno: lo atrapa GlobalExceptionHandler y responde ORD-007.
        throw new InvalidOperationException(
            $"Users.API respondió {(int)respuesta.StatusCode} al validar el usuario {usuarioId}.");
    }

    // Le pide a Products.API los datos del producto (precio y stock). Si responde 404 -> ORD-004.
    private async Task<ProductoInfo> ObtenerProductoOFallarAsync(Guid productoId)
    {
        HttpClient cliente = CrearCliente("ProductsApi", "http://localhost:5001");
        AgregarCorrelationId(cliente);

        HttpResponseMessage respuesta = await cliente.GetAsync($"/api/products/{productoId}");

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.ORD_004, "Producto no encontrado al crear la orden.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Products.API respondió {(int)respuesta.StatusCode} al consultar el producto {productoId}.");
        }

        // Convierte el JSON de Products.API en un objeto ProductoInfo
        ProductoInfo? producto = await respuesta.Content.ReadFromJsonAsync<ProductoInfo>();

        if (producto == null)
        {
            throw new InvalidOperationException(
                $"Products.API devolvió un body vacío para el producto {productoId}.");
        }

        return producto;
    }

    // Crea el cliente HTTP hacia otro servicio. La URL se lee de appsettings.json
    // (sección "Servicios"); si no está, se usa la URL por defecto. Espera máximo 5 segundos.
    private HttpClient CrearCliente(string nombre, string urlPorDefecto)
    {
        HttpClient cliente = _httpClientFactory.CreateClient(nombre);

        string? url = _configuration["Servicios:" + nombre];
        if (url == null)
        {
            url = urlPorDefecto;
        }

        cliente.BaseAddress = new Uri(url);
        cliente.Timeout = TimeSpan.FromSeconds(5);
        return cliente;
    }

    // Copia el X-Correlation-Id del pedido actual al pedido que le hacemos a otro servicio.
    private void AgregarCorrelationId(HttpClient cliente)
    {
        HttpContext? contexto = _httpContextAccessor.HttpContext;

        if (contexto != null)
        {
            string? correlationId = contexto.Items["CorrelationId"] as string;

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                cliente.DefaultRequestHeaders.Remove("X-Correlation-Id");
                cliente.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
            }
        }
    }

    // Convierte una Order (modelo interno) en OrderResponse (lo que se devuelve en el JSON).
    private static OrderResponse ConvertirAResponse(Order orden)
    {
        List<OrderItemResponse> items = new List<OrderItemResponse>();

        foreach (OrderItem item in orden.Items)
        {
            OrderItemResponse itemResponse = new OrderItemResponse();
            itemResponse.ProductoId = item.ProductoId;
            itemResponse.Cantidad = item.Cantidad;
            itemResponse.PrecioUnitario = item.PrecioUnitario;
            items.Add(itemResponse);
        }

        OrderResponse respuesta = new OrderResponse();
        respuesta.Id = orden.Id;
        respuesta.UsuarioId = orden.UsuarioId;
        respuesta.Items = items;
        respuesta.Total = orden.Total;
        respuesta.Estado = orden.Estado;
        respuesta.FechaCreacion = orden.FechaCreacion;
        return respuesta;
    }
}
