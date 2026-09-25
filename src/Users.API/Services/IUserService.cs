using Users.API.DTOs;
using Users.API.Models;

namespace Users.API.Services;

/// <summary>
/// Interfaz para las operaciones de negocio sobre usuarios.
/// </summary>
public interface IUserService
{
    Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
