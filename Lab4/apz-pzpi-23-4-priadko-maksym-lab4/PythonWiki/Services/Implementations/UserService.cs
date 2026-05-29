using Microsoft.EntityFrameworkCore;
using PythonWiki.Dtos;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class UserService : IUserService
{
    private readonly PythonWikiDbContext _db;

    public UserService(PythonWikiDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileResponse> GetMyProfileAsync(int userId)
    {
        return await GetUserProfileByIdAsync(userId);
    }

    public async Task<UserProfileResponse> GetUserProfileByIdAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.ArticleProgresses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            XP = user.XP,
            Role = user.Role.Name,
            CompletedArticles = user.ArticleProgresses.Count(p => p.IsCompleted)
        };
    }
    public async Task DeleteUserAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.ArticleProgresses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (user.RefreshTokens.Any())
            _db.RefreshTokens.RemoveRange(user.RefreshTokens);

        if (user.ArticleProgresses.Any())
            _db.UserArticleProgresses.RemoveRange(user.ArticleProgresses);

        var courseProgress = await _db.UserCourseProgresses
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if (courseProgress.Any())
            _db.UserCourseProgresses.RemoveRange(courseProgress);

        var authoredArticles = await _db.Articles
            .Where(a => a.AuthorId == userId)
            .ToListAsync();

        foreach (var article in authoredArticles)
        {
            article.AuthorId = null;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

}
