using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly LoginRateLimiter _rateLimiter;

    public AuthService(AppDbContext db, IConfiguration config, LoginRateLimiter rateLimiter)
    {
        _db = db;
        _config = config;
        _rateLimiter = rateLimiter;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("Email already registered");

        var org = await _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync()
            ?? (await SeedDefaultOrganizationAsync());

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            OrganizationId = org.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (_rateLimiter.IsLockedOut(request.Email))
            throw new UnauthorizedAccessException("Account temporarily locked. Try again later.");

        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            _rateLimiter.RecordFailedAttempt(request.Email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var result = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, request.Password);
        if (result != PasswordVerificationResult.Success)
        {
            _rateLimiter.RecordFailedAttempt(request.Email);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        _rateLimiter.Reset(request.Email);
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (token.IsRevoked)
        {
            var user = token.User;
            await RevokeAllUserRefreshTokensAsync(user.Id);
            throw new UnauthorizedAccessException("Refresh token reused — all tokens revoked for security");
        }

        if (token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired");

        token.IsRevoked = true;
        await _db.SaveChangesAsync();

        return await GenerateAuthResponseAsync(token.User);
    }

    private async Task RevokeAllUserRefreshTokensAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();

        foreach (var t in tokens)
            t.IsRevoked = true;

        await _db.SaveChangesAsync();
    }

    public async Task<UserInfo> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("User not found");

        return new UserInfo(user.Id, user.Email, user.Username, user.Role.ToString());
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user);

        return new AuthResponse(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15),
            new UserInfo(user.Id, user.Email, user.Username, user.Role.ToString()));
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("organizationId", user.OrganizationId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(User user)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        return token;
    }

    private async Task<Organization> SeedDefaultOrganizationAsync()
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Default Organization",
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        return org;
    }
}
