
namespace IronGate.Core.Database.Entities;

/*
 * This class (table) holds the logs from all attempts to authenticate (login, register, etc.)
 */
public class AuthAttempt {
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Username { get; set; } = null!;      // snapshot, even if user does not exist

    /* DB Defensive Profile used at time of attempt */
    public Guid DefenseProfileId { get; set; }         // DB Defense profile used 
    public DefenseProfile DefenseProfile { get; set; } = null!;
    public string ProtectionFlags { get; set; } = "";  // JSON, bitmask, or simple flags string

    /* Attempt Information */
    public string HashMode { get; set; } = null!;      // Attempted password type
    public string AttemptType { get; set; } = null!;   // "login", "register", "login_totp", etc.
    public string Result { get; set; } = null!;        // "success", "fail_bad_password", "fail_locked", ...
    public string? ClientIp { get; set; }

    /* Questionable */
    public string? ErrorCode { get; set; }              // optional internal error code
    public int LatencyMs { get; set; }
}
