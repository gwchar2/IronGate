using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace IronGate.Core.Database.Builders;


public class ExperimentRunConfiguration : IEntityTypeConfiguration<ExperimentRun> {
    public void Configure(EntityTypeBuilder<ExperimentRun> builder) {
        builder.ToTable("experiment_runs");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.StartedAtUtc)
            .IsRequired();

        builder.Property(r => r.CompletedAtUtc);

        builder.Property(r => r.DurationMs)
            .IsRequired();

        builder.Property(r => r.Success)
            .IsRequired();

        builder.Property(r => r.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(r => r.TotalAttempts)
            .IsRequired();

        builder.Property(r => r.AttemptsPerSecond)
            .IsRequired();

        builder.Property(r => r.TimeToFirstSuccessMs);

        builder.Property(r => r.AverageLatencyMs)
            .IsRequired();

        builder.Property(r => r.SuccessCount)
            .IsRequired();

        builder.Property(r => r.FailureCount)
            .IsRequired();

        builder.HasIndex(r => r.StartedAtUtc);
        builder.HasIndex(r => new {
            r.HashProfileId,
            r.DefenseProfileId
        });
    }
}