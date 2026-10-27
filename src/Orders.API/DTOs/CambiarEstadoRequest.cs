using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>Body esperado por PUT /api/orders/{id}/status.</summary>
public class CambiarEstadoRequest
{
    [Required]
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Respuesta de PUT /api/orders/{id}/status (igual al ejemplo de la consigna).</summary>
public class CambiarEstadoResponse
{
    public Guid Id { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaActualizacion { get; set; }
}
