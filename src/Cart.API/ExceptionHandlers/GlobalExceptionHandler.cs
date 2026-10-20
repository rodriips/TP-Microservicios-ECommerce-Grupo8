using System.Net;
using Cart.API.Common;
using Cart.API.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace Cart.API.ExceptionHandlers;

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
        string correlationId = httpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

        _logger.LogError(exception, "Error no controlado procesando la solicitud en {Path}. CorrelationId: {CorrelationId}",
            httpContext.Request.Path, correlationId);

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "Ocurrió un error inesperado al procesar la solicitud.",
            Instance = httpContext.Request.Path,
            ErrorCode = ErrorCodes.CRT_005,
            ErrorMessage = "Error interno al procesar el carrito.",
            CorrelationId = correlationId
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }
}
