namespace Products.API.DTOs;

/// <summary>
/// Formato de todas las respuestas de error (sección 3.1 de la consigna).
/// Los IExceptionHandler son los que la arman.
/// </summary>
public class ErrorResponse
{
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
    public string Type { get; set; } = string.Empty;

    /// <example>Not Found</example>
    public string Title { get; set; } = string.Empty;

    /// <example>404</example>
    public int Status { get; set; }

    /// <example>El recurso solicitado no fue encontrado.</example>
    public string Detail { get; set; } = string.Empty;

    /// <example>/api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string? Instance { get; set; }

    /// <summary>Código del catálogo de errores (PRD-001 a PRD-005).</summary>
    /// <example>PRD-001</example>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>Mensaje del error para el cliente.</summary>
    /// <example>Producto no encontrado.</example>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Identificador del request, para buscarlo en los logs.</summary>
    /// <example>0f8fad5b-d9cb-469f-a165-70867728950e</example>
    public string? CorrelationId { get; set; }
}
