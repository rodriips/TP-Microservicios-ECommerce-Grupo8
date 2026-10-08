using Users.API.Models;

namespace Users.API.Data;

/// <summary>
/// Repositorio en memoria de usuarios.
/// Incluye usuarios precargados para pruebas y demostraciones en vivo.
/// </summary>
public class UserRepository
{
    private readonly List<User> _usuarios = new List<User>();

    public UserRepository()
    {
        // 1. Usuario normal activo de ejemplo (con ID fijo para pruebas)
        _usuarios.Add(new User
        {
            Id = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
            Nombre = "María",
            Apellido = "González",
            Email = "maria@email.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!"),
            FechaRegistro = DateTime.UtcNow,
            Activo = true,
            IntentosFallidos = 0,
            BloqueadoPorFraude = false
        });

        // 2. Usuario bloqueado por fraude para probar USR-005 en la demo
        _usuarios.Add(new User
        {
            Id = Guid.Parse("b2c3d4e5-0000-0000-0000-222233334444"),
            Nombre = "Carlos",
            Apellido = "Fraude",
            Email = "fraude@email.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!"),
            FechaRegistro = DateTime.UtcNow,
            Activo = false,
            IntentosFallidos = 0,
            BloqueadoPorFraude = true
        });

        // 3. Usuario ya bloqueado por intentos fallidos para probar USR-004 directo
        _usuarios.Add(new User
        {
            Id = Guid.Parse("c3d4e5f6-0000-0000-0000-333344445555"),
            Nombre = "Pedro",
            Apellido = "Bloqueado",
            Email = "bloqueado@email.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("MiPassword123!"),
            FechaRegistro = DateTime.UtcNow,
            Activo = false,
            IntentosFallidos = 3,
            BloqueadoPorFraude = false
        });
    }

    public List<User> ObtenerTodos()
    {
        return _usuarios;
    }

    public User? ObtenerPorId(Guid id)
    {
        foreach (var usuario in _usuarios)
        {
            if (usuario.Id == id)
            {
                return usuario;
            }
        }
        return null;
    }

    public User? ObtenerPorEmail(string email)
    {
        foreach (var usuario in _usuarios)
        {
            if (string.Equals(usuario.Email, email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return usuario;
            }
        }
        return null;
    }

    public void Agregar(User usuario)
    {
        _usuarios.Add(usuario);
    }
}
