namespace PythonWiki.Dtos;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshExpiresAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Role { get; set; } = null!;
}

