namespace Cart.API.Exceptions;

/// <summary>
/// Excepción cuando los datos enviados son inválidos (400 Bad Request).
/// </summary>
public class ValidationException : Exception
{
    public string ErrorCode { get; }

    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
