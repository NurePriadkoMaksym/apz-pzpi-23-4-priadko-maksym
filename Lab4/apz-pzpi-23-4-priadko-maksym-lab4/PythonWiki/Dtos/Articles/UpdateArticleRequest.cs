namespace PythonWiki.Dtos.Articles;

public class UpdateArticleRequest
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;

    public int XPReward { get; set; }
    public int XPRequired { get; set; }

    public bool? IsLocked { get; set; }
}
