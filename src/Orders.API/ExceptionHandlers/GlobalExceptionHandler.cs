using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Common;
using Orders.API.DTOs;

namespace Orders.API.ExceptionHandlers;

/// <summary>
/// Red de seguridad: atrapa cualquier excepción que los handlers anteriores
/// no manejaron. Responde 500 y nunca expone el stack trace en Production.
/// Debe registrarse último en Program.cs.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "[{ErrorCode}] Error no controlado", ErrorCodes.ORD_007);

        string detail = _env.IsDevelopment()
            ? exception.Message
            : "Ocurrió un error inesperado al procesar la solicitud.";

        ErrorResponse error = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = detail,
            Instance = context.Request.Path,
            ErrorCode = ErrorCodes.ORD_007,
            ErrorMessage = "Error interno al procesar la orden.",
            CorrelationId = context.Items["CorrelationId"] as string
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
