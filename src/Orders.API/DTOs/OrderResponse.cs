namespace Orders.API.DTOs;

/// <summary>Respuesta que devuelven GET /api/orders, GET /api/orders/{id} y POST /api/orders.</summary>
public class OrderResponse
{
    /// <summary>Id de la orden.</summary>
    /// <example>7c9e6679-7425-40de-944b-e07fc1f90ae7</example>
    public Guid Id { get; set; }

    /// <summary>Id del usuario dueño de la orden.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; set; }

    /// <summary>Productos de la orden.</summary>
    public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();

    /// <summary>Suma de Cantidad x PrecioUnitario de todos los items.</summary>
    /// <example>3000.00</example>
    public decimal Total { get; set; }

    /// <summary>Pendiente, Confirmada, Enviada, Entregada o Cancelada.</summary>
    /// <example>Pendiente</example>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Fecha y hora (UTC) en que se creó la orden.</summary>
    /// <example>2026-11-06T21:30:00Z</example>
    public DateTime FechaCreacion { get; set; }
}

/// <summary>Un producto dentro de una orden ya creada.</summary>
public class OrderItemResponse
{
    /// <summary>Id del producto.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ProductoId { get; set; }

    /// <summary>Cantidad pedida.</summary>
    /// <example>2</example>
    public int Cantidad { get; set; }

    /// <summary>Precio del producto al momento de crear la orden.</summary>
    /// <example>1500.00</example>
    public decimal PrecioUnitario { get; set; }
}
