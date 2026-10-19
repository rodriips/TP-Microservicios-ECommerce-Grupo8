using Microsoft.AspNetCore.Mvc;
using Users.API.Common;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Services;

namespace Users.API.Controllers;

/// <summary>
/// Endpoints del microservicio de autenticación y gestión de usuarios.
/// El controller no tiene try/catch: los errores los lanza el service
/// y los responden los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Registra un nuevo usuario en la plataforma.
    /// </summary>
    /// <remarks>
    /// Si el correo electrónico ya existe, devuelve un error 409 Conflict con código USR-001.
    /// Si los datos son inválidos, devuelve un 400 Bad Request con código USR-002.
    /// </remarks>
    /// <param name="request">Datos requeridos para el registro de usuario.</param>
    /// <response code="201">Usuario registrado con éxito.</response>
    /// <response code="400">USR-002 - Los datos del usuario son inválidos.</response>
    /// <response code="409">USR-001 - El email ya está registrado.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<RegisterUserResponse> Register([FromBody] RegisterUserRequest request)
    {
        ValidarDatos();
        RegisterUserResponse creado = _userService.Registrar(request);
        return CreatedAtAction(nameof(GetById), new { id = creado.Id }, creado);
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
    /// <response code="200">Autenticación exitosa.</response>
    /// <response code="400">USR-002 - Los datos del usuario son inválidos.</response>
    /// <response code="401">USR-003 - Credenciales incorrectas.</response>
    /// <response code="403">USR-004 o USR-005 - Usuario bloqueado por intentos fallidos o fraude.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        ValidarDatos();
        LoginResponse resultado = _userService.IniciarSesion(request);
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene el detalle de un usuario por su identificador único.
    /// </summary>
    /// <param name="id">ID del usuario (GUID).</param>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="404">USR-007 - Usuario no encontrado.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<UserResponse> GetById([FromRoute] Guid id)
    {
        UserResponse usuario = _userService.ObtenerPorId(id);
        return Ok(usuario);
    }

    /// <summary>
    /// Lista todos los usuarios registrados en el sistema.
    /// </summary>
    /// <response code="200">Listado de usuarios obtenido correctamente.</response>
    /// <response code="500">USR-006 - Error interno al procesar el usuario.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<UserResponse>> GetAll()
    {
        List<UserResponse> usuarios = _userService.ObtenerTodos();
        return Ok(usuarios);
    }

    /// <summary>
    /// Revisa las Data Annotations del request (Required, EmailAddress, etc.).
    /// Si hay errores, los junta separados por "; " y lanza USR-002.
    /// </summary>
    private void ValidarDatos()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var errores = new List<string>();
        foreach (var campo in ModelState.Values)
        {
            foreach (var error in campo.Errors)
            {
                errores.Add(error.ErrorMessage);
            }
        }

        throw new ValidationException(ErrorCodes.USR_002, string.Join("; ", errores));
    }
}
