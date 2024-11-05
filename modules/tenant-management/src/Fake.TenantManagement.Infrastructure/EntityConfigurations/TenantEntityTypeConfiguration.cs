using Fake.EntityFrameworkCore.Modeling;
using Fake.TenantManagement.Domain.TenantAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.TenantManagement.Infrastructure.EntityConfigurations;

public class TenantEntityTypeConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant", TenantManagementDbContext.DefaultSchema);

        builder.TryConfigureByConvention();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(Tenant.MaxNameLength);

        builder.HasMany(u => u.ConnectionStrings).WithOne().HasForeignKey(uc => uc.TenantId).IsRequired();

        builder.HasIndex(u => u.Name);
    }
}