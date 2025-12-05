
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IronGate.Core.Database.Entities;
namespace IronGate.Core.Database.Builders;

/*
 * This class configures the DefenseProfile entity for Entity Framework Core.
 */
public class DbAuthProfileConfig : IEntityTypeConfiguration<DbAuthProfile> {
    public void Configure(EntityTypeBuilder<DbAuthProfile> builder) {
        builder.ToTable("db_auth_profile");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.RateLimitWindowSeconds);
        builder.Property(x => x.MaxAttemptsPerUser);
        builder.Property(x => x.LockoutThreshold);
        builder.Property(x => x.LockoutDurationSeconds);
        builder.Property(x => x.CaptchaAfterFailedAttempts);
    }
}
