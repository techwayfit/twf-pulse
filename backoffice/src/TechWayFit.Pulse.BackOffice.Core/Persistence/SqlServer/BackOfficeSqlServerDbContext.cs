using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.BackOffice.Core.Persistence.Entities;
using I = TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.BackOffice.Core.Persistence.SqlServer;

/// <summary>
/// SQL Server-specific BackOffice DbContext.
/// Uses the <c>pulse</c> schema and NVARCHAR(MAX)/DATETIME2 column types to match
/// the main application's SQL Server schema.
/// 
/// IMPORTANT: EF Migrations are DISABLED — apply schema changes via SQL scripts.
/// </summary>
public sealed class BackOfficeSqlServerDbContext : BackOfficeDbContext
{
    public BackOfficeSqlServerDbContext(DbContextOptions<BackOfficeSqlServerDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlServer(o => o.MigrationsHistoryTable("__MigrationHistory", "pulse"));
    }

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // Apply common BackOffice entity config (keys, indexes, max-lengths)
        base.ConfigureProviderSpecific(modelBuilder);

        // Default schema for all tables
        modelBuilder.HasDefaultSchema("pulse");

        // ── Main-app entities (SQL Server column types + pulse schema) ────────

        modelBuilder.Entity<I.SessionRecord>(e =>
        {
            e.ToTable("Sessions", "pulse");
            e.Property(x => x.ContextJson).HasColumnType("NVARCHAR(MAX)");
            e.Property(x => x.SettingsJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            e.Property(x => x.JoinFormSchemaJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            e.Property(x => x.SessionStart).HasColumnType("DATETIME2");
            e.Property(x => x.SessionEnd).HasColumnType("DATETIME2");
        });

        modelBuilder.Entity<I.ActivityRecord>(e =>
        {
            e.ToTable("Activities", "pulse");
            e.Property(x => x.ConfigJson).HasColumnType("NVARCHAR(MAX)");
        });

        modelBuilder.Entity<I.ParticipantRecord>(e =>
        {
            e.ToTable("Participants", "pulse");
            e.Property(x => x.DimensionsJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            e.Property(x => x.Token).HasMaxLength(64);
        });

        modelBuilder.Entity<I.ResponseRecord>(e =>
        {
            e.ToTable("Responses", "pulse");
            e.Property(x => x.PayloadJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            e.Property(x => x.DimensionsJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
        });

        modelBuilder.Entity<I.ContributionCounterRecord>(e => e.ToTable("ContributionCounters", "pulse"));
        modelBuilder.Entity<I.FacilitatorUserRecord>(e => e.ToTable("FacilitatorUsers", "pulse"));

        modelBuilder.Entity<I.FacilitatorUserDataRecord>(e =>
        {
            e.ToTable("FacilitatorUserData", "pulse");
            e.Property(x => x.Value).HasColumnType("NVARCHAR(MAX)").IsRequired();
        });

        modelBuilder.Entity<I.LoginOtpRecord>(e => e.ToTable("LoginOtps", "pulse"));
        modelBuilder.Entity<I.SessionGroupRecord>(e => e.ToTable("SessionGroups", "pulse"));

        modelBuilder.Entity<I.SessionTemplateRecord>(e =>
        {
            e.ToTable("SessionTemplates", "pulse");
            e.Property(x => x.ConfigJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
        });

        // ── BackOffice-exclusive tables (SQL Server types + pulse schema) ─────

        modelBuilder.Entity<AuditLogRecord>(entity =>
        {
            entity.ToTable("AuditLogs", "pulse");
            entity.Property(x => x.OldValue).HasColumnType("NVARCHAR(MAX)");
            entity.Property(x => x.NewValue).HasColumnType("NVARCHAR(MAX)");
            entity.Property(x => x.Reason).HasColumnType("NVARCHAR(MAX)");
        });

        modelBuilder.Entity<BackOfficeUserRecord>(entity =>
        {
            entity.ToTable("BackOfficeUsers", "pulse");
        });
    }
}
