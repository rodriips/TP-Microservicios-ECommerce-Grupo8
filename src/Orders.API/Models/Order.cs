namespace Orders.API.Models;

/// <summary>
/// Entidad de dominio que representa una orden de compra.
/// </summary>
public class Order
{
    /// <summary>Identificador único de la orden.</summary>
    public Guid Id { get; set; }

    /// <summary>Referencia al usuario que realizó la orden (Users.API).</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Lista de productos incluidos en la orden.</summary>
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>Calculado automáticamente al crear la orden: suma de Cantidad x PrecioUnitario de cada item.</summary>
    public decimal Total { get; set; }

    /// <summary>Pendiente | Confirmada | Enviada | Entregada | Cancelada.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Asignado automáticamente al crear la orden.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Asignado automáticamente cada vez que cambia el estado.</summary>
    public DateTime? FechaActualizacion { get; set; }
}
