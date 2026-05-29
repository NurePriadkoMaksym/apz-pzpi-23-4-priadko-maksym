using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PythonWiki.Models;

namespace PythonWiki.Persistence.Configurations;

public class ArticleLinkConfiguration : IEntityTypeConfiguration<ArticleLink>
{
    public void Configure(EntityTypeBuilder<ArticleLink> builder)
    {
        builder.HasKey(x => new { x.FromArticleId, x.ToArticleId });

        builder.HasOne(x => x.FromArticle)
            .WithMany(a => a.OutgoingLinks)
            .HasForeignKey(x => x.FromArticleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToArticle)
            .WithMany(a => a.IncomingLinks)
            .HasForeignKey(x => x.ToArticleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
