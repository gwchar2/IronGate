
namespace IronGate.Core.Database.Entities;
/*
 * This class represents a user in the system.
 */
public class User {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = null!;
    public string PlainPassword { get; set; } = null!;           // research dataset only
    public string PasswordStrengthCategory { get; set; } = "";   // "weak", "medium", "strong" etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /* Lockout & Rate Limiting */
    public int FailedAttemptsInWindow { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime? LastLoginSuccessAt { get; set; } 
    public DateTime? LastLoginAttemptAt { get; set; }

    /* TOTP */
    public bool TotpEnabled { get; set; }           // does this user use TOTP at all?
    public string? TotpSecret { get; set; }         // shared secret (Base32), null if not using TOTP
    public DateTime? TotpRegisteredAt { get; set; } 


    /* PASSWORD HASHES */
    public ICollection<UserHash> Hashes { get; set; } = [];
}
