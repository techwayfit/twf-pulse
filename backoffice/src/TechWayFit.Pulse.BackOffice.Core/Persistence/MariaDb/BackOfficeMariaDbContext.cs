using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Persistence.MariaDb;

/// <summary>
/// BackOffice MariaDB DbContext - unified read/write access to main app tables + BackOffice tables.
/// Uses LONGTEXT for JSON columns and DATETIME(6) for timestamps.
/// 
/// IMPORTANT: EF Migrations are DISABLED — apply schema changes via SQL scripts.
/// </summary>
public sealed class BackOfficeMariaDbContext : DbContext
{
    public BackOfficeMariaDbContext(DbContextOptions<BackOfficeMariaDbContext> options) : base(options)
    {
    }

    // ── Main application tables (read/write access) ───────────────────────────
    public DbSet<I.SessionRecord> Sessions => Set<I.SessionRecord>();
    public DbSet<I.ActivityRecord> Activities => Set<I.ActivityRecord>();
    public DbSet<I.ParticipantRecord> Participants => Set<I.ParticipantRecord>();
    public DbSet<I.ResponseRecord> Responses => Set<I.ResponseRecord>();
    public DbSet<I.ContributionCounterRecord> ContributionCounters => Set<I.ContributionCounterRecord>();
    public DbSet<I.FacilitatorUserRecord> FacilitatorUsers => Set<I.FacilitatorUserRecord>();
    public DbSet<I.FacilitatorUserDataRecord> FacilitatorUserData => Set<I.FacilitatorUserDataRecord>();
    public DbSet<I.LoginOtpRecord> LoginOtps => Set<I.LoginOtpRecord>();
    public DbSet<I.SessionGroupRecord> SessionGroups => Set<I.SessionGroupRecord>();
    public DbSet<I.SessionTemplateRecord> SessionTemplates => Set<I.SessionTemplateRecord>();
    public DbSet<I.SessionActivityMetadataRecord> SessionActivityMetadata => Set<I.SessionActivityMetadataRecord>();
    public DbSet<I.SubscriptionPlanRecord> SubscriptionPlans => Set<I.SubscriptionPlanRecord>();
    public DbSet<I.FacilitatorSubscriptionRecord> FacilitatorSubscriptions => Set<I.FacilitatorSubscriptionRecord>();
    public DbSet<I.ActivityTypeDefinitionRecord> ActivityTypeDefinitions => Set<I.ActivityTypeDefinitionRecord>();

    // ── BackOffice-exclusive tables ───────────────────────────────────────────
    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();
    public DbSet<BackOfficeUserRecord> BackOfficeUsers => Set<BackOfficeUserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Main-app entities (MariaDB column types) ──────────────────────────

        modelBuilder.Entity<I.SessionRecord>(e =>
        {
            e.ToTable("Sessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContextJson).HasColumnType("LONGTEXT");
            e.Property(x => x.SettingsJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.JoinFormSchemaJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.SessionStart).HasColumnType("DATETIME(6)");
            e.Property(x => x.SessionEnd).HasColumnType("DATETIME(6)");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => x.FacilitatorUserId);
            e.HasIndex(x => x.GroupId);
        });

