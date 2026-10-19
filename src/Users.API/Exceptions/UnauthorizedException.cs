namespace Users.API.Exceptions;

/// <summary>
/// Excepción lanzada cuando las credenciales no son válidas (HTTP 401).
/// </summary>
public class UnauthorizedException : Exception
{
    public string ErrorCode { get; }

    public UnauthorizedException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
