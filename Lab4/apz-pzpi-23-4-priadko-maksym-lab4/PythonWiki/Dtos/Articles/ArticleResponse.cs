namespace PythonWiki.Dtos.Articles;

public class ArticleResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int XPReward { get; set; }
    public int XPRequired { get; set; }
    public bool IsLocked { get; set; }
    public int? AuthorId { get; set; }
}
