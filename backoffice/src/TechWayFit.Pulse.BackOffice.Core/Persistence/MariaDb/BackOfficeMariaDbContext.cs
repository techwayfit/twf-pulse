using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;

/// <summary>
/// MariaDB/MySQL-specific BackOffice DbContext.
/// Uses LONGTEXT for large text/JSON columns and DATETIME(6) for timestamps,
/// matching the main application's MariaDB schema.
///
/// IMPORTANT: EF Migrations are DISABLED — apply schema changes via SQL scripts.
/// </summary>
public sealed class BackOfficeMariaDbContext : BackOfficeDbContext
{
    public BackOfficeMariaDbContext(DbContextOptions<BackOfficeMariaDbContext> options) : base(options)
    {
    }

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // Apply common BackOffice entity config (keys, indexes, max-lengths)
        base.ConfigureProviderSpecific(modelBuilder);

        // ── Main-app entities (MariaDB column types) ──────────────────────────

        modelBuilder.Entity<I.SessionRecord>(e =>
        {
            e.ToTable("Sessions");
            e.Property(x => x.ContextJson).HasColumnType("LONGTEXT");
            e.Property(x => x.SettingsJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.JoinFormSchemaJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.SessionStart).HasColumnType("DATETIME(6)");
            e.Property(x => x.SessionEnd).HasColumnType("DATETIME(6)");
        });

        modelBuilder.Entity<I.ActivityRecord>(e =>
        {
            e.ToTable("Activities");
            e.Property(x => x.ConfigJson).HasColumnType("LONGTEXT");
        });

        modelBuilder.Entity<I.ParticipantRecord>(e =>
        {
            e.ToTable("Participants");
            e.Property(x => x.DimensionsJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.Token).HasMaxLength(64);
        });

        modelBuilder.Entity<I.ResponseRecord>(e =>
        {
            e.ToTable("Responses");
            e.Property(x => x.PayloadJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.DimensionsJson).HasColumnType("LONGTEXT").IsRequired();
        });

        modelBuilder.Entity<I.ContributionCounterRecord>(e => e.ToTable("ContributionCounters"));
        modelBuilder.Entity<I.FacilitatorUserRecord>(e => e.ToTable("FacilitatorUsers"));

        modelBuilder.Entity<I.FacilitatorUserDataRecord>(e =>
        {
            e.ToTable("FacilitatorUserData");
            e.Property(x => x.Value).HasColumnType("LONGTEXT").IsRequired();
        });

        modelBuilder.Entity<I.LoginOtpRecord>(e => e.ToTable("LoginOtps"));
        modelBuilder.Entity<I.SessionGroupRecord>(e => e.ToTable("SessionGroups"));

        modelBuilder.Entity<I.SessionTemplateRecord>(e =>
        {
            e.ToTable("SessionTemplates");
            e.Property(x => x.ConfigJson).HasColumnType("LONGTEXT").IsRequired();
        });

        modelBuilder.Entity<I.SessionActivityMetadataRecord>(e =>
        {
            e.ToTable("SessionActivityMetadata");
            e.Property(x => x.Value).HasColumnType("LONGTEXT").IsRequired();
        });

        modelBuilder.Entity<I.SubscriptionPlanRecord>(e =>
        {
            e.ToTable("SubscriptionPlans");
            e.Property(x => x.FeaturesJson).HasColumnType("LONGTEXT").IsRequired();
        });

        modelBuilder.Entity<I.FacilitatorSubscriptionRecord>(e =>
        {
            e.ToTable("FacilitatorSubscriptions");
        });

        modelBuilder.Entity<I.ActivityTypeDefinitionRecord>(e =>
        {
            e.ToTable("ActivityTypeDefinitions");
        });

        // ── BackOffice-exclusive tables (MariaDB column types) ────────────────

        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.OldValue).HasColumnType("LONGTEXT");
            entity.Property(x => x.NewValue).HasColumnType("LONGTEXT");
            entity.Property(x => x.Reason).HasColumnType("LONGTEXT");
        });

        modelBuilder.Entity<BackOfficeUserRecord>(entity =>
        {
            entity.ToTable("BackOfficeUsers");
        });
    }
}
