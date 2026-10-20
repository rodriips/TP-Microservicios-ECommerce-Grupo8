namespace Notifications.API.Common;

/// <summary>
/// Catálogo de códigos de error de Notifications.API (sección 4.5 de la consigna).
/// Se usan constantes para no escribir los códigos "a mano" en cada lugar.
/// </summary>
public static class ErrorCodes
{
    /// <summary>404 - Usuario no encontrado (no existe en Users.API).</summary>
    public const string NTF_001 = "NTF-001";

    /// <summary>400 - Los datos de la notificación son inválidos.</summary>
    public const string NTF_002 = "NTF-002";

    /// <summary>404 - No se encontraron notificaciones para el usuario.</summary>
    public const string NTF_003 = "NTF-003";

    /// <summary>500 - Error interno al procesar la notificación.</summary>
    public const string NTF_004 = "NTF-004";
}
