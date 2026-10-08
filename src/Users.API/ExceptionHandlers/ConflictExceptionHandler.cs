using Microsoft.AspNetCore.Diagnostics;
using Users.API.DTOs;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Maneja las excepciones ConflictException y responde 409 Conflict (ej: USR-001 email duplicado).
/// </summary>
public class ConflictExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ConflictExceptionHandler> _logger;

    public ConflictExceptionHandler(ILogger<ConflictExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ConflictException ex)
        {
            return false;
        }

        string? correlationId = httpContext.Items["CorrelationId"] as string
                                ?? httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        _logger.LogWarning("[{ErrorCode}] {Mensaje}", ex.ErrorCode, ex.Message);

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = ex.Detail,
            Instance = httpContext.Request.Path.Value,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = correlationId
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}
