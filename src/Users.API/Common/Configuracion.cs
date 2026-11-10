using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Users.API.Common;

/// <summary>
/// Métodos de configuración que se pasan a Program.cs.
/// Van acá (y no como lambdas en Program.cs) para que cada paso sea un método con nombre.
/// </summary>
public static class Configuracion
{
    /// <summary>
    /// Desactiva la validación automática de ASP.NET: la hacemos nosotros en el controller
    /// (ValidarDatos) para devolver el errorCode USR-002.
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
            Title = "Users.API - ECommerce Microservices",
            Version = "v1",
            Description = "Microservicio de autenticación y gestión de usuarios para la plataforma de E-Commerce."
        });

        string rutaXml = Path.Combine(AppContext.BaseDirectory, "Users.API.xml");
        if (File.Exists(rutaXml))
        {
            options.IncludeXmlComments(rutaXml);
        }

        // Ejemplos de error de cada endpoint (los [EjemploError] de los controllers)
        options.OperationFilter<EjemplosDeErrorFilter>();
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Users.API v1");
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
