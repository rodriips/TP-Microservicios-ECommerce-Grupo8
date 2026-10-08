namespace Users.API.Exceptions;

/// <summary>
/// Excepción lanzada cuando el acceso está prohibido (cuenta bloqueada por intentos o fraude) (HTTP 403).
/// </summary>
public class ForbiddenException : Exception
{
    public string ErrorCode { get; }

    public ForbiddenException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
