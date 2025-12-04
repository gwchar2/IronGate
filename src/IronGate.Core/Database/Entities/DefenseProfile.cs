using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronGate.Core.Database.Entities;

// Represents a defense profile with various security measures.
public class DefenseProfile {
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool UsePepper { get; set; }
    public bool UseRateLimiting { get; set; }
    public int? MaxAttemptsPerMinute { get; set; }
    public bool UseCaptcha { get; set; }
    public bool UseTotp { get; set; }
    public string? Notes { get; set; }
    public ICollection<ExperimentRun> ExperimentRuns { get; set; } = new List<ExperimentRun>();
}