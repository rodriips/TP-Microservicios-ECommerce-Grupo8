using Microsoft.AspNetCore.Diagnostics;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

/// <summary>Maneja BusinessRuleException (ej: ORD-006), respondiendo 409.</summary>
public class BusinessRuleExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BusinessRuleExceptionHandler> _logger;

    public BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex)
            return false;

        _logger.LogWarning("[{ErrorCode}] {ErrorMessage}", ex.ErrorCode, ex.Message);

        var error = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = ex.Detail,
            Instance = context.Request.Path,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = context.Items["CorrelationId"] as string
        };

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
