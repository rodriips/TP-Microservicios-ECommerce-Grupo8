using Serilog.Context;

namespace Orders.API.Middleware;

/// <summary>
/// Genera (o propaga) un X-Correlation-Id por request, lo guarda en HttpContext.Items
/// para que los ExceptionHandlers y los logs lo usen, y lo devuelve en la respuesta.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string NombreHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[NombreHeader].ToString();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items["CorrelationId"] = correlationId;

        // Se agrega en OnStarting (no directo) porque ASP.NET limpia los headers
        // antes de invocar a los IExceptionHandler cuando hay un error.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[NombreHeader] = correlationId;
            return Task.CompletedTask;
        });

        var endpoint = $"{context.Request.Method} {context.Request.Path}";

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            _logger.LogInformation("Inicio request {Endpoint}", endpoint);
            await _next(context);
        }
    }
}
