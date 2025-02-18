using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fake.EntityFrameworkCore.IntegrationEventLog;

public class IntegrationEventLogContext : EfCoreDbContext<IntegrationEventLogContext>
{
    public DbSet<IntegrationEventLogEntry> IntegrationEventLogs { get; set; } = null!;

    public IntegrationEventLogContext(DbContextOptions<IntegrationEventLogContext> options) : base(options)
    {
        System.Diagnostics.Debug.WriteLine("IntegrationEventLogContext::ctor ->" + base.GetHashCode());
    }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IntegrationEventLogEntry>(ConfigureIntegrationEventLogEntry);
    }

    void ConfigureIntegrationEventLogEntry(EntityTypeBuilder<IntegrationEventLogEntry> builder)
    {
        builder.ToTable("IntegrationEventLog");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.Content)
            .HasMaxLength(-1)
            .IsRequired();

        builder.Property(e => e.CreationTime)
            .IsRequired();

        builder.Property(e => e.State)
            .IsRequired();

        builder.Property(e => e.TimesSent)
            .IsRequired();

        builder.Property(e => e.EventName)
            .HasMaxLength(30)
            .IsRequired();
    }
}