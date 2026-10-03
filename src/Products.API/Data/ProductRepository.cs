using Products.API.Models;

namespace Products.API.Data;

/// <summary>
/// Guarda los productos en una lista en memoria.
/// PROVISORIO: la consigna dice que la persistencia la da la cátedra como librería.
/// Cuando la tengamos, se cambia solo esta clase y el resto del código queda igual.
/// </summary>
public class ProductRepository
{
    private readonly List<Product> _productos = new List<Product>();

    public ProductRepository()
    {
        // Productos de ejemplo para probar desde Swagger sin cargar nada.
        // El primero usa el mismo Id que los ejemplos de la consigna.
        _productos.Add(new Product
        {
            Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            Nombre = "Notebook Dell XPS 15",
            Descripcion = "Laptop 15 pulgadas, 32GB RAM",
            Precio = 1500.00m,
            Stock = 10,
            Categoria = "Electrónica",
            FechaCreacion = DateTime.UtcNow
        });
        _productos.Add(new Product
        {
            Id = Guid.NewGuid(),
            Nombre = "Zapatillas Running",
            Descripcion = "Zapatillas livianas para correr",
            Precio = 120.00m,
            Stock = 25,
            Categoria = "Deportes",
            FechaCreacion = DateTime.UtcNow
        });
        _productos.Add(new Product
        {
            Id = Guid.NewGuid(),
            Nombre = "Lámpara de pie",
            Descripcion = "Lámpara LED de 1,60 m",
            Precio = 85.50m,
            Stock = 0,
            Categoria = "Hogar y Deco",
            FechaCreacion = DateTime.UtcNow
        });
    }

    public List<Product> ObtenerTodos()
    {
        return _productos;
    }

    public Product? ObtenerPorId(Guid id)
    {
        foreach (var producto in _productos)
        {
            if (producto.Id == id)
            {
                return producto;
            }
        }
        return null;
    }

    public void Agregar(Product producto)
    {
        _productos.Add(producto);
    }

    public void Eliminar(Product producto)
    {
        _productos.Remove(producto);
    }
}
