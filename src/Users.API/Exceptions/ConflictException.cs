namespace Users.API.Exceptions;

/// <summary>
/// Excepción lanzada cuando hay un conflicto de negocio, como email duplicado (HTTP 409).
/// </summary>
public class ConflictException : Exception
{
    public string ErrorCode { get; }
    public string Detail { get; }

    public ConflictException(string errorCode, string message, string detail = "Ya existe un recurso con esos datos.") : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}
