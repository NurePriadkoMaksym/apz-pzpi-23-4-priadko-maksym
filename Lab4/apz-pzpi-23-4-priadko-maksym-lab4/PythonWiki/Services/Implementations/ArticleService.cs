using Microsoft.EntityFrameworkCore;
using PythonWiki.Dtos.Articles;
using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class ArticleService : IArticleService
{
    private readonly PythonWikiDbContext _db;
    private readonly IRoleProgressionService _roleProgressionService;

    public ArticleService(
        PythonWikiDbContext db,
        IRoleProgressionService roleProgressionService)
    {
        _db = db;
        _roleProgressionService = roleProgressionService;
    }


    public async Task<ArticleResponse> CreateArticleAsync(int authorId, CreateArticleRequest request)
    {
        var article = new Article
        {
            Title = request.Title,
            Content = request.Content,
            XPReward = request.XPReward,
            XPRequired = request.XPRequired,
            AuthorId = authorId,
            IsLocked = true 
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        if (request.LinkToArticleIds != null)
        {
            foreach (var toId in request.LinkToArticleIds)
            {
                var link = new ArticleLink
                {
                    FromArticleId = article.Id,
                    ToArticleId = toId
                };

                _db.ArticleLinks.Add(link);
            }

            await _db.SaveChangesAsync();
        }

        return new ArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            XPReward = article.XPReward,
            XPRequired = article.XPRequired,
            IsLocked = article.IsLocked,
            AuthorId = article.AuthorId
        };
    }
    public async Task DeleteArticleAsync(int articleId)
    {
        var article = await _db.Articles
            .Include(a => a.OutgoingLinks)
            .Include(a => a.IncomingLinks)
            .Include(a => a.CourseArticles)
            .Include(a => a.UserProgresses)
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
            throw new InvalidOperationException("Article not found.");
        if (article.OutgoingLinks.Any())
            _db.ArticleLinks.RemoveRange(article.OutgoingLinks);

        if (article.IncomingLinks.Any())
            _db.ArticleLinks.RemoveRange(article.IncomingLinks);

        if (article.CourseArticles.Any())
            _db.CourseArticles.RemoveRange(article.CourseArticles);
        if (article.UserProgresses.Any())
            _db.UserArticleProgresses.RemoveRange(article.UserProgresses);

        _db.Articles.Remove(article);

        await _db.SaveChangesAsync();
    }
    public async Task AddLinkAsync(int fromArticleId, int toArticleId)
    {
        if (fromArticleId == toArticleId)
            throw new InvalidOperationException("Cannot link an article to itself.");

        var from = await _db.Articles.FindAsync(fromArticleId);
        var to = await _db.Articles.FindAsync(toArticleId);

        if (from == null || to == null)
            throw new InvalidOperationException("One or both article IDs do not exist.");

        var exists = await _db.ArticleLinks
            .AnyAsync(l => l.FromArticleId == fromArticleId && l.ToArticleId == toArticleId);

        if (exists)
            return;

        _db.ArticleLinks.Add(new ArticleLink
        {
            FromArticleId = fromArticleId,
            ToArticleId = toArticleId
        });

        await _db.SaveChangesAsync();
    }

    public async Task RemoveLinkAsync(int fromArticleId, int toArticleId)
    {
        var link = await _db.ArticleLinks
            .FirstOrDefaultAsync(l => l.FromArticleId == fromArticleId && l.ToArticleId == toArticleId);

        if (link == null)
            throw new InvalidOperationException("Link does not exist.");

        _db.ArticleLinks.Remove(link);
        await _db.SaveChangesAsync();
    }
    public async Task<ArticleResponse> UpdateArticleAsync(int articleId, int editorId, UpdateArticleRequest request)
    {
        var article = await _db.Articles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == articleId);

        if (article == null)
            throw new InvalidOperationException("Article not found.");

        var user = await _db.Users.Include(u => u.Role).FirstAsync(u => u.Id == editorId);

        bool isAdmin = user.Role.Name == "Admin";
        bool isCreator = user.Role.Name == "Creator";
        bool isEditor = user.Role.Name == "Editor";
        bool isAuthor = article.AuthorId == editorId;

        if (!(isAdmin || isCreator || isEditor || isAuthor))
            throw new UnauthorizedAccessException("You are not allowed to edit this article.");

        article.Title = request.Title;
        article.Content = request.Content;
        article.XPReward = request.XPReward;
        article.XPRequired = request.XPRequired;

        if (request.IsLocked.HasValue)
            article.IsLocked = request.IsLocked.Value;

        await _db.SaveChangesAsync();

        return new ArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            XPReward = article.XPReward,
            XPRequired = article.XPRequired,
            IsLocked = article.IsLocked,
            AuthorId = article.AuthorId
        };
    }
    public async Task<List<ArticleResponse>> GetAvailableArticlesAsync(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var articles = await _db.Articles
            .Where(a =>
                !a.IsLocked || user.XP >= a.XPRequired
            )
            .OrderBy(a => a.XPRequired)
            .ToListAsync();

        return articles.Select(a => new ArticleResponse
        {
            Id = a.Id,
            Title = a.Title,
            Content = a.Content,
            XPReward = a.XPReward,
            XPRequired = a.XPRequired,
            IsLocked = a.IsLocked && user.XP < a.XPRequired,

            AuthorId = a.AuthorId
        }).ToList();
    }


    public async Task<ArticleResponse> GetArticleByIdAsync(int userId, int articleId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId);
        if (article == null)
            throw new InvalidOperationException("Article not found.");
        if (article.IsLocked && user.XP < article.XPRequired)
            throw new InvalidOperationException("Not enough XP to access this article.");

        var progress = await _db.UserArticleProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ArticleId == articleId);

        bool gainedXp = false;
        if (progress == null)
        {
            progress = new UserArticleProgress
            {
                UserId = userId,
                ArticleId = articleId,
                IsCompleted = true
            };

            _db.UserArticleProgresses.Add(progress);

            user.XP += article.XPReward;
            gainedXp = true;
        }

        await _db.SaveChangesAsync();

        if (gainedXp)
        {
            await _roleProgressionService.CheckAndUpgradeRoleAsync(userId);
        }

        return new ArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Content = article.Content,
            XPReward = article.XPReward,
            XPRequired = article.XPRequired,
            IsLocked = article.IsLocked && user.XP < article.XPRequired,

            AuthorId = article.AuthorId
        };
    }
    public async Task<List<ArticleResponse>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new List<ArticleResponse>();

        var query = keyword.Trim().ToLower();

        var articles = await _db.Articles
            .Where(a =>
                a.Title.ToLower().Contains(query) ||
                a.Content.ToLower().Contains(query)
            )
            .ToListAsync();

        return articles.Select(a => new ArticleResponse
        {
            Id = a.Id,
            Title = a.Title,
            Content = a.Content,
            XPReward = a.XPReward,
            XPRequired = a.XPRequired,
            IsLocked = a.IsLocked,
            AuthorId = a.AuthorId
        }).ToList();
    }


}
