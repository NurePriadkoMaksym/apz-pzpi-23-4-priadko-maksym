namespace PythonWiki.Dtos.Articles;

public class CreateArticleLinkRequest
{
    public int FromArticleId { get; set; }
    public int ToArticleId { get; set; }
}
