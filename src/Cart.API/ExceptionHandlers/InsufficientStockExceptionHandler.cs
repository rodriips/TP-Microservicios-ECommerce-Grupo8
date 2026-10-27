using System.Net;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Cart.API.ExceptionHandlers;

public class InsufficientStockExceptionHandler : IExceptionHandler
{
    private readonly ILogger<InsufficientStockExceptionHandler> _logger;

    public InsufficientStockExceptionHandler(ILogger<InsufficientStockExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not InsufficientStockException insufficientStockException)
        {
            return false;
        }

        string correlationId = httpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();

        _logger.LogWarning("Stock insuficiente: {ErrorCode} - {ErrorMessage}",
            insufficientStockException.ErrorCode, insufficientStockException.Message);

        var errorResponse = new ErrorResponse
        {
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "Unprocessable Entity",
            Status = (int)HttpStatusCode.UnprocessableEntity,
            Detail = "La solicitud no se pudo procesar debido a reglas de negocio de stock.",
            Instance = httpContext.Request.Path,
            ErrorCode = insufficientStockException.ErrorCode,
            ErrorMessage = insufficientStockException.Message,
            CorrelationId = correlationId
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }
}
