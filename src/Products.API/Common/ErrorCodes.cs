namespace Products.API.Common;

/// <summary>
/// Catálogo de códigos de error de Products.API (sección 4.1 de la consigna).
/// Se usan constantes para no escribir los códigos "a mano" en cada lugar.
/// </summary>
public static class ErrorCodes
{
    /// <summary>404 - Producto no encontrado.</summary>
    public const string PRD_001 = "PRD-001";

    /// <summary>400 - Los datos del producto son inválidos.</summary>
    public const string PRD_002 = "PRD-002";

    /// <summary>409 - Ya existe un producto con ese nombre en la categoría.</summary>
    public const string PRD_003 = "PRD-003";

    /// <summary>409 - El producto tiene órdenes activas y no puede eliminarse.</summary>
    public const string PRD_004 = "PRD-004";

    /// <summary>500 - Error interno al procesar el producto.</summary>
    public const string PRD_005 = "PRD-005";
}
