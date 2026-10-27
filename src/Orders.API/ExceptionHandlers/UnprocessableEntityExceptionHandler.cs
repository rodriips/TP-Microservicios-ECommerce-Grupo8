using Microsoft.AspNetCore.Diagnostics;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

/// <summary>Maneja UnprocessableEntityException (ORD-005, stock insuficiente), respondiendo 422.</summary>
public class UnprocessableEntityExceptionHandler : IExceptionHandler
{
    private readonly ILogger<UnprocessableEntityExceptionHandler> _logger;

    public UnprocessableEntityExceptionHandler(ILogger<UnprocessableEntityExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not UnprocessableEntityException ex)
            return false;

        _logger.LogWarning("[{ErrorCode}] {ErrorMessage}", ex.ErrorCode, ex.Message);

        var error = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "Unprocessable Entity",
            Status = 422,
            Detail = ex.Detail,
            Instance = context.Request.Path,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = context.Items["CorrelationId"] as string
        };

        context.Response.StatusCode = 422;
        await context.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
