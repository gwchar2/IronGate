
namespace IronGate.Core.Database.Entities;

/*
 * Represents the defense profile configuration used for authentication.
 */
public class DefenseProfile {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;            // "baseline", "full_protection"
    public int SeedGroup { get; set; }                   // SEED_GROUP (for reproducibility)
    public string HashMode { get; set; } = null!;        // "sha256_salt", "bcrypt_cost12", "argon2id_m64_t1_p1"
    public bool PepperEnabled { get; set; }
    public bool RateLimitEnabled { get; set; }
    public int? RateLimitWindowSeconds { get; set; }
    public int? MaxAttemptsPerUser { get; set; }
    public int? MaxAttemptsGlobal { get; set; }
    public bool LockoutEnabled { get; set; }
    public int? LockoutThreshold { get; set; }
    public int? LockoutDurationSeconds { get; set; }
    public bool CaptchaEnabled { get; set; }
    public int? CaptchaAfterFailedAttempts { get; set; }
    public bool TotpRequired { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public ICollection<AuthAttempt> AuthAttempts { get; set; } = [];
}
