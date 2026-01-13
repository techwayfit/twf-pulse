using Microsoft.EntityFrameworkCore;
using TechWayFit.Pulse.Infrastructure.Persistence.Entities;

namespace TechWayFit.Pulse.Infrastructure.Persistence;

public sealed class PulseDbContext : DbContext
{
    public PulseDbContext(DbContextOptions<PulseDbContext> options) : base(options)
    {
    }

    public DbSet<SessionRecord> Sessions => Set<SessionRecord>();

    public DbSet<ActivityRecord> Activities => Set<ActivityRecord>();

    public DbSet<ParticipantRecord> Participants => Set<ParticipantRecord>();

    public DbSet<ResponseRecord> Responses => Set<ResponseRecord>();

    public DbSet<ContributionCounterRecord> ContributionCounters => Set<ContributionCounterRecord>();

    public DbSet<FacilitatorUserRecord> FacilitatorUsers => Set<FacilitatorUserRecord>();

    public DbSet<LoginOtpRecord> LoginOtps => Set<LoginOtpRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionRecord>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContextJson).HasColumnType("TEXT");
            entity.Property(x => x.SettingsJson).HasColumnType("TEXT").IsRequired();
            entity.Property(x => x.JoinFormSchemaJson).HasColumnType("TEXT").IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ExpiresAt);
            entity.HasIndex(x => x.FacilitatorUserId);
        });

        modelBuilder.Entity<ActivityRecord>(entity =>
        {
            entity.ToTable("Activities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Prompt).HasMaxLength(1000);
            entity.Property(x => x.ConfigJson).HasColumnType("TEXT");
            entity.HasIndex(x => new { x.SessionId, x.Order });
            entity.HasIndex(x => new { x.SessionId, x.Status });
        });

        modelBuilder.Entity<ParticipantRecord>(entity =>
        {
            entity.ToTable("Participants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
            entity.HasIndex(x => new { x.SessionId, x.JoinedAt });
        });

        modelBuilder.Entity<ResponseRecord>(entity =>
        {
            entity.ToTable("Responses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PayloadJson).HasColumnType("TEXT").IsRequired();
            entity.Property(x => x.DimensionsJson).HasColumnType("TEXT").IsRequired();
            entity.HasIndex(x => new { x.SessionId, x.ActivityId, x.CreatedAt });
            entity.HasIndex(x => new { x.ParticipantId, x.CreatedAt });
        });

        modelBuilder.Entity<ContributionCounterRecord>(entity =>
        {
            entity.ToTable("ContributionCounters");
            entity.HasKey(x => x.ParticipantId);
            entity.HasIndex(x => x.SessionId);
        });

        modelBuilder.Entity<FacilitatorUserRecord>(entity =>
        {
            entity.ToTable("FacilitatorUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
        });

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
    }
}
