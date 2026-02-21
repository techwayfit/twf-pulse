using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Sqlite;

/// <summary>
/// SQLite-specific DbContext for TechWayFit Pulse.
/// Uses SQLite data types and conventions.
/// </summary>
public sealed class PulseSqlLiteDbContext : PulseDbContextBase
{
    public PulseSqlLiteDbContext(DbContextOptions<PulseSqlLiteDbContext> options) : base(options)
    {
    }

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // SQLite-specific configuration
        modelBuilder.Entity<SessionRecord>(entity =>
        {
            entity.ToTable("Sessions");
            entity.Property(x => x.ContextJson).HasColumnType("TEXT");
            entity.Property(x => x.SettingsJson).HasColumnType("TEXT").IsRequired();
            entity.Property(x => x.JoinFormSchemaJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<ActivityRecord>(entity =>
        {
            entity.ToTable("Activities");
            entity.Property(x => x.ConfigJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<ParticipantRecord>(entity =>
        {
            entity.ToTable("Participants");
            entity.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<ResponseRecord>(entity =>
        {
            entity.ToTable("Responses");
            entity.Property(x => x.PayloadJson).HasColumnType("TEXT").IsRequired();
            entity.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<ContributionCounterRecord>(entity =>
        {
            entity.ToTable("ContributionCounters");
        });

        modelBuilder.Entity<FacilitatorUserRecord>(entity =>
        {
            entity.ToTable("FacilitatorUsers");
        });

        modelBuilder.Entity<FacilitatorUserDataRecord>(entity =>
        {
            entity.ToTable("FacilitatorUserData");
            entity.Property(x => x.Value).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<LoginOtpRecord>(entity =>
        {
            entity.ToTable("LoginOtps");
        });

        modelBuilder.Entity<SessionGroupRecord>(entity =>
        {
            entity.ToTable("SessionGroups");
        });

        modelBuilder.Entity<SessionTemplateRecord>(entity =>
        {
            entity.ToTable("SessionTemplates");
            entity.Property(x => x.ConfigJson).HasColumnType("TEXT").IsRequired();
        });
    }
}
