namespace Users.API.Exceptions;

/// <summary>
/// Excepción lanzada cuando los datos del request son inválidos (HTTP 400).
/// </summary>
public class ValidationException : Exception
{
    public string ErrorCode { get; }

    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
