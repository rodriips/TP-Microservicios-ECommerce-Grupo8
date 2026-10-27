using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Transiciones de estado permitidas. Pendiente -> Confirmada -> Enviada -> Entregada,
    // con Cancelada posible desde Pendiente o Confirmada. Entregada y Cancelada son estados finales.
    private static readonly Dictionary<string, string[]> TransicionesValidas = new()
    {
        ["Pendiente"] = new[] { "Confirmada", "Cancelada" },
        ["Confirmada"] = new[] { "Enviada", "Cancelada" },
        ["Enviada"] = new[] { "Entregada" },
        ["Entregada"] = Array.Empty<string>(),
        ["Cancelada"] = Array.Empty<string>()
    };

    private readonly OrderRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderRepository repository,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public List<OrderResponse> ObtenerTodos(Guid? usuarioId)
    {
        var resultado = new List<OrderResponse>();

        foreach (var orden in _repository.ObtenerTodos())
        {
            if (usuarioId.HasValue && orden.UsuarioId != usuarioId.Value)
                continue;

            resultado.Add(ConvertirAResponse(orden));
        }

        _logger.LogInformation("Se encontraron {Cantidad} órdenes", resultado.Count);
        return resultado;
    }

    public OrderResponse ObtenerPorId(Guid id)
    {
        var orden = BuscarOrdenOFallar(id);
        return ConvertirAResponse(orden);
    }

    public async Task<OrderResponse> CrearAsync(CrearOrdenRequest request)
    {
        await ValidarUsuarioExisteAsync(request.UsuarioId);

        var items = new List<OrderItem>();
        decimal total = 0;

        foreach (var itemRequest in request.Items)
        {
            var producto = await ObtenerProductoOFallarAsync(itemRequest.ProductoId);

            if (producto.Stock < itemRequest.Cantidad)
            {
                throw new UnprocessableEntityException(
                    ErrorCodes.ORD_005,
                    $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, solicitado: {itemRequest.Cantidad}.",
                    "No se puede procesar la solicitud.");
            }

            items.Add(new OrderItem
            {
                ProductoId = producto.Id,
                Cantidad = itemRequest.Cantidad,
                PrecioUnitario = producto.Precio
            });

            total += producto.Precio * itemRequest.Cantidad;
        }

        var orden = new Order
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Items = items,
            Total = total,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        _repository.Agregar(orden);
        _logger.LogInformation("Orden {OrdenId} creada para el usuario {UsuarioId}", orden.Id, orden.UsuarioId);

        return ConvertirAResponse(orden);
    }

    public CambiarEstadoResponse CambiarEstado(Guid id, CambiarEstadoRequest request)
    {
        var orden = BuscarOrdenOFallar(id);
        var nuevoEstado = request.Estado;

        if (!TransicionesValidas.ContainsKey(nuevoEstado))
        {
            throw new ValidationException(
                ErrorCodes.ORD_002,
                $"'{nuevoEstado}' no es un estado válido. Los valores permitidos son: Pendiente, Confirmada, Enviada, Entregada, Cancelada.");
        }

        var estadosPermitidos = TransicionesValidas[orden.Estado];

        if (!estadosPermitidos.Contains(nuevoEstado))
        {
            throw new BusinessRuleException(
                ErrorCodes.ORD_006,
                $"Una orden en estado '{orden.Estado}' no puede pasar a '{nuevoEstado}'.",
                "No se puede modificar el estado.");
        }

        orden.Estado = nuevoEstado;
        orden.FechaActualizacion = DateTime.UtcNow;

        _logger.LogInformation("Orden {OrdenId} cambió a estado {Estado}", orden.Id, orden.Estado);

        return new CambiarEstadoResponse
        {
            Id = orden.Id,
            Estado = orden.Estado,
            FechaActualizacion = orden.FechaActualizacion!.Value
        };
    }

    private Order BuscarOrdenOFallar(Guid id)
    {
        var orden = _repository.ObtenerPorId(id);

        if (orden is null)
            throw new NotFoundException(ErrorCodes.ORD_001, "Orden no encontrada.");

        return orden;
    }

    private async Task ValidarUsuarioExisteAsync(Guid usuarioId)
    {
        var cliente = _httpClientFactory.CreateClient("UsersApi");
        AgregarCorrelationId(cliente);

        var respuesta = await cliente.GetAsync($"/api/users/{usuarioId}");

        if (respuesta.IsSuccessStatusCode)
            return;

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.ORD_003, "Usuario no encontrado al crear la orden.");
        }

        // Cualquier otra respuesta inesperada de Users.API se trata como error
        // interno: lo atrapa GlobalExceptionHandler y responde ORD-007.
        throw new InvalidOperationException(
            $"Users.API respondió {(int)respuesta.StatusCode} al validar el usuario {usuarioId}.");
    }

    private async Task<ProductoInfo> ObtenerProductoOFallarAsync(Guid productoId)
    {
        var cliente = _httpClientFactory.CreateClient("ProductsApi");
        AgregarCorrelationId(cliente);

        var respuesta = await cliente.GetAsync($"/api/products/{productoId}");

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.ORD_004, "Producto no encontrado al crear la orden.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Products.API respondió {(int)respuesta.StatusCode} al consultar el producto {productoId}.");
        }

        var producto = await respuesta.Content.ReadFromJsonAsync<ProductoInfo>(JsonOptions);

        if (producto is null)
        {
            throw new InvalidOperationException(
                $"Products.API devolvió un body vacío para el producto {productoId}.");
        }

        return producto;
    }

    private void AgregarCorrelationId(HttpClient cliente)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            cliente.DefaultRequestHeaders.Remove("X-Correlation-Id");
            cliente.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
        }
    }

    private static OrderResponse ConvertirAResponse(Order orden)
    {
        return new OrderResponse
        {
            Id = orden.Id,
            UsuarioId = orden.UsuarioId,
            Items = orden.Items.Select(i => new OrderItemResponse
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario
            }).ToList(),
            Total = orden.Total,
            Estado = orden.Estado,
            FechaCreacion = orden.FechaCreacion
        };
    }
}
