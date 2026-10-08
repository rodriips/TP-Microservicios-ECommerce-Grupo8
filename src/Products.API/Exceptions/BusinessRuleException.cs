namespace Products.API.Exceptions;

/// <summary>
/// Se lanza cuando se viola una regla de negocio (HTTP 409).
/// Ej: PRD-003 (nombre duplicado en la categoría) o PRD-004 (órdenes activas).
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }

    /// <summary>Texto del campo "detail" de la respuesta de error.</summary>
    public string Detail { get; }

    public BusinessRuleException(string errorCode, string message, string detail) : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}
