using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cart.API.Common;

/// <summary>
/// Formatea las respuestas de los health checks en JSON estructurado (sección 5.4 de la consigna).
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            servicio = "Cart.API",
            checks = report.Entries.Select(e => new
            {
                nombre = e.Key,
                status = e.Value.Status.ToString(),
                descripcion = e.Value.Description,
                duracion = e.Value.Duration.TotalMilliseconds + " ms"
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
