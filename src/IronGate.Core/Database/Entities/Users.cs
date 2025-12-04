
namespace IronGate.Core.Database.Entities;

// Represents a user in the system with various password hashes.
public class User {

    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? GroupName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }


    // Sha256 Hash
    public string Sha256Hash { get; set; } = null!;
    public string Sha256Salt { get; set; } = null!;
    // Argon2 Hash
    public string Argon2Hash { get; set; } = null!;
    public string Argon2Salt { get; set; } = null!;
    // Bcrypt Hash
    public string BcryptHash { get; set; } = null!;
    public string? BcryptSalt { get; set; }

    public ICollection<ExperimentRun> ExperimentRuns { get; set; } = new List<ExperimentRun>();
}