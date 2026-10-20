using Notifications.API.Models;

namespace Notifications.API.Data;

/// <summary>
/// Guarda las notificaciones en una lista en memoria.
/// PROVISORIO: la consigna dice que la persistencia la da la cátedra como librería.
/// Cuando la tengamos, se cambia solo esta clase y el resto del código queda igual.
/// </summary>
public class NotificationRepository
{
    private readonly List<Notification> _notificaciones = new List<Notification>();

    public NotificationRepository()
    {
        // Una notificación de ejemplo para María (el usuario de ejemplo de Users.API),
        // así el GET responde algo desde Swagger sin cargar nada antes.
        _notificaciones.Add(new Notification
        {
            Id = Guid.Parse("11112222-3333-4444-5555-666677778888"),
            UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
            Mensaje = "Su orden fue confirmada.",
            Tipo = "Email",
            Estado = "Enviada",
            FechaEnvio = DateTime.UtcNow
        });
    }

    public List<Notification> ObtenerPorUsuario(Guid usuarioId)
    {
        var resultado = new List<Notification>();
        foreach (var notificacion in _notificaciones)
        {
            if (notificacion.UsuarioId == usuarioId)
            {
                resultado.Add(notificacion);
            }
        }
        return resultado;
    }

    public void Agregar(Notification notificacion)
    {
        _notificaciones.Add(notificacion);
    }
}
