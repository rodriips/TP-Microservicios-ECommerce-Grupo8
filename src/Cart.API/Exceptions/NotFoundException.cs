namespace Cart.API.Exceptions;

/// <summary>
/// Excepción cuando un recurso (carrito o producto) no es encontrado (404).
/// </summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
