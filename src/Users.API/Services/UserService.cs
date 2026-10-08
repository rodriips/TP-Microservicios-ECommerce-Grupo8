using Users.API.Common;
using Users.API.Data;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Services;

/// <summary>
/// Lógica de negocio de usuarios: registro, login, hashing seguro con BCrypt y bloqueo por intentos/fraude.
/// </summary>
public class UserService : IUserService
{
    private readonly UserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(UserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public RegisterUserResponse Registrar(RegisterUserRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();

        // USR-001: Validación de email duplicado
        User? existente = _repository.ObtenerPorEmail(email);
        if (existente != null)
        {
            throw new ConflictException(
                ErrorCodes.USR_001,
                $"El email '{request.Email}' ya está registrado.",
                "Ya existe un recurso con esos datos.");
        }

        var usuario = new User
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FechaRegistro = DateTime.UtcNow,
            Activo = true,
            IntentosFallidos = 0,
            BloqueadoPorFraude = false
        };

        _repository.Agregar(usuario);
        _logger.LogInformation("Usuario registrado con éxito: {UserId} ({Email})", usuario.Id, usuario.Email);

        return new RegisterUserResponse
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            FechaRegistro = usuario.FechaRegistro,
            Activo = usuario.Activo
        };
    }

    public LoginResponse IniciarSesion(LoginRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        User? usuario = _repository.ObtenerPorEmail(email);

        if (usuario == null)
        {
            _logger.LogWarning("Login fallido para email no existente: {Email}", request.Email);
            throw new UnauthorizedException(ErrorCodes.USR_003, "Credenciales incorrectas.");
        }

        // USR-005: Bloqueo por fraude
        if (usuario.BloqueadoPorFraude)
        {
            _logger.LogWarning("Login rechazado para usuario bloqueado por fraude: {UserId}", usuario.Id);
            throw new ForbiddenException(ErrorCodes.USR_005, "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");
        }

        // USR-004: Bloqueo previo por intentos fallidos
        if (!usuario.Activo || usuario.IntentosFallidos >= 3)
        {
            _logger.LogWarning("Login rechazado para usuario bloqueado por intentos fallidos: {UserId}", usuario.Id);
            throw new ForbiddenException(ErrorCodes.USR_004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
        }

        // Verificación de contraseña con BCrypt
        bool esPasswordValido = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);

        if (!esPasswordValido)
        {
            usuario.IntentosFallidos++;
            _logger.LogWarning("Contraseña incorrecta para {UserId}. Intentos fallidos: {Attempts}/3", usuario.Id, usuario.IntentosFallidos);

            if (usuario.IntentosFallidos >= 3)
            {
                usuario.Activo = false;
                _logger.LogWarning("Usuario {UserId} ha sido bloqueado tras alcanzar 3 intentos fallidos", usuario.Id);
                throw new ForbiddenException(ErrorCodes.USR_004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
            }

            throw new UnauthorizedException(ErrorCodes.USR_003, "Credenciales incorrectas.");
        }

        // Login exitoso: se resetea el contador de intentos fallidos
        usuario.IntentosFallidos = 0;
        _logger.LogInformation("Login exitoso para usuario {UserId} ({Email})", usuario.Id, usuario.Email);

        return new LoginResponse
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email
        };
    }

    public UserResponse ObtenerPorId(Guid id)
    {
        User? usuario = _repository.ObtenerPorId(id);
        if (usuario == null)
        {
            _logger.LogWarning("Usuario no encontrado con ID: {UserId}", id);
            throw new NotFoundException(ErrorCodes.USR_007, $"Usuario con ID '{id}' no encontrado.");
        }

        return new UserResponse
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            FechaRegistro = usuario.FechaRegistro,
            Activo = usuario.Activo
        };
    }

    public List<UserResponse> ObtenerTodos()
    {
        var resultado = new List<UserResponse>();
        foreach (var usuario in _repository.ObtenerTodos())
        {
            resultado.Add(new UserResponse
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                FechaRegistro = usuario.FechaRegistro,
                Activo = usuario.Activo
            });
        }

        return resultado;
    }
}
