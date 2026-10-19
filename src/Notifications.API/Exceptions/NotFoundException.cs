namespace Notifications.API.Exceptions;

/// <summary>
/// Se lanza cuando no existe el recurso pedido (HTTP 404). Ej: NTF-001 o NTF-003.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
