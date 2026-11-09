using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

/// <summary>
/// Prepara la base SQLite al levantar el servicio (basado en el ejemplo MiniApi de la cátedra):
/// crea las tablas Carts y CartItems si todavía no existen.
/// No carga datos de ejemplo: los carritos se crean desde Swagger.
/// El archivo de la base (cart.db) se crea solo la primera vez.
/// </summary>
public class DatabaseInitializer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Inicializar()
    {
        string? connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=cart.db";
        }

        // Las columnas se llaman igual que las propiedades de ShoppingCart y CartItem,
        // así Dapper las convierte solo. Guid y fechas se guardan como texto.
        string sqlCarts = @"CREATE TABLE IF NOT EXISTS Carts (
                                UsuarioId          TEXT PRIMARY KEY,
                                FechaActualizacion TEXT NOT NULL
                            )";

        // Un ítem es único por usuario y producto (la misma pareja no se repite).
        string sqlItems = @"CREATE TABLE IF NOT EXISTS CartItems (
                                UsuarioId  TEXT    NOT NULL,
                                ProductoId TEXT    NOT NULL,
                                Cantidad   INTEGER NOT NULL,
                                PRIMARY KEY (UsuarioId, ProductoId)
                            )";

        using (SqliteConnection conexion = new SqliteConnection(connectionString))
        {
            conexion.Execute(sqlCarts);
            conexion.Execute(sqlItems);
        }

        _logger.LogInformation("Base SQLite lista ({ConnectionString}) con las tablas Carts y CartItems",
            connectionString);
    }
}
