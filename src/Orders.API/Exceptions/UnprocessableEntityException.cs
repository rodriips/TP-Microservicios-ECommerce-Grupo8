namespace Orders.API.Exceptions;

/// <summary>
/// Se lanza cuando la solicitud está bien formada pero no se puede procesar
/// por una regla de negocio puntual: stock insuficiente (ORD-005).
/// Es un tipo aparte de BusinessRuleException porque responde 422, no 409
/// (Products.API no tenía este caso, así que no existía en su plantilla).
/// La maneja UnprocessableEntityExceptionHandler, que responde 422.
/// </summary>
public class UnprocessableEntityException : Exception
{
    public string ErrorCode { get; }
    public string Detail { get; }

    public UnprocessableEntityException(string errorCode, string message, string detail) : base(message)
    {
        ErrorCode = errorCode;
        Detail = detail;
    }
}
