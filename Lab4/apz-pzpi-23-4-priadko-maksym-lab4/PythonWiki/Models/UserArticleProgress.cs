namespace PythonWiki.Models;

public class UserArticleProgress
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public bool IsCompleted { get; set; }
}
