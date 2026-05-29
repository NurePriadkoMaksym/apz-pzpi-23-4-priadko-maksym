using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PythonWiki.Models;

namespace PythonWiki.Persistence.Configurations;

public class CourseArticleConfiguration : IEntityTypeConfiguration<CourseArticle>
{
    public void Configure(EntityTypeBuilder<CourseArticle> builder)
    {
        builder.HasKey(x => new { x.CourseId, x.ArticleId });

        builder.HasOne(x => x.Course)
            .WithMany(x => x.CourseArticles)
            .HasForeignKey(x => x.CourseId);

        builder.HasOne(x => x.Article)
            .WithMany(x => x.CourseArticles)
            .HasForeignKey(x => x.ArticleId);
    }
}
