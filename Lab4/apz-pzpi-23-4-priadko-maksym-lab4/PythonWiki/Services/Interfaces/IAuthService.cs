using PythonWiki.Dtos;
using PythonWiki.Models;

namespace PythonWiki.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<(string Token, DateTime ExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)> GenerateTokensForUserAsync(User user, string? createdByIp = null);
    Task<RefreshResponse> RefreshAsync(RefreshRequest request, string ipAddress);
    Task LogoutAsync(int userId, string? refreshTokenHash, string ipAddress);

}
