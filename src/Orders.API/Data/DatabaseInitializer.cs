using Dapper;
using Microsoft.Data.Sqlite;

namespace Orders.API.Data;

/// <summary>
/// Crea las tablas de la base SQLite al arrancar la API (si todavía no existen).
/// Se llama una sola vez desde Program.cs.
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

    /// <summary>
    /// Crea las tablas Orders y OrderItems si no existen.
    /// </summary>
    public void Inicializar()
    {
        string? connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=orders.db";
        }

        string sql = @"
            CREATE TABLE IF NOT EXISTS Orders (
                Id                  TEXT PRIMARY KEY,
                UsuarioId           TEXT NOT NULL,
                Total               REAL NOT NULL,
                Estado              TEXT NOT NULL,
                FechaCreacion       TEXT NOT NULL,
                FechaActualizacion  TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS OrderItems (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId         TEXT NOT NULL,
                ProductoId      TEXT NOT NULL,
                Cantidad        INTEGER NOT NULL,
                PrecioUnitario  REAL NOT NULL,
                FOREIGN KEY (OrderId) REFERENCES Orders(Id)
            );";

        using (SqliteConnection conexion = new SqliteConnection(connectionString))
        {
            conexion.Execute(sql);
        }

        _logger.LogInformation("Base SQLite lista ({ConnectionString})", connectionString);
    }
}
