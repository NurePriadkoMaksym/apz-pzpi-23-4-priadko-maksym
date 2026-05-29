using PythonWiki.Dtos.Courses;

namespace PythonWiki.Services.Interfaces;

public interface ICourseCreationService
{
    Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request);
    Task DeleteCourseAsync(int courseId);

}
