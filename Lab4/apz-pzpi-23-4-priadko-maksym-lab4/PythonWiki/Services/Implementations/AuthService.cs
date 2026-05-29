using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PythonWiki.Dtos;
using PythonWiki.Infrastructure;
using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PythonWiki.Services.Implementations;

public partial class AuthService : IAuthService
{
    private readonly PythonWikiDbContext _db;
    private readonly JwtSettings _jwtSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogService _logService;

    public AuthService(PythonWikiDbContext db, IOptions<JwtSettings> jwtOptions, IConfiguration configuration)
    {
        _db = db;
        _jwtSettings = jwtOptions.Value;
        _configuration = configuration;
    }

    private static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private string GenerateRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<(string Token, DateTime ExpiresAt)> GenerateJwtTokenAsync(User user)
    {
        var role = await _db.Roles.FindAsync(user.RoleId);

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, role?.Name ?? "Reader")
    };

        var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = creds
        };

        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(tokenDescriptor);
        var tokenString = handler.WriteToken(securityToken);

        return (tokenString, expires);
    }


    public async Task<(string Token, DateTime ExpiresAt, string RefreshToken, DateTime RefreshExpiresAt)> GenerateTokensForUserAsync(User user, string? createdByIp = null)
    {
        var jwt = await GenerateJwtTokenAsync(user);
        var rawRefreshToken = GenerateRandomToken();
        var refreshExpires = DateTime.UtcNow.AddDays(30);

        var refreshToken = new RefreshToken
        {
            TokenHash = Sha256(rawRefreshToken),
            UserId = user.Id,
            Created = DateTime.UtcNow,
            CreatedByIp = createdByIp,
            Expires = refreshExpires
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return (jwt.Token, jwt.ExpiresAt, rawRefreshToken, refreshExpires);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("Email already in use.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            throw new InvalidOperationException("Username already in use.");

        var readerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Reader");
        if (readerRole == null)
            throw new InvalidOperationException("Default role not found. Seed roles first.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            XP = 0,
            RoleId = readerRole.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var tokens = await GenerateTokensForUserAsync(user, createdByIp: null);

        return new AuthResponse
        {
            Token = tokens.Token,
            ExpiresAt = tokens.ExpiresAt,
            UserId = user.Id,
            Username = user.Username,
            Role = readerRole.Name
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {

        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == request.Email); 

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) 
            throw new InvalidOperationException("Invalid email or password");

        var tokenData = await GenerateTokensForUserAsync(user); 

        return new AuthResponse
        {
            Token = tokenData.Token,
            ExpiresAt = tokenData.ExpiresAt,
            RefreshToken = tokenData.RefreshToken,
            RefreshExpiresAt = tokenData.RefreshExpiresAt,

            UserId = user.Id,
            Username = user.Username,
            Role = user.Role.Name
        };
    }
    public async Task<RefreshResponse> RefreshAsync(RefreshRequest request, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new InvalidOperationException("Refresh token is required.");

        var incomingHash = Sha256(request.RefreshToken);

        var existing = await _db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash);

        if (existing == null || existing.Revoked != null || existing.IsExpired)
            throw new InvalidOperationException("Invalid refresh token.");

        var newRawRefresh = GenerateRandomToken();
        var newExpires = DateTime.UtcNow.AddDays(30);

        var newRefreshToken = new RefreshToken
        {
            TokenHash = Sha256(newRawRefresh),
            UserId = existing.UserId,
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            Expires = newExpires
        };
        existing.Revoked = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReplacedByTokenHash = newRefreshToken.TokenHash;
        existing.ReasonRevoked = "Rotated";

        _db.RefreshTokens.Add(newRefreshToken);
        _db.RefreshTokens.Update(existing);

        await _db.SaveChangesAsync();

        var jwt = await GenerateJwtTokenAsync(existing.User);

        return new RefreshResponse
        {
            Token = jwt.Token,
            ExpiresAt = jwt.ExpiresAt,
            RefreshToken = newRawRefresh,
            RefreshTokenExpiresAt = newExpires,
            UserId = existing.User.Id,
            Username = existing.User.Username,
            Role = existing.User.Role?.Name ?? "Reader"
        };
    }
    public async Task LogoutAsync(int userId, string? refreshTokenRaw, string ipAddress)
    {
        if (!string.IsNullOrWhiteSpace(refreshTokenRaw))
        {
            var tHash = Sha256(refreshTokenRaw);

            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tHash && rt.UserId == userId);

            if (token != null && token.Revoked == null)
            {
                token.Revoked = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
                token.ReasonRevoked = "User logout";
                _db.RefreshTokens.Update(token);
                await _db.SaveChangesAsync();
            }

            return;
        }

        var now = DateTime.UtcNow;

        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.Revoked == null && rt.Expires > now)
            .ToListAsync();

        foreach (var t in activeTokens)
        {
            t.Revoked = DateTime.UtcNow;
            t.RevokedByIp = ipAddress;
            t.ReasonRevoked = "User logout (all)";
        }

        if (activeTokens.Any())
        {
            _db.RefreshTokens.UpdateRange(activeTokens);
            await _db.SaveChangesAsync();
        }
    }

}
