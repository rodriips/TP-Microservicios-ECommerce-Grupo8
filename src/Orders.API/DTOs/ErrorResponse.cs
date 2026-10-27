namespace Orders.API.DTOs;

/// <summary>Formato de respuesta de error (estilo ProblemDetails) para todos los endpoints.</summary>
public class ErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? Instance { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
