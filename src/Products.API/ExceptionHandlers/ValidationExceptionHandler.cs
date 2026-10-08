using Microsoft.AspNetCore.Diagnostics;
using Products.API.DTOs;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Maneja ValidationException y devuelve 400.
/// </summary>
public class ValidationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ValidationExceptionHandler> _logger;

    public ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException ex)
        {
            return false;
        }

        _logger.LogWarning("[{ErrorCode}] {ErrorMessage}", ex.ErrorCode, ex.Message);

        var error = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Los datos enviados son inválidos.",
            Instance = context.Request.Path,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = context.Items["CorrelationId"] as string
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
