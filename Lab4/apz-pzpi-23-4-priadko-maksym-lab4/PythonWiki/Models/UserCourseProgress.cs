namespace PythonWiki.Models;

public class UserCourseProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public DateTime CompletedAt { get; set; }
}
