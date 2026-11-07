namespace Cart.API.DTOs;

/// <summary>
/// Un producto dentro del carrito, tal como se devuelve en CartResponse.
/// </summary>
public class CartItemResponse
{
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ProductoId { get; set; }

    /// <example>2</example>
    public int Cantidad { get; set; }
}
