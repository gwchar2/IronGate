using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;

/*
 * This class configures the User entity for Entity Framework Core.
 */
public class UserConfig : IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder) {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.Username)
            .IsUnique();

        builder.Property(x => x.PlainPassword)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.PasswordStrengthCategory)
            .HasMaxLength(32);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.FailedAttemptsInWindow)
            .IsRequired();

        builder.Property(x => x.LastLoginSuccessAt);
        builder.Property(x => x.LastLoginAttemptAt);
        builder.Property(x => x.LockoutUntil);

        builder.HasMany(x => x.Hashes)
            .WithOne(h => h.User)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.TotpSecret)
            .HasMaxLength(128); 
    }
}
