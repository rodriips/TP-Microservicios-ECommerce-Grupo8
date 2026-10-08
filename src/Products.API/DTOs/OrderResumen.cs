namespace Products.API.DTOs;

/// <summary>
/// Lo mínimo que necesitamos leer de una orden de Orders.API para saber
/// si un producto tiene órdenes activas (PRD-004). Los demás campos se ignoran.
/// </summary>
public class OrderResumen
{
    public string Estado { get; set; } = string.Empty;
    public List<OrderItemResumen> Items { get; set; } = new List<OrderItemResumen>();
}

public class OrderItemResumen
{
    public Guid ProductoId { get; set; }
}
