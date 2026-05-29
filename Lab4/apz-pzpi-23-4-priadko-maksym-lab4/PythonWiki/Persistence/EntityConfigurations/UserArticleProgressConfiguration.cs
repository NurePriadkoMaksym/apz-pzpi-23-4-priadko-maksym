using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PythonWiki.Models;

namespace PythonWiki.Persistence.Configurations;

public class UserArticleProgressConfiguration : IEntityTypeConfiguration<UserArticleProgress>
{
    public void Configure(EntityTypeBuilder<UserArticleProgress> builder)
    {
        builder.HasKey(x => new { x.UserId, x.ArticleId });

        builder.HasOne(x => x.User)
            .WithMany(x => x.ArticleProgresses)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Article)
            .WithMany(x => x.UserProgresses)
            .HasForeignKey(x => x.ArticleId);
    }
}
