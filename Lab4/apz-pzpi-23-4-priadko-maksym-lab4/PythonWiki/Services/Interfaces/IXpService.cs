namespace PythonWiki.Services.Interfaces;

public interface IXpService
{
    Task<int> CompleteArticleAsync(int userId, int articleId);
    int CalculateLevel(int xp);
}
