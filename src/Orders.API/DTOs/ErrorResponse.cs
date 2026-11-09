namespace Orders.API.DTOs;

/// <summary>Formato de respuesta de error (estilo ProblemDetails) para todos los endpoints.</summary>
public class ErrorResponse
{
    /// <summary>Enlace RFC que describe el tipo de error HTTP.</summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
    public string Type { get; set; } = string.Empty;

    /// <summary>Nombre corto del error HTTP.</summary>
    /// <example>Not Found</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>Código de estado HTTP.</summary>
    /// <example>404</example>
    public int Status { get; set; }

    /// <summary>Explicación general del error.</summary>
    /// <example>El recurso solicitado no fue encontrado.</example>
    public string Detail { get; set; } = string.Empty;

    /// <summary>Ruta que se llamó.</summary>
    /// <example>/api/orders/7c9e6679-7425-40de-944b-e07fc1f90ae7</example>
    public string? Instance { get; set; }

    /// <summary>Código de error de Orders.API (ORD-001 a ORD-007).</summary>
    /// <example>ORD-001</example>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>Mensaje del error.</summary>
    /// <example>Orden no encontrada.</example>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Id para seguir el pedido entre servicios (header X-Correlation-Id).</summary>
    /// <example>0f8fad5b-d9cb-469f-a165-70867728950e</example>
    public string? CorrelationId { get; set; }
}
