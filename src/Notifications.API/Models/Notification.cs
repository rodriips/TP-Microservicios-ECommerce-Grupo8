namespace Notifications.API.Models;

/// <summary>
/// Entidad Notification del dominio (Apéndice A de la consigna).
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Email, Push o SMS.</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Pendiente, Enviada o Fallida.</summary>
    public string Estado { get; set; } = string.Empty;

    public DateTime FechaEnvio { get; set; }
}
