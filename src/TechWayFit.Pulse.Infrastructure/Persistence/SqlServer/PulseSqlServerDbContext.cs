using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.SqlServer;

/// <summary>
/// SQL Server-specific DbContext for TechWayFit Pulse.
/// Uses SQL Server data types and conventions with pulse schema.
/// 
/// IMPORTANT: EF Migrations are DISABLED for SQL Server.
/// All schema changes must be made via SQL scripts in Infrastructure/Scripts/
/// </summary>
public sealed class PulseSqlServerDbContext : PulseDbContextBase
{
    public PulseSqlServerDbContext(DbContextOptions<PulseSqlServerDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
  
        // Disable migrations for SQL Server - use manual scripts only
        optionsBuilder.UseSqlServer(o => o.MigrationsHistoryTable("__MigrationHistory", "pulse"));
  }

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // Configure default schema for SQL Server
  modelBuilder.HasDefaultSchema("pulse");

        // SQL Server-specific configuration
   modelBuilder.Entity<SessionRecord>(entity =>
        {
     entity.ToTable("Sessions", "pulse");
          entity.Property(x => x.ContextJson).HasColumnType("NVARCHAR(MAX)");
            entity.Property(x => x.SettingsJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            entity.Property(x => x.JoinFormSchemaJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
      entity.Property(x => x.SessionStart).HasColumnType("DATETIME2");
          entity.Property(x => x.SessionEnd).HasColumnType("DATETIME2");
        });

        modelBuilder.Entity<ActivityRecord>(entity =>
     {
            entity.ToTable("Activities", "pulse");
            entity.Property(x => x.ConfigJson).HasColumnType("NVARCHAR(MAX)");
 });

        modelBuilder.Entity<ParticipantRecord>(entity =>
        {
    entity.ToTable("Participants", "pulse");
            entity.Property(x => x.DimensionsJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
            entity.Property(x => x.Token).HasMaxLength(64);
        });

    modelBuilder.Entity<ResponseRecord>(entity =>
        {
   entity.ToTable("Responses", "pulse");
     entity.Property(x => x.PayloadJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
  entity.Property(x => x.DimensionsJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
      });

    modelBuilder.Entity<ContributionCounterRecord>(entity =>
    {
         entity.ToTable("ContributionCounters", "pulse");
     });

  modelBuilder.Entity<FacilitatorUserRecord>(entity =>
        {
            entity.ToTable("FacilitatorUsers", "pulse");
   });

        modelBuilder.Entity<FacilitatorUserDataRecord>(entity =>
 {
            entity.ToTable("FacilitatorUserData", "pulse");
entity.Property(x => x.Value).HasColumnType("NVARCHAR(MAX)").IsRequired();
        });

      modelBuilder.Entity<LoginOtpRecord>(entity =>
     {
        entity.ToTable("LoginOtps", "pulse");
        });

        modelBuilder.Entity<SessionGroupRecord>(entity =>
        {
  entity.ToTable("SessionGroups", "pulse");
     });

        modelBuilder.Entity<SessionTemplateRecord>(entity =>
        {
            entity.ToTable("SessionTemplates", "pulse");
        entity.Property(x => x.ConfigJson).HasColumnType("NVARCHAR(MAX)").IsRequired();
        });
    }
}
