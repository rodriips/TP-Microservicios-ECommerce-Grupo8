namespace Cart.API.Common;

/// <summary>
/// Catálogo de códigos de error de Cart.API (sección 4.4 de la consigna).
/// </summary>
public static class ErrorCodes
{
    /// <summary>404 - Carrito no encontrado (el usuario no tiene carrito activo).</summary>
    public const string CRT_001 = "CRT-001";

    /// <summary>404 - Producto no encontrado (no existe en Products.API o en el carrito).</summary>
    public const string CRT_002 = "CRT-002";

    /// <summary>422 - Stock insuficiente para agregar al carrito.</summary>
    public const string CRT_003 = "CRT-003";

    /// <summary>400 - Cantidad inválida (menor o igual a cero).</summary>
    public const string CRT_004 = "CRT-004";

    /// <summary>500 - Error interno al procesar el carrito.</summary>
    public const string CRT_005 = "CRT-005";
}
