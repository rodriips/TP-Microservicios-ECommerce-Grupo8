using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Products.API.Common;

/// <summary>
/// Métodos que configuran ASP.NET desde Program.cs.
/// En Program.cs le pasamos a ASP.NET el NOMBRE de cada método
/// (por ejemplo Configuracion.ConfigurarSwagger) en vez de escribir
/// el código ahí mismo. Así no usamos expresiones lambda.
/// </summary>
public static class Configuracion
{
    /// <summary>
    /// Desactiva la respuesta 400 automática de ASP.NET.
    /// Validamos nosotros en el controller para devolver PRD-002
    /// con el mismo formato de error que el resto.
    /// </summary>
    public static void ConfigurarValidacion(ApiBehaviorOptions options)
    {
        options.SuppressModelStateInvalidFilter = true;
    }

    /// <summary>
    /// Datos generales de Swagger y comentarios XML (los ///) de controllers y DTOs.
    /// </summary>
    public static void ConfigurarSwagger(SwaggerGenOptions options)
    {
        OpenApiInfo info = new OpenApiInfo();
        info.Title = "Products.API - ECommerce Microservices";
        info.Version = "v1";
        info.Description = "Microservicio de catálogo de productos de la plataforma de E-Commerce.";
        options.SwaggerDoc("v1", info);

        string rutaXml = Path.Combine(AppContext.BaseDirectory, "Products.API.xml");
        if (File.Exists(rutaXml))
        {
            options.IncludeXmlComments(rutaXml);
        }

        // Ejemplos de error de cada endpoint (los [EjemploError] de los controllers)
        options.OperationFilter<EjemplosDeErrorFilter>();
    }

    /// <summary>
    /// Formato del archivo swagger.json (igual que en el resto de los servicios).
    /// </summary>
    public static void ConfigurarSwaggerJson(SwaggerOptions options)
    {
        options.SerializeAsV2 = true;
    }

    /// <summary>
    /// Pantalla de Swagger UI en /swagger.
    /// </summary>
    public static void ConfigurarSwaggerUI(SwaggerUIOptions options)
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Products.API v1");
        options.RoutePrefix = "swagger";
    }

    /// <summary>
    /// Filtro para /health/live: no corre ningún check, solo confirma
    /// que el proceso está vivo. Devuelve false para cada check registrado.
    /// </summary>
    public static bool NingunCheck(HealthCheckRegistration registro)
    {
        return false;
    }
}
