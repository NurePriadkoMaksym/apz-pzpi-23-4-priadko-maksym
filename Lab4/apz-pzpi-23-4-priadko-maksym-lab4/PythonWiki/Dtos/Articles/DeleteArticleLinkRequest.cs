namespace PythonWiki.Dtos.Articles;

public class DeleteArticleLinkRequest
{
    public int FromArticleId { get; set; }
    public int ToArticleId { get; set; }
}
