using Dapper;
using Microsoft.Data.Sqlite;
using Notifications.API.Models;

namespace Notifications.API.Data;

/// <summary>
/// Guarda las notificaciones en la base SQLite (tabla Notifications) usando Dapper.
/// Cada método abre su propia conexión y la cierra al terminar (el "using").
/// La tabla la crea DatabaseInitializer al levantar el servicio.
/// (Mismo esquema que ProductRepository en Products.API.)
/// </summary>
public class NotificationRepository
{
    private readonly string _connectionString;

    public NotificationRepository(IConfiguration configuration)
    {
        // Se lee de appsettings.json ("ConnectionStrings:DefaultConnection").
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=notifications.db";
        }
        _connectionString = connectionString;
    }

    /// <summary>
    /// Devuelve las notificaciones de un usuario, de la más vieja a la más nueva.
    /// </summary>
    public List<Notification> ObtenerPorUsuario(Guid usuarioId)
    {
        string sql = @"SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio
                       FROM Notifications
                       WHERE UsuarioId = @UsuarioId
                       ORDER BY FechaEnvio";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("UsuarioId", usuarioId);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            IEnumerable<Notification> filas = conexion.Query<Notification>(sql, parametros);
            List<Notification> notificaciones = new List<Notification>(filas);
            return notificaciones;
        }
    }

    /// <summary>
    /// Devuelve cuántas notificaciones hay guardadas (lo usa DatabaseInitializer).
    /// </summary>
    public int Contar()
    {
        string sql = "SELECT COUNT(*) FROM Notifications";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            int cantidad = conexion.ExecuteScalar<int>(sql);
            return cantidad;
        }
    }

    /// <summary>
    /// Inserta una notificación nueva.
    /// Cada @Propiedad del SQL toma el valor de la propiedad con el mismo nombre.
    /// </summary>
    public void Agregar(Notification notificacion)
    {
        string sql = @"INSERT INTO Notifications (Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio)
                       VALUES (@Id, @UsuarioId, @Mensaje, @Tipo, @Estado, @FechaEnvio)";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sql, notificacion);
        }
    }
}
