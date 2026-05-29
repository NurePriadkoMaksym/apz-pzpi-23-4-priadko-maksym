namespace PythonWiki.Models;

public class ArticleLink
{
    public int FromArticleId { get; set; }
    public Article FromArticle { get; set; } = null!;

    public int ToArticleId { get; set; }
    public Article ToArticle { get; set; } = null!;
}
