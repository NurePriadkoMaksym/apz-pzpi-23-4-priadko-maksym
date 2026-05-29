using PythonWiki.Models;

namespace PythonWiki.Services.Interfaces;

public interface ILogService
{
    Task LogAsync(int? userId, string action, string? ipAddress);
    Task<List<UserActivityLog>> GetLogsAsync(int? userId = null);
}
