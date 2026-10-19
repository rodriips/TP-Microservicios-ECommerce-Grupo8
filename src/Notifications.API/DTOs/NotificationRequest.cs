using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

/// <summary>
/// Datos que manda el cliente para enviar una notificación.
/// Id, Estado y FechaEnvio los asigna el servicio.
/// </summary>
public class NotificationRequest
{
    /// <summary>Id del usuario destinatario (obligatorio, tiene que existir en Users.API).</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; set; }

    /// <summary>Texto de la notificación (obligatorio, máx. 500 caracteres).</summary>
    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    [StringLength(500, ErrorMessage = "El mensaje no puede superar los 500 caracteres.")]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Canal de envío: Email, Push o SMS.</summary>
    /// <example>Email</example>
    [Required(ErrorMessage = "El tipo es obligatorio.")]
    public string Tipo { get; set; } = string.Empty;
}
