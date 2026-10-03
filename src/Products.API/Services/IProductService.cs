using Products.API.DTOs;

namespace Products.API.Services;

/// <summary>
/// Operaciones de negocio sobre productos. El controller depende de esta interfaz,
/// no de la clase concreta (principio de inversión de dependencias).
/// </summary>
public interface IProductService
{
    List<ProductResponse> ObtenerTodos(string? categoria, string? nombre);
    ProductResponse ObtenerPorId(Guid id);
    ProductResponse Crear(ProductRequest request);
    ProductResponse Actualizar(Guid id, ProductRequest request);
    Task EliminarAsync(Guid id);
}
