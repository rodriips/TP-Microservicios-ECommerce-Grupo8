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

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string? correlationId = httpContext.Items["CorrelationId"] as string
                                ?? httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        _logger.LogError(exception, "Error no controlado: {Mensaje}", exception.Message);

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Ocurrió un error inesperado al procesar la solicitud.",
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
