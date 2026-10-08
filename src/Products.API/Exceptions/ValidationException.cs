namespace Products.API.Exceptions;

/// <summary>
/// Se lanza cuando los datos enviados son inválidos (HTTP 400). Ej: PRD-002.
/// </summary>
public class ValidationException : Exception
{
    public string ErrorCode { get; }

    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
