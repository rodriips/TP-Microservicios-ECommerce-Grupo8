using Dapper;
using Microsoft.Data.Sqlite;
using Users.API.Models;

namespace Users.API.Data;

/// <summary>
/// Prepara la base SQLite al levantar el servicio (basado en el ejemplo MiniApi de la cátedra):
/// 1. Crea la tabla Users si todavía no existe.
/// 2. Si la tabla está vacía, carga los 3 usuarios de ejemplo para probar desde Swagger.
/// El archivo de la base (users.db) se crea solo la primera vez.
/// </summary>
public class DatabaseInitializer
{
    private readonly IConfiguration _configuration;
    private readonly UserRepository _repository;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, UserRepository repository, ILogger<DatabaseInitializer> logger)
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
            connectionString = "Data Source=users.db";
        }

        // Las columnas se llaman igual que las propiedades de User,
        // así Dapper las convierte solo. Guid y fechas se guardan como texto,
        // y los bool como INTEGER (0 = false, 1 = true).
        string sql = @"CREATE TABLE IF NOT EXISTS Users (
                           Id                 TEXT    PRIMARY KEY,
                           Nombre             TEXT    NOT NULL,
                           Apellido           TEXT    NOT NULL,
                           Email              TEXT    NOT NULL UNIQUE,
                           PasswordHash       TEXT    NOT NULL,
                           FechaRegistro      TEXT    NOT NULL,
                           Activo             INTEGER NOT NULL,
                           IntentosFallidos   INTEGER NOT NULL,
                           BloqueadoPorFraude INTEGER NOT NULL
                       )";

        using (SqliteConnection conexion = new SqliteConnection(connectionString))
        {
            conexion.Execute(sql);
        }

        if (_repository.Contar() == 0)
        {
            CargarUsuariosDeEjemplo();
        }

        _logger.LogInformation("Base SQLite lista ({ConnectionString}) con {Cantidad} usuarios",
            connectionString, _repository.Contar());
    }

    /// <summary>
    /// Usuarios de ejemplo. Los Id son fijos para poder usarlos en las pruebas.
    /// Los tres tienen la contraseña "MiPassword123!".
    /// </summary>
    private void CargarUsuariosDeEjemplo()
    {
        // 1. Usuario normal activo de ejemplo
        User maria = new User();
        maria.Id = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        maria.Nombre = "María";
        maria.Apellido = "González";
        maria.Email = "maria@email.com";
        maria.PasswordHash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!");
        maria.FechaRegistro = DateTime.UtcNow;
        maria.Activo = true;
        maria.IntentosFallidos = 0;
        maria.BloqueadoPorFraude = false;
        _repository.Agregar(maria);

        // 2. Usuario bloqueado por fraude para probar USR-005 en la demo
        User carlos = new User();
        carlos.Id = Guid.Parse("b2c3d4e5-0000-0000-0000-222233334444");
        carlos.Nombre = "Carlos";
        carlos.Apellido = "Fraude";
        carlos.Email = "fraude@email.com";
        carlos.PasswordHash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!");
        carlos.FechaRegistro = DateTime.UtcNow.AddSeconds(1);
        carlos.Activo = false;
        carlos.IntentosFallidos = 0;
        carlos.BloqueadoPorFraude = true;
        _repository.Agregar(carlos);

        // 3. Usuario ya bloqueado por intentos fallidos para probar USR-004 directo
        User pedro = new User();
        pedro.Id = Guid.Parse("c3d4e5f6-0000-0000-0000-333344445555");
        pedro.Nombre = "Pedro";
        pedro.Apellido = "Bloqueado";
        pedro.Email = "bloqueado@email.com";
        pedro.PasswordHash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!");
        pedro.FechaRegistro = DateTime.UtcNow.AddSeconds(2);
        pedro.Activo = false;
        pedro.IntentosFallidos = 3;
        pedro.BloqueadoPorFraude = false;
        _repository.Agregar(pedro);

        _logger.LogInformation("Se cargaron 3 usuarios de ejemplo");
    }
}
