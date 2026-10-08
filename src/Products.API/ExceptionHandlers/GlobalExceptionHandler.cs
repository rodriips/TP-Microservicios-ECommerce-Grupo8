using Microsoft.AspNetCore.Diagnostics;
using Products.API.Common;
using Products.API.DTOs;

namespace Products.API.ExceptionHandlers;

/// <summary>
/// Red de seguridad: atrapa cualquier error que no manejaron los handlers anteriores
/// y devuelve 500 con PRD-005. Nunca muestra el stack trace al cliente.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // Error inesperado: se loguea como Error, con la excepción completa (solo en el log)
        _logger.LogError(exception, "[{ErrorCode}] Error inesperado: {Mensaje}", ErrorCodes.PRD_005, exception.Message);

        // Nivel de detalle según el entorno: en Development mostramos el mensaje
        // de la excepción para facilitar la depuración; en Production, un texto genérico.
        string detalle = "Ocurrió un error inesperado al procesar la solicitud.";
        if (_environment.IsDevelopment())
        {
            detalle = exception.Message;
        }

        var error = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = detalle,
            Instance = context.Request.Path,
            ErrorCode = ErrorCodes.PRD_005,
            ErrorMessage = "Error interno al procesar el producto.",
            CorrelationId = context.Items["CorrelationId"] as string
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
