namespace PythonWiki.Dtos.Courses;

public class CreateCourseRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int XPRequired { get; set; }
    public List<int>? ArticleIds { get; set; }
}
