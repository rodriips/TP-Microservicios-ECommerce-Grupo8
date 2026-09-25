using Microsoft.AspNetCore.Diagnostics;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

/// <summary>
/// Manejador de excepciones para errores de dominio (NotFound, Conflict, Unauthorized, Forbidden, Validation).
/// </summary>
public class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainEx)
        {
            return false;
        }

        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? httpContext.TraceIdentifier;

        logger.LogWarning(domainEx, "Error de dominio capturado: [{ErrorCode}] {Message} (CorrelationId: {CorrelationId})",
            domainEx.ErrorCode, domainEx.Message, correlationId);

        httpContext.Response.StatusCode = domainEx.StatusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var (typeUrl, title) = domainEx.StatusCode switch
        {
            400 => ("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            401 => ("https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized"),
            403 => ("https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden"),
            404 => ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            409 => ("https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict"),
            422 => ("https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity"),
            _   => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Error")
        };

        var problemDetails = new
        {
            type = typeUrl,
            title = title,
            status = domainEx.StatusCode,
            detail = GetDetailForStatus(domainEx.StatusCode),
            instance = httpContext.Request.Path.Value,
            errorCode = domainEx.ErrorCode,
            errorMessage = domainEx.Message,
            correlationId = correlationId
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static string GetDetailForStatus(int statusCode) => statusCode switch
    {
        400 => "Los datos enviados son inválidos.",
        401 => "Las credenciales no son válidas.",
        403 => "El acceso está prohibido.",
        404 => "El recurso solicitado no fue encontrado.",
        409 => "Ya existe un recurso con esos datos o el estado actual no lo permite.",
        _   => "Ha ocurrido un error en la solicitud."
    };
}

/// <summary>
/// Manejador global de excepciones para errores no controlados (HTTP 500 / USR-006).
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? httpContext.TraceIdentifier;

        logger.LogError(exception, "Excepción no controlada en el servidor: {Message} (CorrelationId: {CorrelationId})",
            exception.Message, correlationId);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = 500,
            detail = "Ocurrió un error inesperado al procesar la solicitud.",
            instance = httpContext.Request.Path.Value,
            errorCode = Common.ErrorCodes.USR_006,
            errorMessage = "Error interno al procesar el usuario.",
            correlationId = correlationId
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
