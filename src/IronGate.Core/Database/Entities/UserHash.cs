
namespace IronGate.Core.Database.Entities;

/*
 * This class represents a stored user hash (password or other).
 */
public class UserHash {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string HashAlgorithm { get; set; } = null!;  // must match DefenseConfig.HashMode values ("SHA256" or "BCRYPT" or "ARGON2ID") + peppers
    public string Salt { get; set; } = null!;      // per-user salt (base64, hex, etc.)
    public string Hash { get; set; } = null!;      // stored hash (base64/hex)
    public bool PepperEnabled { get; set; } = false;          // Is pepper active on this hash?
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
