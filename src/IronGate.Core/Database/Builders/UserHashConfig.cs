using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;

/*
 * This class configures the UserHash entity for Entity Framework Core.
 */
public class UserHashConfig : IEntityTypeConfiguration<UserHash> {
    public void Configure(EntityTypeBuilder<UserHash> builder) {
        builder.ToTable("user_hashes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.HashAlgorithm)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Salt)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Hash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.HashAlgorithm }).IsUnique();
    }
}
