using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;

public class HashProfileConfiguration : IEntityTypeConfiguration<HashProfile> {
    public void Configure(EntityTypeBuilder<HashProfile> builder) {
        builder.ToTable("hash_profiles");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.Algorithm)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(h => h.Iterations);

        builder.Property(h => h.MemoryKb);

        builder.Property(h => h.Parallelism);

        builder.Property(h => h.SaltMode)
            .IsRequired();

        builder.Property(h => h.PepperMode)
            .IsRequired();

        builder.Property(h => h.Notes)
            .HasMaxLength(1000);

        builder.HasMany(h => h.ExperimentRuns)
            .WithOne(r => r.HashProfile)
            .HasForeignKey(r => r.HashProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
