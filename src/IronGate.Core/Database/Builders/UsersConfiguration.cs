using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;
public class UserConfiguration : IEntityTypeConfiguration<User> {
    public void Configure(EntityTypeBuilder<User> builder) {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.UserName)
            .IsUnique();

        builder.Property(u => u.GroupName)
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();

        builder.Property(u => u.Sha256Hash)
            .IsRequired();

        builder.Property(u => u.Sha256Salt)
            .IsRequired();

        builder.Property(u => u.Argon2Hash)
            .IsRequired();

        builder.Property(u => u.Argon2Salt)
            .IsRequired();

        builder.Property(u => u.BcryptHash)
            .IsRequired();

        builder.Property(u => u.BcryptSalt);

        builder.HasMany(u => u.ExperimentRuns)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
