namespace TaskManagement.Application.DTOs;

public record RegisterRequest(string Email, string Username, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfo User);

public record UserInfo(Guid Id, string Email, string Username, string Role);
