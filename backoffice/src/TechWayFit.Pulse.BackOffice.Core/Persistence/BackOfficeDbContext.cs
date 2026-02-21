using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence;

namespace TechWayFit.Pulse.BackOffice.Core.Persistence;

/// <summary>
/// BackOffice DbContext — extends the shared PulseDbContextBase so operators can
/// read all main-app tables (sessions, users, participants, etc.) while also having
/// exclusive tables for BackOffice audit logs and operator accounts.
/// </summary>
public sealed class BackOfficeDbContext : PulseDbContextBase
{
    public BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options) : base(options)
    {
    }

    // ── BackOffice-exclusive tables ────────────────────────────────────────────

    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();
    public DbSet<BackOfficeUserRecord> BackOfficeUsers => Set<BackOfficeUserRecord>();

    // ── EF configuration ───────────────────────────────────────────────────────

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // ── Reuse the same SQLite table names / types as the main app ──────────

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.SessionRecord>(e =>
        {
            e.ToTable("Sessions");
            e.Property(x => x.ContextJson).HasColumnType("TEXT");
            e.Property(x => x.SettingsJson).HasColumnType("TEXT").IsRequired();
            e.Property(x => x.JoinFormSchemaJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.ActivityRecord>(e =>
        {
            e.ToTable("Activities");
            e.Property(x => x.ConfigJson).HasColumnType("TEXT");
        });

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.ParticipantRecord>(e =>
        {
            e.ToTable("Participants");
            e.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.ResponseRecord>(e =>
        {
            e.ToTable("Responses");
            e.Property(x => x.PayloadJson).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.FacilitatorUserRecord>(e =>
        {
            e.ToTable("FacilitatorUsers");
        });

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.FacilitatorUserDataRecord>(e =>
        {
            e.ToTable("FacilitatorUserData");
        });

        modelBuilder.Entity<TechWayFit.Pulse.Infrastructure.Persistence.Entities.SessionGroupRecord>(e =>
        {
            e.ToTable("SessionGroups");
        });

        // ── BackOffice-exclusive tables ────────────────────────────────────────

        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.OperatorRole).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.FieldName).HasMaxLength(128);
            entity.Property(x => x.OldValue).HasColumnType("TEXT");
            entity.Property(x => x.NewValue).HasColumnType("TEXT");
            entity.Property(x => x.Reason).HasColumnType("TEXT");
            entity.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.OperatorId);
            entity.HasIndex(x => x.OccurredAt);
        });

        modelBuilder.Entity<BackOfficeUserRecord>(entity =>
        {
            entity.ToTable("BackOfficeUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
        });
    }
}
