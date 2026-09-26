namespace Users.API.Models;

/// <summary>
/// Representa la entidad de usuario en el sistema.
/// </summary>
public class User
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre del usuario.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario.
    /// </summary>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Email único del usuario.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña (nunca se expone en respuestas).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de registro del usuario en UTC.
    /// </summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indica si el usuario está activo. False cuando está bloqueado por intentos o fraude.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Cantidad de intentos fallidos consecutivos de login.
    /// </summary>
    public int IntentosFallidos { get; set; } = 0;

    /// <summary>
    /// Indica si el usuario fue bloqueado por detección de fraude.
    /// </summary>
    public bool BloqueadoPorFraude { get; set; } = false;
}
