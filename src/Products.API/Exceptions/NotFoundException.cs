namespace Products.API.Exceptions;

/// <summary>
/// Se lanza cuando no existe el recurso pedido (HTTP 404). Ej: PRD-001.
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
