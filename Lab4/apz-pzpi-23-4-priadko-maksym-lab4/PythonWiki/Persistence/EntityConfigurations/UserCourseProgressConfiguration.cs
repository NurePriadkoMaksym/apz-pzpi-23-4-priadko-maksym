using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PythonWiki.Models;

namespace PythonWiki.Persistence.Configurations;

public class UserCourseProgressConfiguration : IEntityTypeConfiguration<UserCourseProgress>
{
    public void Configure(EntityTypeBuilder<UserCourseProgress> builder)
    {
        builder.HasOne(x => x.User)
            .WithMany(u => u.CourseProgresses)
            .HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId);

        builder.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
    }
}
