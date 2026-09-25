namespace Users.API.Exceptions;

/// <summary>
/// Excepción base para errores de dominio con código identificador del catálogo.
/// </summary>
public abstract class DomainException(string errorCode, string message, int statusCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// Excepción para recursos no encontrados (HTTP 404).
/// </summary>
public class NotFoundException(string errorCode, string message) : DomainException(errorCode, message, 404);

/// <summary>
/// Excepción para conflictos de negocio o duplicados (HTTP 409).
/// </summary>
public class ConflictException(string errorCode, string message) : DomainException(errorCode, message, 409);

/// <summary>
/// Excepción para credenciales inválidas (HTTP 401).
/// </summary>
public class UnauthorizedException(string errorCode, string message) : DomainException(errorCode, message, 401);

/// <summary>
/// Excepción para accesos prohibidos o cuentas bloqueadas (HTTP 403).
/// </summary>
public class ForbiddenException(string errorCode, string message) : DomainException(errorCode, message, 403);

/// <summary>
/// Excepción para errores de validación de negocio (HTTP 400).
/// </summary>
public class ValidationException(string errorCode, string message) : DomainException(errorCode, message, 400);
