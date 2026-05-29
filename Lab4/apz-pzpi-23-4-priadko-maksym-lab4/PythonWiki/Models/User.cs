using System.Data;

namespace PythonWiki.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public int XP { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public ICollection<UserArticleProgress> ArticleProgresses { get; set; } = new List<UserArticleProgress>();
    public ICollection<UserCourseProgress> CourseProgresses { get; set; } = new List<UserCourseProgress>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
