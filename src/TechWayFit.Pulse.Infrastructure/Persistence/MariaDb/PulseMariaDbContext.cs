using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

/// <summary>
/// MariaDB-specific DbContext for TechWayFit Pulse.
/// Uses MySQL/MariaDB data types and conventions.
/// 
/// NOTE: Using Pomelo 9.0.0 with EF Core 10.0.3 (version mismatch).
/// Full compatibility pending Pomelo 10.x release.
/// </summary>
public sealed class PulseMariaDbContext : PulseDbContextBase
{
    public PulseMariaDbContext(DbContextOptions<PulseMariaDbContext> options) : base(options)
    {
    }

    protected override void ConfigureProviderSpecific(ModelBuilder modelBuilder)
    {
        // MariaDB/MySQL uses database-level organization (no schemas like SQL Server)
        // Tables are created directly in the specified database

        // MariaDB-specific configuration with TEXT columns for JSON
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

        modelBuilder.Entity<SessionActivityMetadataRecord>(entity =>
        {
            entity.ToTable("SessionActivityMetadata");
            entity.Property(x => x.Value).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<SubscriptionPlanRecord>(entity =>
        {
            entity.ToTable("SubscriptionPlans");
  entity.Property(x => x.FeaturesJson).HasColumnType("TEXT").IsRequired();
     });

        modelBuilder.Entity<FacilitatorSubscriptionRecord>(entity =>
{
  entity.ToTable("FacilitatorSubscriptions");
        });

        modelBuilder.Entity<ActivityTypeDefinitionRecord>(entity =>
        {
      entity.ToTable("ActivityTypeDefinitions");
        });
    }
}
