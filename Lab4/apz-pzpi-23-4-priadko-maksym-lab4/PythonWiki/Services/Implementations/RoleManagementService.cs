using Microsoft.EntityFrameworkCore;
using PythonWiki.Dtos;
using PythonWiki.Persistence.DbContext;
using PythonWiki.Services.Interfaces;

namespace PythonWiki.Services.Implementations;

public class RoleManagementService : IRoleManagementService
{
    private readonly PythonWikiDbContext _db;

    public RoleManagementService(PythonWikiDbContext db)
    {
        _db = db;
    }

    public async Task ChangeUserRoleAsync(ChangeUserRoleRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
        if (role == null)
            throw new InvalidOperationException("Role not found.");

        user.RoleId = role.Id;
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task<List<string>> GetAllRolesAsync()
    {
        return await _db.Roles.Select(r => r.Name).ToListAsync();
    }
}
