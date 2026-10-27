namespace Orders.API.DTOs;

/// <summary>Respuesta que devuelven GET /api/orders, GET /api/orders/{id} y POST /api/orders.</summary>
public class OrderResponse
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}

public class OrderItemResponse
{
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