        modelBuilder.Entity<I.ActivityRecord>(e =>
        {
            e.ToTable("Activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Prompt).HasMaxLength(1000);
            e.Property(x => x.ConfigJson).HasColumnType("LONGTEXT");
            e.HasIndex(x => new { x.SessionId, x.Order });
            e.HasIndex(x => new { x.SessionId, x.Status });
        });

        modelBuilder.Entity<I.ParticipantRecord>(e =>
        {
            e.ToTable("Participants");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.Property(x => x.DimensionsJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.Token).HasMaxLength(64);
            e.HasIndex(x => new { x.SessionId, x.JoinedAt });
        });

        modelBuilder.Entity<I.ResponseRecord>(e =>
        {
            e.ToTable("Responses");
            e.HasKey(x => x.Id);
            e.Property(x => x.PayloadJson).HasColumnType("LONGTEXT").IsRequired();
            e.Property(x => x.DimensionsJson).HasColumnType("LONGTEXT").IsRequired();
            e.HasIndex(x => new { x.SessionId, x.ActivityId, x.CreatedAt });
            e.HasIndex(x => new { x.ParticipantId, x.CreatedAt });
        });

        modelBuilder.Entity<I.ContributionCounterRecord>(e =>
        {
            e.ToTable("ContributionCounters");
            e.HasKey(x => x.ParticipantId);
            e.HasIndex(x => x.SessionId);
        });

        modelBuilder.Entity<I.FacilitatorUserRecord>(e =>
        {
            e.ToTable("FacilitatorUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<I.FacilitatorUserDataRecord>(e =>
        {
            e.ToTable("FacilitatorUserData");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Value).HasColumnType("LONGTEXT").IsRequired();
            e.HasIndex(x => new { x.FacilitatorUserId, x.Key }).IsUnique();
            e.HasIndex(x => x.FacilitatorUserId);
        });

        modelBuilder.Entity<I.LoginOtpRecord>(e =>
        {
            e.ToTable("LoginOtps");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.OtpCode).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.Email, x.OtpCode });
            e.HasIndex(x => new { x.Email, x.CreatedAt });
            e.HasIndex(x => x.ExpiresAt);
        });

        modelBuilder.Entity<I.SessionGroupRecord>(e =>
        {
            e.ToTable("SessionGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Level).IsRequired();
            e.Property(x => x.Icon).IsRequired().HasMaxLength(50);
            e.Property(x => x.Color).HasMaxLength(20);
            e.HasIndex(x => x.FacilitatorUserId);
            e.HasIndex(x => x.ParentGroupId);
            e.HasIndex(x => new { x.FacilitatorUserId, x.Level, x.ParentGroupId });
        });

        modelBuilder.Entity<I.SessionTemplateRecord>(e =>
        {
            e.ToTable("SessionTemplates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.IconEmoji).HasMaxLength(10).IsRequired();
            e.Property(x => x.ConfigJson).HasColumnType("LONGTEXT").IsRequired();
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.IsSystemTemplate);
            e.HasIndex(x => x.CreatedByUserId);
        });

        modelBuilder.Entity<I.SessionActivityMetadataRecord>(e =>
        {
            e.ToTable("SessionActivityMetadata");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.Property(x => x.Value).HasColumnType("LONGTEXT").IsRequired();
            e.HasIndex(x => new { x.SessionId, x.ActivityId, x.Key }).IsUnique();
            e.HasIndex(x => new { x.SessionId, x.ActivityId });
        });

        modelBuilder.Entity<I.SubscriptionPlanRecord>(e =>
        {
            e.ToTable("SubscriptionPlans");
            e.HasKey(x => x.Id);
            e.Property(x => x.PlanCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.PriceMonthly).HasColumnType("decimal(10,2)").IsRequired();
            e.Property(x => x.PriceYearly).HasColumnType("decimal(10,2)");
            e.Property(x => x.FeaturesJson).HasColumnType("LONGTEXT").IsRequired();
            e.HasIndex(x => x.PlanCode).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        modelBuilder.Entity<I.FacilitatorSubscriptionRecord>(e =>
        {
            e.ToTable("FacilitatorSubscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.PaymentProvider).HasMaxLength(50);
            e.Property(x => x.ExternalCustomerId).HasMaxLength(200);
            e.Property(x => x.ExternalSubscriptionId).HasMaxLength(200);
            e.HasIndex(x => new { x.FacilitatorUserId, x.Status });
            e.HasIndex(x => x.ExternalSubscriptionId);
            e.HasIndex(x => x.PlanId);
        });

        modelBuilder.Entity<I.ActivityTypeDefinitionRecord>(e =>
        {
            e.ToTable("ActivityTypeDefinitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.IconClass).HasMaxLength(100).IsRequired();
            e.Property(x => x.ColorHex).HasMaxLength(7).IsRequired();
            e.Property(x => x.MinPlanCode).HasMaxLength(50);
            e.HasIndex(x => x.ActivityType).IsUnique();
            e.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        // ── BackOffice-exclusive tables (MariaDB column types) ────────────────

        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TableName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.RecordId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OldValue).HasColumnType("LONGTEXT");
            entity.Property(x => x.NewValue).HasColumnType("LONGTEXT");
            entity.Property(x => x.Reason).HasColumnType("LONGTEXT");
            entity.Property(x => x.PerformedBy).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.TableName, x.RecordId });
            entity.HasIndex(x => x.PerformedAt);
            entity.HasIndex(x => x.PerformedBy);
        });

        modelBuilder.Entity<BackOfficeUserRecord>(entity =>
        {
            entity.ToTable("BackOfficeUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Role);
            entity.HasIndex(x => x.IsActive);
        });
    }
}
