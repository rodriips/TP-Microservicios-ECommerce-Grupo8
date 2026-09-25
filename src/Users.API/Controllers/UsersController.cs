using Microsoft.AspNetCore.Mvc;
using Users.API.DTOs;
using Users.API.Services;

namespace Users.API.Controllers;

/// <summary>
/// Controlador principal para la gestión de usuarios, registro y autenticación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo usuario en la plataforma.
    /// </summary>
    /// <remarks>
    /// Si el correo electrónico ya existe, devuelve un error 409 Conflict con código USR-001.
    /// Si los datos son inválidos, devuelve un 400 Bad Request con código USR-002.
    /// </remarks>
    /// <param name="request">Datos requeridos para el registro de usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario creado con su ID y fecha de registro.</returns>
    /// <response code="201">Usuario registrado con éxito.</response>
    /// <response code="400">USR-002 - Los datos del usuario son inválidos.</response>
    /// <response code="409">USR-001 - El email ya está registrado.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Autentica a un usuario mediante su email y contraseña.
    /// </summary>
    /// <remarks>
    /// - Al acumular 3 intentos fallidos consecutivos, la cuenta se bloquea automáticamente (USR-004).
    /// - Si la cuenta está suspendida por fraude devuelve USR-005.
    /// - Nunca expone el PasswordHash en ninguna respuesta.
    /// </remarks>
    /// <param name="request">Credenciales del usuario (email y contraseña).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datos del usuario autenticado.</returns>
    /// <response code="200">Autenticación exitosa.</response>
    /// <response code="400">USR-002 - Los datos del usuario son inválidos.</response>
    /// <response code="401">USR-003 - Credenciales incorrectas.</response>
    /// <response code="403">USR-004 o USR-005 - Usuario bloqueado por intentos fallidos o fraude.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de un usuario por su identificador único.
    /// </summary>
    /// <param name="id">ID del usuario (GUID).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datos del usuario.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">USR-007 - Usuario no encontrado.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lista todos los usuarios registrados en el sistema.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Colección de usuarios.</returns>
    /// <response code="200">Listado de usuarios obtenido correctamente.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
