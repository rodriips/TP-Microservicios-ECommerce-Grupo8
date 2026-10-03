using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API.Common;

/// <summary>
/// Arma la respuesta JSON de los endpoints /health.
/// Por defecto ASP.NET devuelve solo el texto "Healthy"; la consigna pide JSON.
/// </summary>
public static class HealthCheckResponseWriter
{
    public static Task EscribirRespuesta(HttpContext context, HealthReport reporte)
    {
        var checks = new List<HealthCheckItem>();
        foreach (var entrada in reporte.Entries)
        {
            checks.Add(new HealthCheckItem
            {
                Nombre = entrada.Key,
                Status = entrada.Value.Status.ToString(),
                Descripcion = entrada.Value.Description
            });
        }

        var respuesta = new HealthCheckRespuesta
        {
            Status = reporte.Status.ToString(), // Healthy, Degraded o Unhealthy
            Servicio = "Products.API",
            Checks = checks
        };

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(respuesta);
    }
}

public class HealthCheckRespuesta
{
    public string Status { get; set; } = string.Empty;
    public string Servicio { get; set; } = string.Empty;
    public List<HealthCheckItem> Checks { get; set; } = new List<HealthCheckItem>();
}

public class HealthCheckItem
{
    public string Nombre { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
