using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>Body esperado por POST /api/orders.</summary>
public class CrearOrdenRequest
{
    [Required]
    public Guid UsuarioId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "La orden debe tener al menos un item.")]
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    [Required]
    public Guid ProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}
