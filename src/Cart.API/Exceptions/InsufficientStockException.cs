namespace Cart.API.Exceptions;

/// <summary>
/// Excepción cuando la cantidad solicitada supera el stock disponible del producto (422 Unprocessable Entity - CRT-003).
/// </summary>
public class InsufficientStockException : Exception
{
    public string ErrorCode { get; }

    public InsufficientStockException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
