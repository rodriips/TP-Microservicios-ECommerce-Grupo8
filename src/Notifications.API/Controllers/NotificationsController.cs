using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Notifications.API.Common;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

/// <summary>
/// Endpoints de notificaciones.
/// El controller no tiene try/catch: los errores los lanza el service
/// y los responden los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Tags("Notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Registra una notificación y simula su envío.
    /// </summary>
    /// <remarks>
    /// Antes de registrarla consulta a Users.API para confirmar que el destinatario existe (NTF-001).
    /// El tipo tiene que ser Email, Push o SMS (NTF-002). El envío es simulado: queda con estado "Enviada".
    /// </remarks>
    /// <param name="request">Destinatario, mensaje y tipo de la notificación.</param>
    /// <response code="201">Notificación registrada y enviada.</response>
    /// <response code="400">NTF-002 - Los datos de la notificación son inválidos.</response>
    /// <response code="404">NTF-001 - El usuario destinatario no fue encontrado.</response>
    /// <response code="500">NTF-004 - Error interno al procesar la notificación.</response>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationResponse>> Enviar([FromBody] NotificationRequest request)
    {
        ValidarDatos(request);
        NotificationResponse creada = await _notificationService.EnviarAsync(request);
        return CreatedAtAction(nameof(ObtenerPorUsuario), new { userId = creada.UsuarioId }, creada);
    }

    /// <summary>
    /// Lista las notificaciones de un usuario.
    /// </summary>
    /// <param name="userId">Id del usuario (GUID).</param>
    /// <response code="200">Notificaciones del usuario.</response>
    /// <response code="404">NTF-003 - No se encontraron notificaciones para el usuario.</response>
    /// <response code="500">NTF-004 - Error interno al procesar la notificación.</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(List<NotificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<NotificationResponse>> ObtenerPorUsuario(Guid userId)
    {
        List<NotificationResponse> notificaciones = _notificationService.ObtenerPorUsuario(userId);
        return Ok(notificaciones);
    }

    /// <summary>
    /// Revisa las Data Annotations del request (Required, StringLength)
    /// y que venga el usuarioId. Si hay errores, los junta separados por "; " y lanza NTF-002.
    /// </summary>
    private void ValidarDatos(NotificationRequest request)
    {
        List<string> errores = new List<string>();

        foreach (ModelStateEntry campo in ModelState.Values)
        {
            foreach (ModelError error in campo.Errors)
            {
                errores.Add(error.ErrorMessage);
            }
        }

        // Si el usuarioId no viene en el JSON, queda en 00000000-0000-0000-0000-000000000000.
        // (request llega null cuando el JSON está mal escrito; ese error ya está en ModelState.)
        if (request != null && request.UsuarioId == Guid.Empty)
        {
            errores.Add("El usuarioId es obligatorio.");
        }

        if (errores.Count > 0)
        {
            throw new ValidationException(ErrorCodes.NTF_002, string.Join("; ", errores));
        }
    }
}
