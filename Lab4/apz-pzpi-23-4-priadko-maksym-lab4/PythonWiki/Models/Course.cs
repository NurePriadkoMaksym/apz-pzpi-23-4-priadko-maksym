namespace PythonWiki.Models;

public class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public int XPRequired { get; set; }

    public ICollection<CourseArticle> CourseArticles { get; set; } = new List<CourseArticle>();
}
