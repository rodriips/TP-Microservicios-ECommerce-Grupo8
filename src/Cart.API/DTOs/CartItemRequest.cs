using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>
/// Solicitud para agregar un producto al carrito.
/// </summary>
public class CartItemRequest
{
    /// <summary>
    /// Identificador del producto a agregar.
    /// </summary>
    [Required(ErrorMessage = "El productoId es obligatorio.")]
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad deseada (debe ser mayor a 0).
    /// </summary>
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}
