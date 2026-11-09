namespace Cart.API.DTOs;

/// <summary>
/// Estructura estándar de respuesta de error según el contrato RFC 7231 (sección 3.1).
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

    /// <example>/api/cart/a1b2c3d4-0000-0000-0000-111122223333</example>
    public string Instance { get; set; } = string.Empty;

    /// <summary>Código del catálogo de errores (CRT-001 a CRT-005).</summary>
    /// <example>CRT-001</example>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>Mensaje del error para el cliente.</summary>
    /// <example>El usuario 'a1b2c3d4-0000-0000-0000-111122223333' no tiene un carrito activo.</example>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Identificador del request, para buscarlo en los logs.</summary>
    /// <example>0f8fad5b-d9cb-469f-a165-70867728950e</example>
    public string? CorrelationId { get; set; }
}
