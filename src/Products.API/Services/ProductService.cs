using Products.API.Common;
using Products.API.Data;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Services;

/// <summary>
/// Lógica de negocio de productos. Cuando algo sale mal, lanza una excepción
/// con su código del catálogo y el IExceptionHandler correspondiente arma la respuesta.
/// </summary>
public class ProductService : IProductService
{
    private readonly ProductRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ProductRepository repository,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProductService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public List<ProductResponse> ObtenerTodos(string? categoria, string? nombre)
    {
        var resultado = new List<ProductResponse>();

        foreach (var producto in _repository.ObtenerTodos())
        {
            // Filtro ?categoria= (igualdad, sin importar mayúsculas)
            if (!string.IsNullOrWhiteSpace(categoria) &&
                !string.Equals(producto.Categoria, categoria.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Filtro ?nombre= (que el nombre contenga el texto, sin importar mayúsculas)
            if (!string.IsNullOrWhiteSpace(nombre) &&
                !producto.Nombre.Contains(nombre.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            resultado.Add(ConvertirAResponse(producto));
        }

        _logger.LogInformation("Se listaron {Cantidad} productos", resultado.Count);
        return resultado;
    }

    public ProductResponse ObtenerPorId(Guid id)
    {
        Product producto = BuscarProductoOFallar(id);
        return ConvertirAResponse(producto);
    }

    public ProductResponse Crear(ProductRequest request)
    {
        string nombre = request.Nombre.Trim();
        string categoria = request.Categoria.Trim();

        // PRD-003: no puede haber dos productos con el mismo nombre en la misma categoría
        foreach (var existente in _repository.ObtenerTodos())
        {
            if (string.Equals(existente.Nombre, nombre, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existente.Categoria, categoria, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(
                    ErrorCodes.PRD_003,
                    $"Ya existe un producto con ese nombre en la categoría '{categoria}'.",
                    "Ya existe un recurso con esos datos.");
            }
        }

        var producto = new Product
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio,
            Stock = request.Stock,
            Categoria = categoria,
            FechaCreacion = DateTime.UtcNow
        };

        _repository.Agregar(producto);
        _logger.LogInformation("Producto creado: {ProductoId} - {Nombre}", producto.Id, producto.Nombre);

        return ConvertirAResponse(producto);
    }

    public ProductResponse Actualizar(Guid id, ProductRequest request)
    {
        Product producto = BuscarProductoOFallar(id);

        // Se actualizan todos los campos editables. Id y FechaCreacion no cambian.
        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = request.Descripcion;
        producto.Precio = request.Precio;
        producto.Stock = request.Stock;
        producto.Categoria = request.Categoria.Trim();

        _logger.LogInformation("Producto actualizado: {ProductoId}", producto.Id);
        return ConvertirAResponse(producto);
    }

    public async Task EliminarAsync(Guid id)
    {
        Product producto = BuscarProductoOFallar(id);

        // PRD-004: no se puede eliminar si hay órdenes Pendiente o Confirmada con este producto
        bool tieneOrdenesActivas = await TieneOrdenesActivasAsync(id);
        if (tieneOrdenesActivas)
        {
            throw new BusinessRuleException(
                ErrorCodes.PRD_004,
                "El producto tiene órdenes activas y no puede eliminarse.",
                "No se puede eliminar el recurso.");
        }

        _repository.Eliminar(producto);
        _logger.LogInformation("Producto eliminado: {ProductoId}", id);
    }

    // ------------------------------------------------------------
    // Métodos privados de ayuda
    // ------------------------------------------------------------

    /// <summary>
    /// Busca el producto; si no existe lanza PRD-001.
    /// La usan GET por id, PUT y DELETE.
    /// </summary>
    private Product BuscarProductoOFallar(Guid id)
    {
        Product? producto = _repository.ObtenerPorId(id);
        if (producto == null)
        {
            throw new NotFoundException(ErrorCodes.PRD_001, "Producto no encontrado.");
        }
        return producto;
    }

    /// <summary>
    /// Pregunta a Orders.API (GET /api/orders) si alguna orden activa usa el producto.
    /// Si Orders.API no responde (por ejemplo, porque todavía no está levantado),
    /// se registra un Warning y se permite eliminar.
    /// </summary>
    private async Task<bool> TieneOrdenesActivasAsync(Guid productoId)
    {
        try
        {
            HttpClient client = _httpClientFactory.CreateClient("OrdersApi");
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders");

            // Propagamos el Correlation ID a Orders.API (sección 5.5 de la consigna)
            string? correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;
            if (correlationId != null)
            {
                request.Headers.Add("X-Correlation-Id", correlationId);
            }

            HttpResponseMessage response = await client.SendAsync(request);

            // Recomendación de la cátedra: mirar el status antes de leer el body
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Orders.API respondió {StatusCode}; no se pudo verificar PRD-004", (int)response.StatusCode);
                return false;
            }

            List<OrderResumen>? ordenes = await response.Content.ReadFromJsonAsync<List<OrderResumen>>();
            if (ordenes == null)
            {
                return false;
            }

            foreach (var orden in ordenes)
            {
                bool estaActiva = orden.Estado == "Pendiente" || orden.Estado == "Confirmada";
                if (!estaActiva)
                {
                    continue;
                }

                foreach (var item in orden.Items)
                {
                    if (item.ProductoId == productoId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("No se pudo conectar con Orders.API ({Mensaje}); no se pudo verificar PRD-004", ex.Message);
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Orders.API no respondió a tiempo; no se pudo verificar PRD-004");
            return false;
        }
    }

    private static ProductResponse ConvertirAResponse(Product producto)
    {
        return new ProductResponse
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            Stock = producto.Stock,
            Categoria = producto.Categoria,
            FechaCreacion = producto.FechaCreacion
        };
    }
}
