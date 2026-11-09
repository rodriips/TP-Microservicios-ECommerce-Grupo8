using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Cart.API.Common;

/// <summary>
/// Métodos de configuración que se pasan a Program.cs.
/// Van acá (y no como lambdas en Program.cs) para que cada paso sea un método con nombre.
/// </summary>
public static class Configuracion
{
    /// <summary>
    /// Desactiva la validación automática de ASP.NET: la hacemos nosotros en el controller
    /// (ValidarDatos) para devolver el errorCode CRT-004.
    /// </summary>
    public static void ConfigurarValidacion(ApiBehaviorOptions options)
    {
        options.SuppressModelStateInvalidFilter = true;
    }

    /// <summary>
    /// Datos del documento de Swagger y lectura de los comentarios XML (summary, response, example).
    /// </summary>
    public static void ConfigurarSwagger(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Cart.API - ECommerce Microservices",
            Version = "v1",
            Description = "Microservicio de carrito de compras. Valida stock en tiempo real consultando Products.API."
        });

        string rutaXml = Path.Combine(AppContext.BaseDirectory, "Cart.API.xml");
        if (File.Exists(rutaXml))
        {
            options.IncludeXmlComments(rutaXml);
        }
    }

    /// <summary>
    /// Genera el swagger.json en formato 2.0.
    /// </summary>
    public static void ConfigurarSwaggerJson(SwaggerOptions options)
    {
        options.SerializeAsV2 = true;
    }

    /// <summary>
    /// Indica a la pantalla de Swagger UI dónde está el swagger.json y en qué ruta se muestra.
    /// </summary>
    public static void ConfigurarSwaggerUI(SwaggerUIOptions options)
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cart.API v1");
        options.RoutePrefix = "swagger";
    }

    /// <summary>
    /// Filtro para /health/live: no ejecuta ningún check (solo confirma que el proceso responde).
    /// </summary>
    public static bool NingunCheck(HealthCheckRegistration registro)
    {
        return false;
    }
}
