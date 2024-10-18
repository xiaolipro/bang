using Fake.EntityFrameworkCore.Modeling;
using Fake.TenantManagement.Domain.TenantAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.TenantManagement.Infrastructure.EntityConfigurations;

public class TenantConnectionStringEntityTypeConfiguration: IEntityTypeConfiguration<TenantConnectionString>
{
    public void Configure(EntityTypeBuilder<TenantConnectionString> builder)
    {
        builder.ToTable("tenant_connectionStrings", TenantManagementDbContext.DefaultSchema);

        builder.TryConfigureByConvention();

        builder.HasKey(x => new { x.TenantId, x.Name });

        builder.Property(cs => cs.Name).IsRequired().HasMaxLength(TenantConnectionString.MaxNameLength);
        builder.Property(cs => cs.Value).IsRequired().HasMaxLength(TenantConnectionString.MaxValueLength);
    }
}