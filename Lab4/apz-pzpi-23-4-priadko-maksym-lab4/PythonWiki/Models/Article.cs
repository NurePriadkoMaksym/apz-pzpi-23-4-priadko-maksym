namespace PythonWiki.Models;

public class Article
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;

    public int XPReward { get; set; }
    public int XPRequired { get; set; }

    public int? AuthorId { get; set; }
    public User? Author { get; set; }
    public bool IsLocked { get; set; } = true;


    public ICollection<UserArticleProgress> UserProgresses { get; set; } = new List<UserArticleProgress>();
    public ICollection<CourseArticle> CourseArticles { get; set; } = new List<CourseArticle>();
    public ICollection<ArticleLink> OutgoingLinks { get; set; } = new List<ArticleLink>();
    public ICollection<ArticleLink> IncomingLinks { get; set; } = new List<ArticleLink>();
}
