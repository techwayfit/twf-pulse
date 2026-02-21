using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence;

/// <summary>
/// Base DbContext implementation with common configuration for all database providers.
/// </summary>
public abstract class PulseDbContextBase : DbContext, IPulseDbContext
{
    protected PulseDbContextBase(DbContextOptions options) : base(options)
    {
    }

public DbSet<SessionRecord> Sessions => Set<SessionRecord>();
    public DbSet<ActivityRecord> Activities => Set<ActivityRecord>();
    public DbSet<ParticipantRecord> Participants => Set<ParticipantRecord>();
    public DbSet<ResponseRecord> Responses => Set<ResponseRecord>();
    public DbSet<ContributionCounterRecord> ContributionCounters => Set<ContributionCounterRecord>();
    public DbSet<FacilitatorUserRecord> FacilitatorUsers => Set<FacilitatorUserRecord>();
  public DbSet<FacilitatorUserDataRecord> FacilitatorUserData => Set<FacilitatorUserDataRecord>();
    public DbSet<LoginOtpRecord> LoginOtps => Set<LoginOtpRecord>();
    public DbSet<SessionGroupRecord> SessionGroups => Set<SessionGroupRecord>();
    public DbSet<SessionTemplateRecord> SessionTemplates => Set<SessionTemplateRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Common entity configuration
    ConfigureEntities(modelBuilder);
 
        // Provider-specific overrides
        ConfigureProviderSpecific(modelBuilder);
    }

    /// <summary>
    /// Common entity configuration shared by all providers.
/// </summary>
    protected virtual void ConfigureEntities(ModelBuilder modelBuilder)
  {
        // Sessions
  modelBuilder.Entity<SessionRecord>(entity =>
   {
    entity.HasKey(x => x.Id);
   entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
     entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
      entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Status);
   entity.HasIndex(x => x.ExpiresAt);
       entity.HasIndex(x => x.FacilitatorUserId);
            entity.HasIndex(x => x.GroupId);
        });

        // Activities
        modelBuilder.Entity<ActivityRecord>(entity =>
        {
    entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
          entity.Property(x => x.Prompt).HasMaxLength(1000);
      entity.HasIndex(x => new { x.SessionId, x.Order });
      entity.HasIndex(x => new { x.SessionId, x.Status });
     });

        // Participants
        modelBuilder.Entity<ParticipantRecord>(entity =>
   {
   entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
         entity.HasIndex(x => new { x.SessionId, x.JoinedAt });
        });

        // Responses
        modelBuilder.Entity<ResponseRecord>(entity =>
        {
  entity.HasKey(x => x.Id);
       entity.HasIndex(x => new { x.SessionId, x.ActivityId, x.CreatedAt });
            entity.HasIndex(x => new { x.ParticipantId, x.CreatedAt });
        });

        // ContributionCounters
   modelBuilder.Entity<ContributionCounterRecord>(entity =>
  {
  entity.HasKey(x => x.ParticipantId);
         entity.HasIndex(x => x.SessionId);
   });

// FacilitatorUsers
        modelBuilder.Entity<FacilitatorUserRecord>(entity =>
    {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
       entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
     entity.HasIndex(x => x.Email).IsUnique();
     entity.HasIndex(x => x.CreatedAt);
        });

        // FacilitatorUserData
        modelBuilder.Entity<FacilitatorUserDataRecord>(entity =>
        {
      entity.HasKey(x => x.Id);
    entity.Property(x => x.Key).HasMaxLength(200).IsRequired();
         entity.HasIndex(x => new { x.FacilitatorUserId, x.Key }).IsUnique();
       entity.HasIndex(x => x.FacilitatorUserId);
     });

     // LoginOtps
     modelBuilder.Entity<LoginOtpRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
      entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.OtpCode).HasMaxLength(10).IsRequired();
            entity.HasIndex(x => new { x.Email, x.OtpCode });
            entity.HasIndex(x => new { x.Email, x.CreatedAt });
            entity.HasIndex(x => x.ExpiresAt);
        });

        // SessionGroups
      modelBuilder.Entity<SessionGroupRecord>(entity =>
   {
    entity.HasKey(x => x.Id);
      entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
 entity.Property(x => x.Level).IsRequired();
            entity.Property(x => x.Icon).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Color).HasMaxLength(20);
      entity.HasIndex(x => x.FacilitatorUserId);
 entity.HasIndex(x => x.ParentGroupId);
            entity.HasIndex(x => new { x.FacilitatorUserId, x.Level, x.ParentGroupId });
        });

        // SessionTemplates
        modelBuilder.Entity<SessionTemplateRecord>(entity =>
      {
         entity.HasKey(x => x.Id);
     entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IconEmoji).HasMaxLength(10).IsRequired();
    entity.HasIndex(x => x.Category);
        entity.HasIndex(x => x.IsSystemTemplate);
            entity.HasIndex(x => x.CreatedByUserId);
      });
    }

 /// <summary>
    /// Provider-specific configuration to be overridden by derived classes.
    /// </summary>
    protected abstract void ConfigureProviderSpecific(ModelBuilder modelBuilder);
}
