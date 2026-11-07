namespace Cart.API.DTOs;

/// <summary>
/// Carrito completo del usuario (sección 4.4 de la consigna).
/// Lo devuelven GET /api/cart/{userId}, POST .../items y PUT .../items/{productId}.
/// </summary>
public class CartResponse
{
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; set; }

    /// <summary>Productos que hay en el carrito.</summary>
    public List<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();

    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime FechaActualizacion { get; set; }
}
