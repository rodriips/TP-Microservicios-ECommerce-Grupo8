namespace Products.API.Models;

/// <summary>
/// Entidad Product del dominio (Apéndice A de la consigna).
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
}
