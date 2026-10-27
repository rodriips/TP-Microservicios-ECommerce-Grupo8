namespace Orders.API.Models;

/// <summary>
/// Un producto incluido dentro de una orden, con la cantidad y el precio
/// capturados en el momento en que se creó la orden.
/// </summary>
public class OrderItem
{
    /// <summary>Referencia al producto (Products.API).</summary>
    public Guid ProductoId { get; set; }

    /// <summary>Cantidad solicitada. Debe ser mayor a 0.</summary>
    public int Cantidad { get; set; }

    /// <summary>Precio unitario capturado del producto al momento de crear la orden.</summary>
    public decimal PrecioUnitario { get; set; }
}
