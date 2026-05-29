namespace PythonWiki.Models;

public class CourseArticle
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public int Order { get; set; }
}
