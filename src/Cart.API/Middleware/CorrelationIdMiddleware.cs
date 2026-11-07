using Serilog.Context;

namespace Cart.API.Middleware;

/// <summary>
/// Se ejecuta en cada request:
/// 1. Toma el header X-Correlation-Id si vino; si no, genera uno nuevo.
/// 2. Lo guarda en HttpContext.Items para que lo usen los handlers y el service.
/// 3. Lo devuelve en el header de la respuesta.
/// 4. Lo agrega a todos los logs de este request (junto con el endpoint).
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

        // El header se agrega justo antes de mandar la respuesta (ver AgregarHeader).
        // Se hace así (y no directo acá) porque cuando hay un error ASP.NET borra los headers
        // antes de llamar a los IExceptionHandler, y se perdería.
        context.Response.OnStarting(AgregarHeader, context);

        string endpoint = context.Request.Method + " " + context.Request.Path;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("Endpoint", endpoint))
        {
            _logger.LogInformation("Inicio request {Endpoint}", endpoint);
            await _next(context);
        }
    }

    /// <summary>
    /// Se ejecuta justo antes de enviar la respuesta: copia el Correlation ID al header.
    /// ASP.NET le pasa como parámetro el objeto que le dimos en OnStarting (el HttpContext).
    /// </summary>
    private static Task AgregarHeader(object estado)
    {
        HttpContext context = (HttpContext)estado;
        string? correlationId = context.Items["CorrelationId"] as string;
        if (correlationId != null)
        {
            context.Response.Headers[NombreHeader] = correlationId;
        }
        return Task.CompletedTask;
    }
}
