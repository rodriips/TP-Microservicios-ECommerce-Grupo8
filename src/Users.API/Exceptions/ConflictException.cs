namespace Users.API.Exceptions;

/// <summary>
/// Excepción lanzada cuando hay un conflicto de negocio, como email duplicado (HTTP 409).
/// </summary>
public class ConflictException : Exception
{
    public string ErrorCode { get; }
    public string Detail { get; }

    // El detalle se pasa siempre desde quien lanza la excepción (sin valor por defecto)
    public ConflictException(string errorCode, string message, string detail) : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}
