using PythonWiki.Models;
using PythonWiki.Persistence.DbContext;

namespace PythonWiki.Persistence;

public static class DbSeed
{
    public static async Task SeedAsync(PythonWikiDbContext db)
    {
        if (!db.Roles.Any())
        {
            var roles = new[]
            {
                new Role { Name = "Reader" },
                new Role { Name = "Editor" },
                new Role { Name = "Creator" },
                new Role { Name = "Moderator" },
                new Role { Name = "Admin" }
            };

            db.Roles.AddRange(roles);
            await db.SaveChangesAsync();
        }
        if (!db.Users.Any(u => u.Role != null && u.Role.Name == "Admin"))
        {
            var adminRole = db.Roles.First(r => r.Name == "Admin");
            var admin = new User
            {
                Username = "admin",
                Email = "admin@pythonwiki.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123!"),
                XP = 0,
                RoleId = adminRole.Id
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
