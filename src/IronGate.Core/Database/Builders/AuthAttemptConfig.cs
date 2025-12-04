using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;

/*
 * This class configures the AuthAttempt entity for EF Core.
 */
public class AuthAttemptConfig : IEntityTypeConfiguration<AuthAttempt> {
    public void Configure(EntityTypeBuilder<AuthAttempt> builder) {
        builder.ToTable("auth_attempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.HashMode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ProtectionFlags)
            .HasMaxLength(1024);

        builder.Property(x => x.AttemptType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Result)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LatencyMs)
            .IsRequired();

        builder.Property(x => x.ClientIp)
            .HasMaxLength(64);

        builder.Property(x => x.ErrorCode)
            .HasMaxLength(64);

        builder.HasOne(x => x.User)
            .WithMany(u => u.AuthAttempts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DefenseProfile)
            .WithMany(p => p.AuthAttempts)
            .HasForeignKey(x => x.DefenseProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => new { x.DefenseProfileId, x.Timestamp });
        builder.HasIndex(x => new { x.Username, x.Timestamp });
    }
}
