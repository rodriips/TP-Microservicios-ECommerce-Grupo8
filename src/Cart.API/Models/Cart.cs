namespace Cart.API.Models;

/// <summary>
/// Modelo de dominio que representa el carrito de compras de un usuario.
/// </summary>
public class ShoppingCart
{
    /// <summary>
    /// Identificador del usuario dueño del carrito.
    /// </summary>
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Lista de productos contenidos en el carrito.
    /// </summary>
    public List<CartItem> Items { get; set; } = new List<CartItem>();

    /// <summary>
    /// Fecha y hora de la última actualización (UTC).
    /// </summary>
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
