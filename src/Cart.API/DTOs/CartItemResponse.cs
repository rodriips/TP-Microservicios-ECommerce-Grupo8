namespace Cart.API.DTOs;

/// <summary>
/// Representación de un item en la respuesta del carrito.
/// </summary>
public class CartItemResponse
{
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
}
