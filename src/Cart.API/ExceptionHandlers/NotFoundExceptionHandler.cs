using System.Net;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Cart.API.ExceptionHandlers;

public class NotFoundExceptionHandler : IExceptionHandler
{
    private readonly ILogger<NotFoundExceptionHandler> _logger;

    public NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
        {
            return false;
        }

        // Id del request: lo guardó el CorrelationIdMiddleware al empezar
        string? correlationId = httpContext.Items["CorrelationId"] as string;

        _logger.LogWarning("Recurso no encontrado: {ErrorCode} - {ErrorMessage}",
            notFoundException.ErrorCode, notFoundException.Message);

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = "El recurso solicitado no fue encontrado.",
            Instance = httpContext.Request.Path,
            ErrorCode = notFoundException.ErrorCode,
            ErrorMessage = notFoundException.Message,
            CorrelationId = correlationId
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }
}
