using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronGate.Core.Database.Entities;


public class ExperimentRun {


    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid HashProfileId { get; set; }
    public HashProfile HashProfile { get; set; } = null!;
    public Guid DefenseProfileId { get; set; }
    public DefenseProfile DefenseProfile { get; set; } = null!;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public double DurationMs { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }


    // Summary metrics
    public long TotalAttempts { get; set; }
    public double AttemptsPerSecond { get; set; }
    public double? TimeToFirstSuccessMs { get; set; }
    public double AverageLatencyMs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate =>
        TotalAttempts == 0 ? 0 : (double)SuccessCount / TotalAttempts;
}
