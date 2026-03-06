using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Abstractions;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence.MariaDb;

/// <summary>
/// MariaDB DbContext for TechWayFit Pulse.
/// Uses LONGTEXT for JSON columns and DATETIME(6) for timestamps.
/// </summary>
public sealed class PulseMariaDbContext : DbContext, IPulseDbContext
{
    public PulseMariaDbContext(DbContextOptions<PulseMariaDbContext> options) : base(options)
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
    public DbSet<SessionActivityMetadataRecord> SessionActivityMetadata => Set<SessionActivityMetadataRecord>();
    public DbSet<SubscriptionPlanRecord> SubscriptionPlans => Set<SubscriptionPlanRecord>();
    public DbSet<FacilitatorSubscriptionRecord> FacilitatorSubscriptions => Set<FacilitatorSubscriptionRecord>();
    public DbSet<ActivityTypeDefinitionRecord> ActivityTypeDefinitions => Set<ActivityTypeDefinitionRecord>();
    public DbSet<PromoCodeRecord> PromoCodes => Set<PromoCodeRecord>();
    public DbSet<PromoCodeRedemptionRecord> PromoCodeRedemptions => Set<PromoCodeRedemptionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ?? Sessions ??????????????????????????????????????????????????????????
        modelBuilder.Entity<SessionRecord>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
          entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
       entity.Property(x => x.ContextJson).HasColumnType("LONGTEXT");
            entity.Property(x => x.SettingsJson).HasColumnType("LONGTEXT").IsRequired();
   entity.Property(x => x.JoinFormSchemaJson).HasColumnType("LONGTEXT").IsRequired();
         entity.Property(x => x.SessionStart).HasColumnType("DATETIME(6)");
        entity.Property(x => x.SessionEnd).HasColumnType("DATETIME(6)");
          entity.HasIndex(x => x.Code).IsUnique();
   entity.HasIndex(x => x.Status);
    entity.HasIndex(x => x.ExpiresAt);
       entity.HasIndex(x => x.FacilitatorUserId);
     entity.HasIndex(x => x.GroupId);
        });

        // ?? Activities ????????????????????????????????????????????????????????
        modelBuilder.Entity<ActivityRecord>(entity =>
        {
          entity.ToTable("Activities");
    entity.HasKey(x => x.Id);
      entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Prompt).HasMaxLength(1000);
            entity.Property(x => x.ConfigJson).HasColumnType("LONGTEXT");
   entity.HasIndex(x => new { x.SessionId, x.Order });
            entity.HasIndex(x => new { x.SessionId, x.Status });
      });

        // ?? Participants ??????????????????????????????????????????????????????
        modelBuilder.Entity<ParticipantRecord>(entity =>
        {
    entity.ToTable("Participants");
   entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
    entity.Property(x => x.DimensionsJson).HasColumnType("LONGTEXT").IsRequired();
            entity.Property(x => x.Token).HasMaxLength(64);
            entity.HasIndex(x => new { x.SessionId, x.JoinedAt });
      });

        // ?? Responses ?????????????????????????????????????????????????????????
  modelBuilder.Entity<ResponseRecord>(entity =>
        {
            entity.ToTable("Responses");
      entity.HasKey(x => x.Id);
       entity.Property(x => x.PayloadJson).HasColumnType("LONGTEXT").IsRequired();
        entity.Property(x => x.DimensionsJson).HasColumnType("LONGTEXT").IsRequired();
     entity.HasIndex(x => new { x.SessionId, x.ActivityId, x.CreatedAt });
    entity.HasIndex(x => new { x.ParticipantId, x.CreatedAt });
        });

        // ?? ContributionCounters ??????????????????????????????????????????????
        modelBuilder.Entity<ContributionCounterRecord>(entity =>
        {
 entity.ToTable("ContributionCounters");
    entity.HasKey(x => x.ParticipantId);
         entity.HasIndex(x => x.SessionId);
  });

