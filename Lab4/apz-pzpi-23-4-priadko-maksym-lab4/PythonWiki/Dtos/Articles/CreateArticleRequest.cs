namespace PythonWiki.Dtos.Articles;

public class CreateArticleRequest
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int XPReward { get; set; } = 10;
    public int XPRequired { get; set; } = 0;
    public List<int>? LinkToArticleIds { get; set; }
}
