using Microsoft.EntityFrameworkCore;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class RoleProgressionService : IRoleProgressionService
{
    private readonly PythonWikiDbContext _db;

    public RoleProgressionService(PythonWikiDbContext db)
    {
        _db = db;
    }

    public async Task CheckAndUpgradeRoleAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.Id == userId);

        var newRoleName = user.XP switch
        {
            >= 3000 => "Moderator",
            >= 1500 => "Creator",
            >= 500 => "Editor",
            _ => "Reader"
        };

        if (user.Role.Name == "Admin")
            return; 

        if (user.Role.Name == newRoleName)
            return; 

        var newRole = await _db.Roles.FirstAsync(r => r.Name == newRoleName);

        user.RoleId = newRole.Id;

        await _db.SaveChangesAsync();
    }
}
