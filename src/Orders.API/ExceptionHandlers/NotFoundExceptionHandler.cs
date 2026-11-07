using Microsoft.AspNetCore.Diagnostics;
using Orders.API.DTOs;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

/// <summary>
/// Maneja NotFoundException. Si la excepción no es de este tipo, ASP.NET
/// prueba el siguiente handler registrado.
/// </summary>
public class NotFoundExceptionHandler : IExceptionHandler
{
    private readonly ILogger<NotFoundExceptionHandler> _logger;

    public NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // Si no es de mi tipo, paso al siguiente handler
        if (exception is not NotFoundException ex)
        {
            return false;
        }

        _logger.LogWarning("[{ErrorCode}] {ErrorMessage}", ex.ErrorCode, ex.Message);

        ErrorResponse error = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "El recurso solicitado no fue encontrado.",
            Instance = context.Request.Path,
            ErrorCode = ex.ErrorCode,
            ErrorMessage = ex.Message,
            CorrelationId = context.Items["CorrelationId"] as string
        };

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
