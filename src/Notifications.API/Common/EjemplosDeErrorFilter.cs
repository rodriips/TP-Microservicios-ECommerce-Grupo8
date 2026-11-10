using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notifications.API.Common;

/// <summary>
/// Swagger llama a este filtro una vez por cada endpoint.
/// Por cada [EjemploError] que tenga el endpoint, arma el JSON de error con el
/// formato de la consigna (sección 3.1) y lo pone como ejemplo de esa respuesta.
/// Así el 409 del DELETE muestra PRD-004, el 404 muestra PRD-001, etc.
/// Se registra en Configuracion.ConfigurarSwagger.
/// </summary>
public class EjemplosDeErrorFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        object[] atributos = context.MethodInfo.GetCustomAttributes(typeof(EjemploErrorAttribute), false);

        foreach (object atributo in atributos)
        {
            EjemploErrorAttribute ejemplo = (EjemploErrorAttribute)atributo;
            string codigoHttp = ejemplo.Status.ToString();

            // La respuesta tiene que estar declarada con [ProducesResponseType] en el controller
            if (!operation.Responses.ContainsKey(codigoHttp))
            {
                continue;
            }

            OpenApiResponse respuesta = operation.Responses[codigoHttp];
            foreach (OpenApiMediaType contenido in respuesta.Content.Values)
            {
                // Swagger muestra un solo ejemplo por código HTTP. Si un código tiene
                // dos errores posibles, queda el primero (los dos figuran en la descripción).
                if (contenido.Example == null)
                {
                    contenido.Example = ArmarEjemplo(ejemplo, context.ApiDescription.RelativePath);
                }
            }
        }
    }

    /// <summary>
    /// Arma el JSON de error igual al que devuelven los IExceptionHandler.
    /// </summary>
    private static OpenApiObject ArmarEjemplo(EjemploErrorAttribute ejemplo, string ruta)
    {
        OpenApiObject json = new OpenApiObject();
        json.Add("type", new OpenApiString(ObtenerType(ejemplo.Status)));
        json.Add("title", new OpenApiString(ObtenerTitle(ejemplo.Status)));
        json.Add("status", new OpenApiInteger(ejemplo.Status));
        json.Add("detail", new OpenApiString(ejemplo.Detalle));
        json.Add("instance", new OpenApiString("/" + ruta));
        json.Add("errorCode", new OpenApiString(ejemplo.ErrorCode));
        json.Add("errorMessage", new OpenApiString(ejemplo.ErrorMessage));
        json.Add("correlationId", new OpenApiString("0f8fad5b-d9cb-469f-a165-70867728950e"));
        return json;
    }

    private static string ObtenerType(int status)
    {
        if (status == 400)
        {
            return "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        }
        if (status == 401)
        {
            return "https://tools.ietf.org/html/rfc7235#section-3.1";
        }
        if (status == 403)
        {
            return "https://tools.ietf.org/html/rfc7231#section-6.5.3";
        }
        if (status == 404)
        {
            return "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        }
        if (status == 409)
        {
            return "https://tools.ietf.org/html/rfc7231#section-6.5.9";
        }
        if (status == 422)
        {
            return "https://tools.ietf.org/html/rfc4918#section-11.2";
        }
        return "https://tools.ietf.org/html/rfc7231#section-6.6.1";
    }

    private static string ObtenerTitle(int status)
    {
        if (status == 400)
        {
            return "Bad Request";
        }
        if (status == 401)
        {
            return "Unauthorized";
        }
        if (status == 403)
        {
            return "Forbidden";
        }
        if (status == 404)
        {
            return "Not Found";
        }
        if (status == 409)
        {
            return "Conflict";
        }
        if (status == 422)
        {
            return "Unprocessable Entity";
        }
        return "Internal Server Error";
    }
}
