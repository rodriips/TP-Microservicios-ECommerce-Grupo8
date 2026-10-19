namespace Users.API.Exceptions;

/// <summary>
/// Excepción lanzada cuando no se encuentra un usuario (HTTP 404).
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
