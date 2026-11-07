using System.Net;
using Notifications.API.Common;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>
/// Lógica de negocio de notificaciones. Cuando algo sale mal, lanza una excepción
/// con su código del catálogo y el IExceptionHandler correspondiente arma la respuesta.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly NotificationRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationRepository repository,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<NotificationResponse> EnviarAsync(NotificationRequest request)
    {
        // NTF-002: el tipo tiene que ser Email, Push o SMS.
        // Las Data Annotations no pueden validar esto, así que lo hacemos acá.
        string tipo = BuscarTipoValido(request.Tipo);

        // NTF-001: el destinatario tiene que existir en Users.API
        await ValidarQueUsuarioExisteAsync(request.UsuarioId);

        Notification notificacion = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Mensaje = request.Mensaje.Trim(),
            Tipo = tipo,
            Estado = "Pendiente",
            FechaEnvio = DateTime.UtcNow
        };

        // Simulación del envío: no hay proveedor real de Email/Push/SMS (lo permite la consigna).
        // Solo lo registramos en el log y marcamos la notificación como Enviada.
        _logger.LogInformation("Simulando envío por {Tipo} al usuario {UsuarioId}", tipo, request.UsuarioId);
        notificacion.Estado = "Enviada";

        _repository.Agregar(notificacion);
        _logger.LogInformation("Notificación {NotificacionId} registrada como {Estado}", notificacion.Id, notificacion.Estado);

        return ConvertirAResponse(notificacion);
    }

    public List<NotificationResponse> ObtenerPorUsuario(Guid usuarioId)
    {
        List<Notification> notificaciones = _repository.ObtenerPorUsuario(usuarioId);

        // NTF-003: el usuario no tiene notificaciones registradas
        if (notificaciones.Count == 0)
        {
            throw new NotFoundException(ErrorCodes.NTF_003, "No se encontraron notificaciones para el usuario.");
        }

        List<NotificationResponse> resultado = new List<NotificationResponse>();
        foreach (Notification notificacion in notificaciones)
        {
            resultado.Add(ConvertirAResponse(notificacion));
        }

        _logger.LogInformation("Se listaron {Cantidad} notificaciones del usuario {UsuarioId}", resultado.Count, usuarioId);
        return resultado;
    }

    // ------------------------------------------------------------
    // Métodos privados de ayuda
    // ------------------------------------------------------------

    /// <summary>
    /// Devuelve el tipo escrito como en la lista (ej: "email" → "Email").
    /// Si no es uno de los permitidos, lanza NTF-002.
    /// </summary>
    private static string BuscarTipoValido(string tipo)
    {
        // Tipos permitidos (Apéndice A de la consigna)
        string[] tiposValidos = { "Email", "Push", "SMS" };

        foreach (string tipoValido in tiposValidos)
        {
            if (string.Equals(tipoValido, tipo.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return tipoValido;
            }
        }

        throw new ValidationException(
            ErrorCodes.NTF_002,
            $"El tipo '{tipo}' no es válido. Valores permitidos: Email, Push o SMS.");
    }

    /// <summary>
    /// Llama a Users.API (GET /api/users/{id}) para confirmar que el usuario existe.
    /// - 200: existe, seguimos.
    /// - 404: no existe, lanzamos NTF-001.
    /// - Otro código o sin conexión: no podemos confirmarlo, así que es un error
    ///   inesperado (lo atrapa el GlobalExceptionHandler y devuelve NTF-004).
    /// </summary>
    private async Task ValidarQueUsuarioExisteAsync(Guid usuarioId)
    {
        HttpClient client = CrearClienteUsers();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{usuarioId}");

        // Propagamos el Correlation ID a Users.API (sección 5.5 de la consigna).
        // Lo guardó el CorrelationIdMiddleware al inicio del request.
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            string? correlationId = httpContext.Items["CorrelationId"] as string;
            if (correlationId != null)
            {
                request.Headers.Add("X-Correlation-Id", correlationId);
            }
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception("No se pudo conectar con Users.API para validar el usuario.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception("Users.API no respondió a tiempo.", ex);
        }

        // Recomendación de la cátedra: mirar el status antes de usar la respuesta
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(ErrorCodes.NTF_001, "El usuario destinatario no fue encontrado.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Users.API respondió {(int)response.StatusCode} al validar el usuario.");
        }

        _logger.LogInformation("Usuario {UsuarioId} validado contra Users.API", usuarioId);
    }

    /// <summary>
    /// Crea el cliente HTTP para Users.API con su URL y un timeout de 5 segundos.
    /// La URL se lee de appsettings.json ("Servicios:UsersApi");
    /// si no está configurada, se usa el puerto 5002 de la tabla de puertos.
    /// </summary>
    private HttpClient CrearClienteUsers()
    {
        string? urlUsersApi = _configuration["Servicios:UsersApi"];
        if (string.IsNullOrWhiteSpace(urlUsersApi))
        {
            urlUsersApi = "http://localhost:5002";
        }

        HttpClient client = _httpClientFactory.CreateClient("UsersApi");
        client.BaseAddress = new Uri(urlUsersApi);
        client.Timeout = TimeSpan.FromSeconds(5);
        return client;
    }

    private static NotificationResponse ConvertirAResponse(Notification notificacion)
    {
        return new NotificationResponse
        {
            Id = notificacion.Id,
            UsuarioId = notificacion.UsuarioId,
            Mensaje = notificacion.Mensaje,
            Tipo = notificacion.Tipo,
            Estado = notificacion.Estado,
            FechaEnvio = notificacion.FechaEnvio
        };
    }
}
