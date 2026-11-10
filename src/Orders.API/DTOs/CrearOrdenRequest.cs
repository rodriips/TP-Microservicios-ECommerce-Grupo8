using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>Body esperado por POST /api/orders.</summary>
public class CrearOrdenRequest
{
    /// <summary>Id del usuario que hace la orden (tiene que existir en Users.API).</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    [Required(ErrorMessage = "El usuarioId es obligatorio.")]
    public Guid UsuarioId { get; set; }

    /// <summary>Productos de la orden. Tiene que haber al menos uno.</summary>
    [Required(ErrorMessage = "La orden debe tener al menos un item.")]
    [MinLength(1, ErrorMessage = "La orden debe tener al menos un item.")]
    public List<OrderItemRequest> Items { get; set; } = new List<OrderItemRequest>();
}

/// <summary>Un producto con su cantidad dentro de la orden a crear.</summary>
public class OrderItemRequest
{
    /// <summary>Id del producto (tiene que existir en Products.API).</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required(ErrorMessage = "El productoId es obligatorio en cada item.")]
    public Guid ProductoId { get; set; }

    /// <summary>Cantidad pedida. Tiene que ser mayor a 0.</summary>
    /// <example>2</example>
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}
