namespace Cart.API.Models;

/// <summary>
/// Modelo de dominio que representa un item dentro del carrito de compras.
/// </summary>
public class CartItem
{
    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad del producto (mayor a 0).
    /// </summary>
    public int Cantidad { get; set; }
}
