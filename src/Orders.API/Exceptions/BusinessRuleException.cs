namespace Orders.API.Exceptions;

/// <summary>
/// Se lanza cuando se viola una regla de negocio (ej: transición de estado inválida, ORD-006).
/// La maneja BusinessRuleExceptionHandler, que responde 409.
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }
    public string Detail { get; }

    public BusinessRuleException(string errorCode, string message, string detail) : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}
