using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Orders.API.Common;

/// <summary>
/// Métodos de configuración que se le pasan a Program.cs. Están acá, como métodos
/// comunes, para no usar lambdas en Program.cs.
/// </summary>
public static class Configuracion
{
    // Apaga el 400 automático de [ApiController]: la validación la hace ValidarDatos()
    // en el controller, para devolver nuestro formato de error (ORD-002).
    public static void ConfigurarValidacion(ApiBehaviorOptions options)
    {
        options.SuppressModelStateInvalidFilter = true;
    }

    // Título, versión y descripción del Swagger, más los comentarios XML (/// y <example>).
    public static void ConfigurarSwagger(SwaggerGenOptions options)
    {
        OpenApiInfo info = new OpenApiInfo();
        info.Title = "Orders.API - ECommerce Microservices";
        info.Version = "v1";
        info.Description = "Gestión de órdenes de compra. Valida usuario y stock contra Users.API y Products.API.";
        options.SwaggerDoc("v1", info);

        // Si existe el archivo XML generado al compilar, Swagger lo usa
        string archivoXml = Path.Combine(AppContext.BaseDirectory, "Orders.API.xml");
        if (File.Exists(archivoXml))
        {
            options.IncludeXmlComments(archivoXml);
        }
    }

    // Genera el swagger.json en formato 2.0
    public static void ConfigurarSwaggerJson(SwaggerOptions options)
    {
        options.SerializeAsV2 = true;
    }

    // Pantalla de Swagger: de dónde lee el JSON y en qué ruta se muestra
    public static void ConfigurarSwaggerUI(SwaggerUIOptions options)
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders.API v1");
        options.RoutePrefix = "swagger";
    }

    // Para /health/live: no corre ningún chequeo, solo confirma que la app responde
    public static bool NingunCheck(HealthCheckRegistration registro)
    {
        return false;
    }
}
