using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>Body esperado por PUT /api/orders/{id}/status.</summary>
public class CambiarEstadoRequest
{
    /// <summary>Nuevo estado: Pendiente, Confirmada, Enviada, Entregada o Cancelada (no distingue mayúsculas).</summary>
    /// <example>Confirmada</example>
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Respuesta de PUT /api/orders/{id}/status (igual al ejemplo de la consigna).</summary>
public class CambiarEstadoResponse
{
    /// <summary>Id de la orden modificada.</summary>
    /// <example>7c9e6679-7425-40de-944b-e07fc1f90ae7</example>
    public Guid Id { get; set; }

    /// <summary>Estado actual de la orden.</summary>
    /// <example>Confirmada</example>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Fecha y hora (UTC) de la última modificación.</summary>
    /// <example>2026-11-06T21:30:00Z</example>
    public DateTime FechaActualizacion { get; set; }
}
