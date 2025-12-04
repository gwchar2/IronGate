using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IronGate.Core.Database.Builders;


public class DefenseProfileConfiguration : IEntityTypeConfiguration<DefenseProfile> {
    public void Configure(EntityTypeBuilder<DefenseProfile> builder) {
        builder.ToTable("defense_profiles");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.UsePepper)
            .IsRequired();

        builder.Property(d => d.UseRateLimiting)
            .IsRequired();

        builder.Property(d => d.MaxAttemptsPerMinute);

        builder.Property(d => d.UseCaptcha)
            .IsRequired();

        builder.Property(d => d.UseTotp)
            .IsRequired();

        builder.Property(d => d.Notes)
            .HasMaxLength(1000);

        builder.HasMany(d => d.ExperimentRuns)
            .WithOne(r => r.DefenseProfile)
            .HasForeignKey(r => r.DefenseProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}