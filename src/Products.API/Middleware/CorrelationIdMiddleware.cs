using Serilog.Context;

namespace Products.API.Middleware;

/// <summary>
/// Se ejecuta en cada request:
/// 1. Toma el header X-Correlation-Id si vino; si no, genera uno nuevo.
/// 2. Lo guarda en HttpContext.Items para que lo usen los handlers y el service.
/// 3. Lo devuelve en el header de la respuesta.
/// 4. Lo agrega a todos los logs de este request (junto con el endpoint).
/// (No maneja errores: eso lo hacen los IExceptionHandler, como pide la consigna.)
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
        string correlationId = context.Request.Headers[NombreHeader].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Items["CorrelationId"] = correlationId;

        // El header se agrega justo antes de mandar la respuesta. Se hace así (y no
        // directamente acá) porque cuando hay un error ASP.NET borra los headers
        // antes de llamar a los IExceptionHandler, y se perdería.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[NombreHeader] = correlationId;
            return Task.CompletedTask;
        });

        string endpoint = context.Request.Method + " " + context.Request.Path;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            _logger.LogInformation("Inicio request {Endpoint}", endpoint);
            await _next(context);
        }
    }
}
