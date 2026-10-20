namespace Notifications.API.DTOs;

/// <summary>
/// Datos de una notificación que devuelve la API.
/// </summary>
public class NotificationResponse
{
    /// <example>11112222-3333-4444-5555-666677778888</example>
    public Guid Id { get; set; }

    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; set; }

    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    public string Mensaje { get; set; } = string.Empty;

    /// <example>Email</example>
    public string Tipo { get; set; } = string.Empty;

    /// <example>Enviada</example>
    public string Estado { get; set; } = string.Empty;

    /// <example>2024-03-10T12:01:00Z</example>
    public DateTime FechaEnvio { get; set; }
}