// ?? FacilitatorUsers ??????????????????????????????????????????????????
        modelBuilder.Entity<FacilitatorUserRecord>(entity =>
 {
  entity.ToTable("FacilitatorUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
      entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
      });

        // ?? FacilitatorUserData ???????????????????????????????????????????????
        modelBuilder.Entity<FacilitatorUserDataRecord>(entity =>
        {
          entity.ToTable("FacilitatorUserData");
        entity.HasKey(x => x.Id);
       entity.Property(x => x.Key).HasMaxLength(200).IsRequired();
   entity.Property(x => x.Value).HasColumnType("LONGTEXT").IsRequired();
       entity.HasIndex(x => new { x.FacilitatorUserId, x.Key }).IsUnique();
            entity.HasIndex(x => x.FacilitatorUserId);
        });

        // ?? LoginOtps ?????????????????????????????????????????????????????????
        modelBuilder.Entity<LoginOtpRecord>(entity =>
     {
       entity.ToTable("LoginOtps");
         entity.HasKey(x => x.Id);
  entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
    entity.Property(x => x.OtpCode).HasMaxLength(10).IsRequired();
    entity.HasIndex(x => new { x.Email, x.OtpCode });
    entity.HasIndex(x => new { x.Email, x.CreatedAt });
    entity.HasIndex(x => x.ExpiresAt);
        });

        // ?? SessionGroups ?????????????????????????????????????????????????????
   modelBuilder.Entity<SessionGroupRecord>(entity =>
        {
    entity.ToTable("SessionGroups");
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

    // ?? SessionTemplates ??????????????????????????????????????????????????
     modelBuilder.Entity<SessionTemplateRecord>(entity =>
        {
       entity.ToTable("SessionTemplates");
            entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
            entity.Property(x => x.IconEmoji).HasMaxLength(10).IsRequired();
     entity.Property(x => x.ConfigJson).HasColumnType("LONGTEXT").IsRequired();
        entity.HasIndex(x => x.Category);
entity.HasIndex(x => x.IsSystemTemplate);
            entity.HasIndex(x => x.CreatedByUserId);
        });

        // ?? SessionActivityMetadata ???????????????????????????????????????????
        modelBuilder.Entity<SessionActivityMetadataRecord>(entity =>
        {
            entity.ToTable("SessionActivityMetadata");
            entity.HasKey(x => x.Id);
          entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
  entity.Property(x => x.Value).HasColumnType("LONGTEXT").IsRequired();
 entity.HasIndex(x => new { x.SessionId, x.ActivityId, x.Key }).IsUnique();
        entity.HasIndex(x => new { x.SessionId, x.ActivityId });
      });

      // ?? SubscriptionPlans ?????????????????????????????????????????????????
        modelBuilder.Entity<SubscriptionPlanRecord>(entity =>
        {
   entity.ToTable("SubscriptionPlans");
  entity.HasKey(x => x.Id);
            entity.Property(x => x.PlanCode).HasMaxLength(50).IsRequired();
       entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.PriceMonthly).HasColumnType("decimal(10,2)").IsRequired();
       entity.Property(x => x.PriceYearly).HasColumnType("decimal(10,2)");
          entity.Property(x => x.FeaturesJson).HasColumnType("LONGTEXT").IsRequired();
entity.HasIndex(x => x.PlanCode).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        // ?? FacilitatorSubscriptions ??????????????????????????????????????????
 modelBuilder.Entity<FacilitatorSubscriptionRecord>(entity =>
     {
      entity.ToTable("FacilitatorSubscriptions");
   entity.HasKey(x => x.Id);
       entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
entity.Property(x => x.PaymentProvider).HasMaxLength(50);
            entity.Property(x => x.ExternalCustomerId).HasMaxLength(200);
      entity.Property(x => x.ExternalSubscriptionId).HasMaxLength(200);
  entity.HasIndex(x => new { x.FacilitatorUserId, x.Status });
  entity.HasIndex(x => x.ExternalSubscriptionId);
            entity.HasIndex(x => x.PlanId);
      });

        // ?? ActivityTypeDefinitions ???????????????????????????????????????????
    modelBuilder.Entity<ActivityTypeDefinitionRecord>(entity =>
        {
       entity.ToTable("ActivityTypeDefinitions");
        entity.HasKey(x => x.Id);
          entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
     entity.Property(x => x.Description).HasMaxLength(500).IsRequired();
         entity.Property(x => x.IconClass).HasMaxLength(100).IsRequired();
    entity.Property(x => x.ColorHex).HasMaxLength(7).IsRequired();
          entity.Property(x => x.ApplicablePlanIds).HasMaxLength(500);
       entity.HasIndex(x => x.ActivityType).IsUnique();
     entity.HasIndex(x => new { x.IsActive, x.SortOrder });
        });

        // ?? PromoCodes ??????????????????????????????????????????
   modelBuilder.Entity<PromoCodeRecord>(entity =>
        {
     entity.ToTable("PromoCodes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
  entity.HasIndex(x => x.Code).IsUnique();
      entity.HasIndex(x => new { x.IsActive, x.ValidFrom, x.ValidUntil });
            entity.HasIndex(x => x.TargetPlanId);
        });

        // ?? PromoCodeRedemptions ??????????????????????????????????????
        modelBuilder.Entity<PromoCodeRedemptionRecord>(entity =>
        {
      entity.ToTable("PromoCodeRedemptions");
       entity.HasKey(x => x.Id);
   entity.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
          entity.HasIndex(x => new { x.PromoCodeId, x.FacilitatorUserId });
            entity.HasIndex(x => x.FacilitatorUserId);
  entity.HasIndex(x => x.SubscriptionId);
         entity.HasIndex(x => x.RedeemedAt);
     });
    }
}
