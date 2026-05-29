using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PythonWiki.Models;

namespace PythonWiki.Persistence.EntityConfigurations
{
    public class IoTDeviceConfigConfiguration : IEntityTypeConfiguration<IoTDeviceConfig>
    {
        public void Configure(EntityTypeBuilder<IoTDeviceConfig> b)
        {
            b.HasKey(x => x.DeviceId);
            b.Property(x => x.Keyword).HasMaxLength(50);
            b.Property(x => x.OutputEnabled).IsRequired();
        }
    }
}
