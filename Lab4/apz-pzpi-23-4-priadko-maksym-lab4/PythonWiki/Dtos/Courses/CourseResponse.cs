namespace PythonWiki.Dtos.Courses;

public class CourseResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int XPRequired { get; set; }
    public List<int> ArticleIds { get; set; } = new();
}
