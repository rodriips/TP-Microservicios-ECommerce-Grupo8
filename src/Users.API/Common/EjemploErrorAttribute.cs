namespace Users.API.Common;

/// <summary>
/// Se pone arriba de un endpoint del controller para indicar un error que puede devolver,
/// con los mismos datos que arma el IExceptionHandler. EjemplosDeErrorFilter lo lee
/// y lo muestra como ejemplo en Swagger.
/// Uso: [EjemploError(401, ErrorCodes.USR_003, "Las credenciales no son válidas.", "Credenciales incorrectas.")]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class EjemploErrorAttribute : Attribute
{
    /// <summary>Código HTTP del error (400, 404, 409, 500...).</summary>
    public int Status { get; }

    /// <summary>Código del catálogo (por ejemplo USR-003).</summary>
    public string ErrorCode { get; }

    /// <summary>Texto del campo "detail".</summary>
    public string Detalle { get; }

    /// <summary>Texto del campo "errorMessage".</summary>
    public string ErrorMessage { get; }

    public EjemploErrorAttribute(int status, string errorCode, string detalle, string errorMessage)
    {
        Status = status;
        ErrorCode = errorCode;
        Detalle = detalle;
        ErrorMessage = errorMessage;
    }
}
