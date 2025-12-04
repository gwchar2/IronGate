using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;

/*
 * This class configures the DefenseProfile entity for Entity Framework Core.
 */
public class DefenseProfileConfig : IEntityTypeConfiguration<DefenseProfile> {
    public void Configure(EntityTypeBuilder<DefenseProfile> builder) {
        builder.ToTable("defense_profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.HashMode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SeedGroup)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.RateLimitWindowSeconds);
        builder.Property(x => x.MaxAttemptsPerUser);
        builder.Property(x => x.MaxAttemptsGlobal);
        builder.Property(x => x.LockoutThreshold);
        builder.Property(x => x.LockoutDurationSeconds);
        builder.Property(x => x.CaptchaAfterFailedAttempts);

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.HashMode, x.IsActive });
    }
}
