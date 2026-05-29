using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PythonWiki.Dtos.Courses;
using PythonWiki.Services.Interfaces;
using System.Security.Claims;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ICourseCreationService _courseCreationService; 

    public CourseController(ICourseService courseService, ICourseCreationService courseCreationService) 
    {
        _courseService = courseService;
        _courseCreationService = courseCreationService; 
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var courses = await _courseService.GetAvailableCoursesAsync(userId);

        var response = courses.Select(c => new CourseResponse
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            XPRequired = c.XPRequired,
            ArticleIds = c.CourseArticles
                .OrderBy(ca => ca.Order)
                .Select(ca => ca.ArticleId)
                .ToList()
        }).ToList();

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var course = await _courseCreationService.CreateCourseAsync(request);
        return Ok(course);
    }

    [Authorize(Roles = "Creator,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        try
        {
            await _courseCreationService.DeleteCourseAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

}
