using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>
/// Solicitud para actualizar la cantidad de un producto en el carrito.
/// </summary>
public class UpdateItemQuantityRequest
{
    /// <summary>
    /// Nueva cantidad del producto (debe ser mayor a 0).
    /// </summary>
    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}
