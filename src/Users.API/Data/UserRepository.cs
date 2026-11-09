using Dapper;
using Microsoft.Data.Sqlite;
using Users.API.Models;

namespace Users.API.Data;

/// <summary>
/// Guarda los usuarios en la base SQLite (tabla Users) usando Dapper.
/// Cada método abre su propia conexión y la cierra al terminar (el "using").
/// La tabla la crea DatabaseInitializer al levantar el servicio.
/// (Mismo esquema que ProductRepository en Products.API.)
///
/// Todos los SELECT traen TODAS las columnas: si faltara PasswordHash, Activo,
/// IntentosFallidos o BloqueadoPorFraude, el login y el bloqueo no funcionarían.
/// </summary>
public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        // Se lee de appsettings.json ("ConnectionStrings:DefaultConnection").
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=users.db";
        }
        _connectionString = connectionString;
    }

    /// <summary>
    /// Devuelve todos los usuarios, del más viejo al más nuevo.
    /// </summary>
    public List<User> ObtenerTodos()
    {
        string sql = @"SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro,
                              Activo, IntentosFallidos, BloqueadoPorFraude
                       FROM Users
                       ORDER BY FechaRegistro";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            IEnumerable<User> filas = conexion.Query<User>(sql);
            List<User> usuarios = new List<User>(filas);
            return usuarios;
        }
    }

    /// <summary>
    /// Devuelve el usuario con ese Id, o null si no existe.
    /// </summary>
    public User? ObtenerPorId(Guid id)
    {
        string sql = @"SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro,
                              Activo, IntentosFallidos, BloqueadoPorFraude
                       FROM Users
                       WHERE Id = @Id";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("Id", id);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            User? usuario = conexion.QuerySingleOrDefault<User>(sql, parametros);
            return usuario;
        }
    }

    /// <summary>
    /// Devuelve el usuario con ese email (sin importar mayúsculas), o null si no existe.
    /// </summary>
    public User? ObtenerPorEmail(string email)
    {
        string sql = @"SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro,
                              Activo, IntentosFallidos, BloqueadoPorFraude
                       FROM Users
                       WHERE LOWER(Email) = LOWER(@Email)";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("Email", email.Trim());

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            User? usuario = conexion.QuerySingleOrDefault<User>(sql, parametros);
            return usuario;
        }
    }

    /// <summary>
    /// Devuelve cuántos usuarios hay guardados (lo usa DatabaseInitializer).
    /// </summary>
    public int Contar()
    {
        string sql = "SELECT COUNT(*) FROM Users";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            int cantidad = conexion.ExecuteScalar<int>(sql);
            return cantidad;
        }
    }

    /// <summary>
    /// Inserta un usuario nuevo.
    /// Le pasamos el objeto usuario a Dapper: cada @Propiedad del SQL
    /// toma el valor de la propiedad con el mismo nombre (@Nombre ← usuario.Nombre).
    /// </summary>
    public void Agregar(User usuario)
    {
        string sql = @"INSERT INTO Users (Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro,
                                          Activo, IntentosFallidos, BloqueadoPorFraude)
                       VALUES (@Id, @Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro,
                               @Activo, @IntentosFallidos, @BloqueadoPorFraude)";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sql, usuario);
        }
    }

    /// <summary>
    /// Guarda los cambios de un usuario que ya existe (por ejemplo los intentos fallidos del login).
    /// Id y FechaRegistro no se modifican.
    /// </summary>
    public void Actualizar(User usuario)
    {
        string sql = @"UPDATE Users
                       SET Nombre = @Nombre,
                           Apellido = @Apellido,
                           Email = @Email,
                           PasswordHash = @PasswordHash,
                           Activo = @Activo,
                           IntentosFallidos = @IntentosFallidos,
                           BloqueadoPorFraude = @BloqueadoPorFraude
                       WHERE Id = @Id";

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sql, usuario);
        }
    }
}
