using System.Collections.Concurrent;
using BCrypt.Net;
using Users.API.Common;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Services;

/// <summary>
/// Implementación en memoria del servicio de usuarios con persistencia thread-safe.
/// </summary>
public class UserService : IUserService
{
    private static readonly ConcurrentDictionary<Guid, User> _users = new();
    private static readonly ConcurrentDictionary<string, Guid> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (_emailIndex.ContainsKey(normalizedEmail))
        {
            _logger.LogWarning("Intento de registro con email duplicado: {Email}", request.Email);
            throw new ConflictException(ErrorCodes.USR_001, $"El email '{request.Email}' ya está registrado.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FechaRegistro = DateTime.UtcNow,
            Activo = true,
            IntentosFallidos = 0,
            BloqueadoPorFraude = false
        };

        if (!_emailIndex.TryAdd(normalizedEmail, user.Id))
        {
            throw new ConflictException(ErrorCodes.USR_001, $"El email '{request.Email}' ya está registrado.");
        }

        _users[user.Id] = user;

        _logger.LogInformation("Usuario registrado con éxito: {UserId} ({Email})", user.Id, user.Email);

        var response = new RegisterUserResponse
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email,
            FechaRegistro = user.FechaRegistro,
            Activo = user.Activo
        };

        return Task.FromResult(response);
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!_emailIndex.TryGetValue(normalizedEmail, out var userId) || !_users.TryGetValue(userId, out var user))
        {
            _logger.LogWarning("Login fallido para email no existente: {Email}", request.Email);
            throw new UnauthorizedException(ErrorCodes.USR_003, "Credenciales incorrectas.");
        }

        // Validación de bloqueo por fraude
        if (user.BloqueadoPorFraude)
        {
            _logger.LogWarning("Login rechazado para usuario bloqueado por fraude: {UserId}", user.Id);
            throw new ForbiddenException(ErrorCodes.USR_005, "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");
        }

        // Validación de bloqueo por intentos fallidos
        if (!user.Activo || user.IntentosFallidos >= 3)
        {
            _logger.LogWarning("Login rechazado para usuario bloqueado por intentos fallidos: {UserId}", user.Id);
            throw new ForbiddenException(ErrorCodes.USR_004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
        }

        // Validación de contraseña con BCrypt
        var isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isValidPassword)
        {
            user.IntentosFallidos++;
            _logger.LogWarning("Contraseña incorrecta para {UserId}. Intentos fallidos: {Attempts}/3", user.Id, user.IntentosFallidos);

            if (user.IntentosFallidos >= 3)
            {
                user.Activo = false;
                _logger.LogWarning("Usuario {UserId} ha sido bloqueado tras alcanzar 3 intentos fallidos", user.Id);
                throw new ForbiddenException(ErrorCodes.USR_004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
            }

            throw new UnauthorizedException(ErrorCodes.USR_003, "Credenciales incorrectas.");
        }

        // Login exitoso: reiniciar contador de intentos
        user.IntentosFallidos = 0;
        _logger.LogInformation("Login exitoso para usuario {UserId} ({Email})", user.Id, user.Email);

        var response = new LoginResponse
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email
        };

        return Task.FromResult(response);
    }

    public Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(id, out var user))
        {
            _logger.LogWarning("Usuario no encontrado con ID: {UserId}", id);
            throw new NotFoundException(ErrorCodes.USR_007, $"Usuario con ID '{id}' no encontrado.");
        }

        var response = new UserResponse
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Email = user.Email,
            FechaRegistro = user.FechaRegistro,
            Activo = user.Activo
        };

        return Task.FromResult(response);
    }

    public Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = _users.Values.Select(u => new UserResponse
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Apellido = u.Apellido,
            Email = u.Email,
            FechaRegistro = u.FechaRegistro,
            Activo = u.Activo
        });

        return Task.FromResult(users);
    }
}
