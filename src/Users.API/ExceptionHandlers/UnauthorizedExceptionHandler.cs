using Microsoft.AspNetCore.Diagnostics;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Maneja las excepciones UnauthorizedException y responde 401 Unauthorized (ej: USR-003 credenciales incorrectas).
/// </summary>
public class UnauthorizedExceptionHandler : IExceptionHandler
{
    private readonly ILogger<UnauthorizedExceptionHandler> _logger;

    public UnauthorizedExceptionHandler(ILogger<UnauthorizedExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedException ex)
        {
            return false;
        }

        string? correlationId = httpContext.Items["CorrelationId"] as string
                                ?? httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        _logger.LogWarning("[{ErrorCode}] {Mensaje}", ex.ErrorCode, ex.Message);

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Las credenciales no son válidas.",
            Instance = httpContext.Request.Path.Value,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = correlationId
        };

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}
