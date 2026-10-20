namespace Orders.API.DTOs;

/// <summary>
/// Forma mínima de la respuesta de GET /api/products/{id} en Products.API,
/// usada acá solo para deserializar y validar stock/precio al crear una orden.
/// </summary>
public class ProductoInfo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
}
