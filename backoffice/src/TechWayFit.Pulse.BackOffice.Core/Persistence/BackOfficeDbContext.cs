using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;
using TechWayFit.Pulse.Infrastructure.Persistence;

namespace TechWayFit.Pulse.BackOffice.Core.Persistence;

/// <summary>
/// BackOffice DbContext base — extends the shared PulseDbContextBase so operators can
/// read all main-app tables (sessions, users, participants, etc.) while also having
/// exclusive tables for BackOffice audit logs and operator accounts.
/// Provider-specific column types are handled in <see cref="Sqlite.BackOfficeSqliteDbContext"/>
/// and <see cref="SqlServer.BackOfficeSqlServerDbContext"/>.
/// </summary>
public abstract class BackOfficeDbContext : PulseDbContextBase
{
    protected BackOfficeDbContext(DbContextOptions options) : base(options)
    {
    }

    // ── BackOffice-exclusive tables ────────────────────────────────────────────

    public DbSet<AuditLogRecord> AuditLogs => Set<AuditLogRecord>();
    public DbSet<BackOfficeUserRecord> BackOfficeUsers => Set<BackOfficeUserRecord>();

    // ── EF configuration ───────────────────────────────────────────────────────

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // ── BackOffice-exclusive tables — provider-agnostic config ─────────────

        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OperatorId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.OperatorRole).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.FieldName).HasMaxLength(128);
            entity.Property(x => x.IpAddress).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.HasIndex(x => x.OperatorId);
            entity.HasIndex(x => x.OccurredAt);
        });

        modelBuilder.Entity<BackOfficeUserRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
        });
    }
}
