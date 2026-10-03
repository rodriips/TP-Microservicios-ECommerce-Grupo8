using Microsoft.AspNetCore.Diagnostics;
using Products.API.DTOs;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Maneja NotFoundException y devuelve 404.
/// Si la excepción es de otro tipo, devuelve false y ASP.NET prueba el siguiente handler.
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
        if (exception is not NotFoundException ex)
        {
            return false;
        }

        // Error de negocio: se loguea como Warning, con su errorCode
        _logger.LogWarning("[{ErrorCode}] {ErrorMessage}", ex.ErrorCode, ex.Message);

        var error = new ErrorResponse
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
