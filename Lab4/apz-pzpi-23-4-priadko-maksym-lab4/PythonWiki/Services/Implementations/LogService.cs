using Microsoft.EntityFrameworkCore;
using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

public class LogService : ILogService
{
    private readonly PythonWikiDbContext _db;

    public LogService(PythonWikiDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? userId, string action, string? ipAddress)
    {
        var entry = new UserActivityLog
        {
            UserId = userId,
            Action = action,
            IpAddress = ipAddress
        };

        _db.UserActivityLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<List<UserActivityLog>> GetLogsAsync(int? userId = null)
    {
        var query = _db.UserActivityLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId);

        return await query.ToListAsync();
    }
}
