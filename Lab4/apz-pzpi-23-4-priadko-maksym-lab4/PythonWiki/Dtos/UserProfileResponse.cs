namespace PythonWiki.Dtos;

public class UserProfileResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int XP { get; set; }
    public string Role { get; set; } = null!;

    public int CompletedArticles { get; set; }
}
