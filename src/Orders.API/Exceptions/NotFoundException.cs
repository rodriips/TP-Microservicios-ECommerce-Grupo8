namespace Orders.API.Exceptions;

/// <summary>
/// Se lanza cuando un recurso (orden, usuario o producto referenciado) no existe.
/// La maneja NotFoundExceptionHandler, que responde 404.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
