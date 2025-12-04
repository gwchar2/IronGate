

using IronGate.Core.Database.Entities.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace IronGate.Core.Database.Entities;


// Represents a hashing profile used for password hashing experiments.
public class HashProfile {
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public required HashAlgorithmType Algorithm { get; set; }
    public int? Iterations { get; set; }
    public int? MemoryKb { get; set; }
    public int? Parallelism { get; set; }
    public SaltMode SaltMode { get; set; }
    public PepperMode PepperMode { get; set; }
    public string? Notes { get; set; }
    public ICollection<ExperimentRun> ExperimentRuns { get; set; } = new List<ExperimentRun>();

}
