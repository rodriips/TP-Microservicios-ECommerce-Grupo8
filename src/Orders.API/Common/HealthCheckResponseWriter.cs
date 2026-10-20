using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.API.Common;

/// <summary>
/// Da forma a la respuesta JSON de los endpoints de Health Checks.
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
            Status = reporte.Status.ToString(),
            Servicio = "Orders.API",
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
    public List<HealthCheckItem> Checks { get; set; } = new();
}

public class HealthCheckItem
{
    public string Nombre { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
