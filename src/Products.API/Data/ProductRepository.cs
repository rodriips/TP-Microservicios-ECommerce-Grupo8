using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Models;

namespace Products.API.Data;

/// <summary>
/// Guarda los productos en la base SQLite (tabla Products) usando Dapper.
/// Cada método abre su propia conexión y la cierra al terminar (el "using").
/// La tabla la crea DatabaseInitializer al levantar el servicio.
///
/// Con Dapper escribimos el SQL a mano:
/// - Query  → para SELECT; devuelve los resultados ya convertidos a Product.
/// - Execute → para INSERT, UPDATE y DELETE.
/// - Los @Nombre del SQL se reemplazan por los valores de los parámetros.
/// </summary>
public class ProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration configuration)
    {
        // Se lee de appsettings.json ("ConnectionStrings:DefaultConnection").
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=products.db";
        }
        _connectionString = connectionString;
    }

    /// <summary>
    /// Devuelve todos los productos, del más viejo al más nuevo.
    /// </summary>
    public List<Product> ObtenerTodos()
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion
                       FROM Products
                       ORDER BY FechaCreacion";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            IEnumerable<Product> filas = conexion.Query<Product>(sql);
            List<Product> productos = new List<Product>(filas);
            return productos;
        }
    }

    /// <summary>
    /// Devuelve el producto con ese Id, o null si no existe.
    /// </summary>
    public Product? ObtenerPorId(Guid id)
    {
        string sql = @"SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion
                       FROM Products
                       WHERE Id = @Id";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("Id", id);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            Product? producto = conexion.QuerySingleOrDefault<Product>(sql, parametros);
            return producto;
        }
    }

    /// <summary>
    /// Inserta un producto nuevo.
    /// Le pasamos el objeto producto a Dapper: cada @Propiedad del SQL
    /// toma el valor de la propiedad con el mismo nombre (@Nombre ← producto.Nombre).
    /// </summary>
    public void Agregar(Product producto)
    {
        string sql = @"INSERT INTO Products (Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion)
                       VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sql, producto);
        }
    }

    /// <summary>
    /// Guarda los cambios de un producto que ya existe.
    /// Id y FechaCreacion no se modifican.
    /// </summary>
    public void Actualizar(Product producto)
    {
        string sql = @"UPDATE Products
                       SET Nombre = @Nombre,
                           Descripcion = @Descripcion,
                           Precio = @Precio,
                           Stock = @Stock,
                           Categoria = @Categoria
                       WHERE Id = @Id";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sql, producto);
        }
    }

    public void Eliminar(Guid id)
    {
        string sql = "DELETE FROM Products WHERE Id = @Id";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("Id", id);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sql, parametros);
        }
    }
}
