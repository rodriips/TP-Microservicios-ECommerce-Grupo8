using Cart.API.DTOs;

namespace Cart.API.Services;

/// <summary>
/// Contrato de negocio del carrito de compras.
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Devuelve todos los ítems del carrito del usuario.
    /// Lanza NotFoundException (CRT-001) si el usuario no tiene carrito.
    /// </summary>
    List<CartItemResponse> ObtenerCarrito(Guid usuarioId);

    /// <summary>
    /// Agrega un producto al carrito.
    /// Consulta Products.API para validar que el producto exista (CRT-002) y tenga stock (CRT-003).
    /// Lanza ValidationException (CRT-004) si la cantidad es menor o igual a 0.
    /// </summary>
    Task AgregarItemAsync(Guid usuarioId, CartItemRequest request);

    /// <summary>
    /// Actualiza la cantidad de un producto ya existente en el carrito.
    /// Lanza NotFoundException (CRT-001 / CRT-002) si el carrito o el ítem no existen.
    /// Consulta stock actualizado antes de aceptar la nueva cantidad (CRT-003).
    /// </summary>
    Task ActualizarItemAsync(Guid usuarioId, Guid productoId, UpdateItemQuantityRequest request);

    /// <summary>
    /// Elimina un producto del carrito.
    /// Lanza NotFoundException (CRT-001 / CRT-002) si el carrito o el ítem no existen.
    /// </summary>
    void EliminarItem(Guid usuarioId, Guid productoId);

    /// <summary>
    /// Vacía por completo el carrito del usuario.
    /// Lanza NotFoundException (CRT-001) si el usuario no tiene carrito.
    /// </summary>
    void VaciarCarrito(Guid usuarioId);
}
