using Dapper;
using Microsoft.Data.Sqlite;
using Notifications.API.Models;

namespace Notifications.API.Data;

/// <summary>
/// Prepara la base SQLite al levantar el servicio (basado en el ejemplo MiniApi de la cátedra):
/// 1. Crea la tabla Notifications si todavía no existe.
/// 2. Si la tabla está vacía, carga una notificación de ejemplo para probar desde Swagger.
/// El archivo de la base (notifications.db) se crea solo la primera vez.
/// </summary>
public class DatabaseInitializer
{
    private readonly IConfiguration _configuration;
    private readonly NotificationRepository _repository;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration configuration, NotificationRepository repository, ILogger<DatabaseInitializer> logger)
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
            connectionString = "Data Source=notifications.db";
        }

        // Las columnas se llaman igual que las propiedades de Notification,
        // así Dapper las convierte solo. Guid y fechas se guardan como texto.
        string sql = @"CREATE TABLE IF NOT EXISTS Notifications (
                           Id         TEXT PRIMARY KEY,
                           UsuarioId  TEXT NOT NULL,
                           Mensaje    TEXT NOT NULL,
                           Tipo       TEXT NOT NULL,
                           Estado     TEXT NOT NULL,
                           FechaEnvio TEXT NOT NULL
                       )";

        using (SqliteConnection conexion = new SqliteConnection(connectionString))
        {
            conexion.Execute(sql);
        }

        if (_repository.Contar() == 0)
        {
            CargarNotificacionDeEjemplo();
        }

        _logger.LogInformation("Base SQLite lista ({ConnectionString}) con {Cantidad} notificaciones",
            connectionString, _repository.Contar());
    }

    /// <summary>
    /// Una notificación de ejemplo para María (el usuario de ejemplo de Users.API),
    /// así el GET responde algo desde Swagger sin cargar nada antes.
    /// </summary>
    private void CargarNotificacionDeEjemplo()
    {
        Notification ejemplo = new Notification();
        ejemplo.Id = Guid.Parse("11112222-3333-4444-5555-666677778888");
        ejemplo.UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        ejemplo.Mensaje = "Su orden fue confirmada.";
        ejemplo.Tipo = "Email";
        ejemplo.Estado = "Enviada";
        ejemplo.FechaEnvio = DateTime.UtcNow;
        _repository.Agregar(ejemplo);

        _logger.LogInformation("Se cargó 1 notificación de ejemplo");
    }
}
