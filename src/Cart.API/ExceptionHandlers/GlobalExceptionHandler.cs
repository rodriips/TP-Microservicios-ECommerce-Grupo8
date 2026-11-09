using System.Net;
using Cart.API.Common;
using Cart.API.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace Cart.API.ExceptionHandlers;

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

        _logger.LogError(exception, "Error no controlado procesando la solicitud en {Path}. CorrelationId: {CorrelationId}",
            httpContext.Request.Path, correlationId);

        // Nivel de detalle según el entorno: en Development mostramos el mensaje
        // de la excepción para facilitar la depuración; en Production, un texto genérico.
        string detalle = "Ocurrió un error inesperado al procesar la solicitud.";
        if (_environment.IsDevelopment())
        {
            detalle = exception.Message;
        }

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = detalle,
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
