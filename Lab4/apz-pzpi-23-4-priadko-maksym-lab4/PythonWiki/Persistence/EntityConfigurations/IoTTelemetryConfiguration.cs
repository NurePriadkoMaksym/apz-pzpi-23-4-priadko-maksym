using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PythonWiki.Models;

namespace PythonWiki.Persistence.EntityConfigurations
{
    public class IoTTelemetryConfiguration : IEntityTypeConfiguration<IoTTelemetry>
    {
        public void Configure(EntityTypeBuilder<IoTTelemetry> b)
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Keyword).HasMaxLength(50);
            b.Property(x => x.DeviceId).HasMaxLength(50);
        }
    }
}
