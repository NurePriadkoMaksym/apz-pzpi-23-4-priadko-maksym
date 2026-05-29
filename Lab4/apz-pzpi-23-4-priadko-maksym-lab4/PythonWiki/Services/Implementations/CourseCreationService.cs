using Microsoft.EntityFrameworkCore;
using PythonWiki.Dtos.Courses;
using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class CourseCreationService : ICourseCreationService
{
    private readonly PythonWikiDbContext _db;

    public CourseCreationService(PythonWikiDbContext db)
    {
        _db = db;
    }

    public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request)
    {
        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            XPRequired = request.XPRequired
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        if (request.ArticleIds != null && request.ArticleIds.Any())
        {
            var validArticles = await _db.Articles
                .Where(a => request.ArticleIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            int order = 1;

            foreach (var articleId in request.ArticleIds)
            {
                if (!validArticles.Contains(articleId))
                    continue;

                _db.CourseArticles.Add(new CourseArticle
                {
                    CourseId = course.Id,
                    ArticleId = articleId,
                    Order = order++
                });
            }

            await _db.SaveChangesAsync();
        }

        return new CourseResponse
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            XPRequired = course.XPRequired,
            ArticleIds = request.ArticleIds?.ToList() ?? new List<int>()
        };
    }
    public async Task DeleteCourseAsync(int courseId)
    {
        var course = await _db.Courses
            .Include(c => c.CourseArticles)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            throw new InvalidOperationException("Course not found.");

        if (course.CourseArticles.Any())
            _db.CourseArticles.RemoveRange(course.CourseArticles);

        var progress = await _db.UserCourseProgresses
            .Where(x => x.CourseId == courseId)
            .ToListAsync();

        if (progress.Any())
            _db.UserCourseProgresses.RemoveRange(progress);

        _db.Courses.Remove(course);

        await _db.SaveChangesAsync();
    }


}
