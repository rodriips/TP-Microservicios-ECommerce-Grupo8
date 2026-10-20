namespace Orders.API.Exceptions;

/// <summary>
/// Se lanza cuando los datos recibidos en el request no son válidos.
/// La maneja ValidationExceptionHandler, que responde 400.
/// </summary>
public class ValidationException : Exception
{
    public string ErrorCode { get; }

    public ValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
