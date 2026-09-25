namespace Users.API.Common;

/// <summary>
/// Catálogo formal de códigos de error para Users.API según la especificación del TP.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Conflicto: El email ya está registrado (HTTP 409).
    /// </summary>
    public const string USR_001 = "USR-001";

    /// <summary>
    /// Bad Request: Los datos del usuario son inválidos (HTTP 400).
    /// </summary>
    public const string USR_002 = "USR-002";

    /// <summary>
    /// Unauthorized: Credenciales incorrectas (HTTP 401).
    /// </summary>
    public const string USR_003 = "USR-003";

    /// <summary>
    /// Forbidden: Usuario bloqueado por demasiados intentos fallidos (HTTP 403).
    /// </summary>
    public const string USR_004 = "USR-004";

    /// <summary>
    /// Forbidden: Usuario bloqueado por detección de fraude (HTTP 403).
    /// </summary>
    public const string USR_005 = "USR-005";

    /// <summary>
    /// Internal Server Error: Error interno al procesar el usuario (HTTP 500).
    /// </summary>
    public const string USR_006 = "USR-006";

    /// <summary>
    /// Not Found: Usuario no encontrado (HTTP 404).
    /// </summary>
    public const string USR_007 = "USR-007";
}
