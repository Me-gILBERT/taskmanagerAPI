using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<UserInfo> GetCurrentUserAsync(Guid userId);
}
