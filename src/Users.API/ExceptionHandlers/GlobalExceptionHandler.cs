using Microsoft.AspNetCore.Diagnostics;
using Users.API.Common;
using Users.API.DTOs;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Red de seguridad para cualquier error no esperado del servidor (HTTP 500 / USR-006).
/// Va al final de todos los handlers registrados.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Id del request: lo guardó el CorrelationIdMiddleware al empezar
        string? correlationId = httpContext.Items["CorrelationId"] as string;

        _logger.LogError(exception, "[{ErrorCode}] Error no controlado: {Mensaje}", ErrorCodes.USR_006, exception.Message);

        // Nivel de detalle según el entorno: en Development mostramos el mensaje
        // de la excepción para facilitar la depuración; en Production, un texto genérico.
        string detalle = "Ocurrió un error inesperado al procesar la solicitud.";
        if (_environment.IsDevelopment())
        {
            detalle = exception.Message;
        }

        ErrorResponse errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = detalle,
            Instance = httpContext.Request.Path.Value,
            ErrorCode = ErrorCodes.USR_006,
            ErrorMessage = "Error interno al procesar el usuario.",
            CorrelationId = correlationId
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}
