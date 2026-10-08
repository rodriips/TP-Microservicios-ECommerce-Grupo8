using Users.API.DTOs;

namespace Users.API.Services;

/// <summary>
/// Contrato del servicio de negocio de usuarios.
/// </summary>
public interface IUserService
{
    RegisterUserResponse Registrar(RegisterUserRequest request);
    LoginResponse IniciarSesion(LoginRequest request);
    UserResponse ObtenerPorId(Guid id);
    List<UserResponse> ObtenerTodos();
}
