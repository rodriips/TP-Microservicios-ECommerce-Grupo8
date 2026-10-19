using Notifications.API.DTOs;

namespace Notifications.API.Services;

/// <summary>
/// Operaciones de negocio sobre notificaciones. El controller depende de esta interfaz,
/// no de la clase concreta (principio de inversión de dependencias).
/// </summary>
public interface INotificationService
{
    Task<NotificationResponse> EnviarAsync(NotificationRequest request);
    List<NotificationResponse> ObtenerPorUsuario(Guid usuarioId);
}
