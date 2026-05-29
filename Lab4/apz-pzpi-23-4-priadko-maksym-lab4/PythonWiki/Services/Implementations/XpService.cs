using Microsoft.EntityFrameworkCore;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class XpService : IXpService
{
    private const int XP_PER_ARTICLE = 50;
    private const int XP_PER_LEVEL = 250;

    private readonly PythonWikiDbContext _db;
    private readonly ICourseService _courseService;
    private readonly IRoleProgressionService _roleProgressionService;

    public XpService(
        PythonWikiDbContext db,
        ICourseService courseService,
        IRoleProgressionService roleProgressionService)
    {
        _db = db;
        _courseService = courseService;
        _roleProgressionService = roleProgressionService;
    }

    public async Task<int> CompleteArticleAsync(int userId, int articleId)
    {
        var progress = await _db.UserArticleProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ArticleId == articleId);

        if (progress != null && progress.IsCompleted)
            return await GetUserXpAsync(userId);

        if (progress == null)
        {
            progress = new Models.UserArticleProgress
            {
                UserId = userId,
                ArticleId = articleId,
                IsCompleted = true
            };

            _db.UserArticleProgresses.Add(progress);
        }
        else
        {
            progress.IsCompleted = true;
        }

        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        user.XP += XP_PER_ARTICLE;

        await _db.SaveChangesAsync();
        await _roleProgressionService.CheckAndUpgradeRoleAsync(userId);
        await _courseService.CheckAndCompleteCoursesAsync(userId);

        return user.XP;
    }

    public int CalculateLevel(int xp)
    {
        return (xp / XP_PER_LEVEL) + 1;
    }

    private async Task<int> GetUserXpAsync(int userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.XP)
            .FirstAsync();
    }
}
