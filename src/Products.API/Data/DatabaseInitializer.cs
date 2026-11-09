using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Models;

namespace Products.API.Data;

/// <summary>
/// Prepara la base SQLite al levantar el servicio (basado en el ejemplo MiniApi de la cátedra):
/// 1. Crea la tabla Products si todavía no existe.
/// 2. Si la tabla está vacía, carga productos de ejemplo para probar desde Swagger.
/// El archivo de la base (products.db) se crea solo la primera vez.
/// </summary>
public class DatabaseInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ProductRepository _repository;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ProductRepository repository, ILogger<DatabaseInitializer> logger)
    {
        _configuration = configuration;
        _repository = repository;
        _logger = logger;
    }

    public void Inicializar()
    {
        string? connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=products.db";
        }

        // Las columnas se llaman igual que las propiedades de Product,
        // así Dapper las convierte solo. Guid y fechas se guardan como texto.
        // Si hiciera falta otra tabla, se agrega otro CREATE TABLE igual a este.
        string sql = @"CREATE TABLE IF NOT EXISTS Products (
                           Id            TEXT    PRIMARY KEY,
                           Nombre        TEXT    NOT NULL,
                           Descripcion   TEXT,
                           Precio        REAL    NOT NULL,
                           Stock         INTEGER NOT NULL,
                           Categoria     TEXT    NOT NULL,
                           FechaCreacion TEXT    NOT NULL
                       )";

        using (SqliteConnection conexion = new SqliteConnection(connectionString))
        {
            conexion.Execute(sql);
        }

        List<Product> existentes = _repository.ObtenerTodos();
        if (existentes.Count == 0)
        {
            CargarProductosDeEjemplo();
        }

        _logger.LogInformation("Base SQLite lista ({ConnectionString}) con {Cantidad} productos",
            connectionString, _repository.ObtenerTodos().Count);
    }

    /// <summary>
    /// Productos de ejemplo. Los Id son fijos para poder usarlos en las pruebas
    /// (el primero es el mismo Id que usan los ejemplos de la consigna).
    /// </summary>
    private void CargarProductosDeEjemplo()
    {
        Product notebook = new Product();
        notebook.Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        notebook.Nombre = "Notebook Dell XPS 15";
        notebook.Descripcion = "Laptop 15 pulgadas, 32GB RAM";
        notebook.Precio = 1500.00m;
        notebook.Stock = 10;
        notebook.Categoria = "Electrónica";
        notebook.FechaCreacion = DateTime.UtcNow;
        _repository.Agregar(notebook);

        Product zapatillas = new Product();
        zapatillas.Id = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
        zapatillas.Nombre = "Zapatillas Running";
        zapatillas.Descripcion = "Zapatillas livianas para correr";
        zapatillas.Precio = 120.00m;
        zapatillas.Stock = 25;
        zapatillas.Categoria = "Deportes";
        zapatillas.FechaCreacion = DateTime.UtcNow.AddSeconds(1);
        _repository.Agregar(zapatillas);

        Product lampara = new Product();
        lampara.Id = Guid.Parse("9b2d5c1e-3f4a-4b6c-8d7e-0f1a2b3c4d5e");
        lampara.Nombre = "Lámpara de pie";
        lampara.Descripcion = "Lámpara LED de 1,60 m";
        lampara.Precio = 85.50m;
        lampara.Stock = 0;
        lampara.Categoria = "Hogar y Deco";
        lampara.FechaCreacion = DateTime.UtcNow.AddSeconds(2);
        _repository.Agregar(lampara);

        _logger.LogInformation("Se cargaron 3 productos de ejemplo");
    }
}
