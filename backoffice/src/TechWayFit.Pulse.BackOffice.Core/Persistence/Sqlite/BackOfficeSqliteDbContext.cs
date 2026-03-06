using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Persistence.Sqlite;

/// <summary>
/// SQLite-specific BackOffice DbContext.
/// Configures TEXT column types to match the main app's SQLite schema.
/// </summary>
public sealed class BackOfficeSqliteDbContext : BackOfficeDbContext
{
    public BackOfficeSqliteDbContext(DbContextOptions<BackOfficeSqliteDbContext> options) : base(options)
    {
    }

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // Apply common BackOffice entity config (keys, indexes, max-lengths)
        base.ConfigureProviderSpecific(modelBuilder);

        // ── Main-app entities (SQLite column types) ───────────────────────────

        modelBuilder.Entity<I.SessionRecord>(e =>
        {
            e.ToTable("Sessions");
            e.Property(x => x.ContextJson).HasColumnType("TEXT");
            e.Property(x => x.SettingsJson).HasColumnType("TEXT").IsRequired();
            e.Property(x => x.JoinFormSchemaJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.ActivityRecord>(e =>
        {
            e.ToTable("Activities");
            e.Property(x => x.ConfigJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<I.ParticipantRecord>(e =>
        {
            e.ToTable("Participants");
            e.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.ResponseRecord>(e =>
        {
            e.ToTable("Responses");
            e.Property(x => x.PayloadJson).HasColumnType("TEXT").IsRequired();
            e.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.ContributionCounterRecord>(e => e.ToTable("ContributionCounters"));
        modelBuilder.Entity<I.FacilitatorUserRecord>(e => e.ToTable("FacilitatorUsers"));

        modelBuilder.Entity<I.FacilitatorUserDataRecord>(e =>
        {
            e.ToTable("FacilitatorUserData");
            e.Property(x => x.Value).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.LoginOtpRecord>(e => e.ToTable("LoginOtps"));
        modelBuilder.Entity<I.SessionGroupRecord>(e => e.ToTable("SessionGroups"));

        modelBuilder.Entity<I.SessionTemplateRecord>(e =>
        {
            e.ToTable("SessionTemplates");
            e.Property(x => x.ConfigJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.SessionActivityMetadataRecord>(e =>
        {
            e.ToTable("SessionActivityMetadata");
            e.Property(x => x.Value).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.SubscriptionPlanRecord>(e =>
        {
            e.ToTable("SubscriptionPlans");
            e.Property(x => x.FeaturesJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<I.FacilitatorSubscriptionRecord>(e =>
        {
            e.ToTable("FacilitatorSubscriptions");
        });

        modelBuilder.Entity<I.ActivityTypeDefinitionRecord>(e =>
        {
            e.ToTable("ActivityTypeDefinitions");
        });

        // ── BackOffice-exclusive tables (SQLite column types) ─────────────────

        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.OldValue).HasColumnType("TEXT");
            entity.Property(x => x.NewValue).HasColumnType("TEXT");
            entity.Property(x => x.Reason).HasColumnType("TEXT");
        });

        modelBuilder.Entity<BackOfficeUserRecord>(entity =>
        {
            entity.ToTable("BackOfficeUsers");
        });
    }
}
