using Microsoft.EntityFrameworkCore;
using PythonWiki.Models;

namespace PythonWiki.Persistence.DbContext;

public class PythonWikiDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public PythonWikiDbContext(DbContextOptions<PythonWikiDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<UserArticleProgress> UserArticleProgresses => Set<UserArticleProgress>();
    public DbSet<CourseArticle> CourseArticles => Set<CourseArticle>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserCourseProgress> UserCourseProgresses => Set<UserCourseProgress>();
    public DbSet<ArticleLink> ArticleLinks => Set<ArticleLink>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<IoTDeviceConfig> IoTDeviceConfigs { get; set; }
    public DbSet<IoTTelemetry> IoTTelemetries { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PythonWikiDbContext).Assembly);
    }
}
