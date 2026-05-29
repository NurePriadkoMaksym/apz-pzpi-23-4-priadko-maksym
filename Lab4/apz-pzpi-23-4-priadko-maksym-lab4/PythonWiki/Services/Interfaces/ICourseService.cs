using PythonWiki.Models;

namespace PythonWiki.Services.Interfaces;

public interface ICourseService
{
    Task<List<Course>> GetAvailableCoursesAsync(int userId);
    Task CheckAndCompleteCoursesAsync(int userId);
}
