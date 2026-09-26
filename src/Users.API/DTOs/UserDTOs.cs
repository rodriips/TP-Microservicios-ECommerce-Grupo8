using System.ComponentModel.DataAnnotations;

namespace Users.API.DTOs;

/// <summary>
/// DTO para el registro de un nuevo usuario.
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// Nombre del usuario.
    /// </summary>
    /// <example>María</example>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario.
    /// </summary>
    /// <example>González</example>
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email es inválido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO para la autenticación de un usuario existente.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email del usuario registrado.
    /// </summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email es inválido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO devuelto tras un registro exitoso.
/// </summary>
public class RegisterUserResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
}

/// <summary>
/// DTO devuelto tras un login exitoso.
/// </summary>
public class LoginResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO para la respuesta de un usuario por ID (usado en consultas inter-servicio).
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
}
