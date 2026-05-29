using Microsoft.EntityFrameworkCore;
using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class CourseService : ICourseService
{
    private const int COURSE_COMPLETION_XP = 200;

    private readonly PythonWikiDbContext _db;
    private readonly IRoleProgressionService _roleProgressionService;

    public CourseService(PythonWikiDbContext db, IRoleProgressionService roleProgressionService)
    {
        _db = db;
        _roleProgressionService = roleProgressionService;
    }

    public async Task<List<Course>> GetAvailableCoursesAsync(int userId)
    {
        var userXp = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.XP)
            .FirstAsync();

        return await _db.Courses
            .Where(c => userXp >= c.XPRequired)
            .Include(c => c.CourseArticles.OrderBy(x => x.Order))
                .ThenInclude(ca => ca.Article)
            .ToListAsync();
    }

    public async Task CheckAndCompleteCoursesAsync(int userId)
    {
        var completedArticleIds = await _db.UserArticleProgresses
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => p.ArticleId)
            .ToListAsync();

        var courses = await _db.Courses
            .Include(c => c.CourseArticles)
            .ToListAsync();

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.Id == userId);

        foreach (var course in courses)
        {
            var courseArticleIds = course.CourseArticles
                .Select(ca => ca.ArticleId)
                .ToList();

            if (!courseArticleIds.Any())
                continue;
            if (!courseArticleIds.All(id => completedArticleIds.Contains(id)))
                continue;
            var alreadyCompleted = await _db.UserCourseProgresses
                .AnyAsync(x => x.UserId == userId && x.CourseId == course.Id);

            if (alreadyCompleted)
                continue;

            _db.UserCourseProgresses.Add(new Models.UserCourseProgress
            {
                UserId = userId,
                CourseId = course.Id,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            });

            user.XP += COURSE_COMPLETION_XP;

            var articlesToUnlock = await _db.Articles
                .Where(a => a.CourseArticles.Any(ca => ca.CourseId == course.Id))
                .ToListAsync();

            foreach (var article in articlesToUnlock)
            {
                article.IsLocked = false;
            }
        }

        await _db.SaveChangesAsync();
        await _roleProgressionService.CheckAndUpgradeRoleAsync(userId);
    }

}
