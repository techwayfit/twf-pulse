using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechWayFit.Pulse.Infrastructure.SignalR.DatabaseBackplane;

namespace TechWayFit.Pulse.Infrastructure.Persistence.Configurations;

public class SignalRMessageConfiguration : IEntityTypeConfiguration<SignalRMessage>
{
    public void Configure(EntityTypeBuilder<SignalRMessage> builder)
    {
   builder.ToTable("SignalRMessages");

      builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
     .ValueGeneratedOnAdd();

     builder.Property(x => x.GroupName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.MethodName)
 .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PayloadJson)
            .IsRequired()
 .HasColumnType("MEDIUMTEXT"); // MariaDB/MySQL specific

        builder.Property(x => x.ServerId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsProcessed)
         .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ProcessedAt);

        // Index for efficient querying
        builder.HasIndex(x => new { x.IsProcessed, x.CreatedAt })
   .HasDatabaseName("IX_SignalRMessages_Processing");

   builder.HasIndex(x => x.GroupName)
            .HasDatabaseName("IX_SignalRMessages_GroupName");
    }
}
