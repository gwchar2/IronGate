
namespace IronGate.Core.Database.Entities;

/*
 * Represents the defense profile configuration used for authentication.
 */
public class DbConfigProfile {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;                   // "baseline", "full_protection", "attempt_1"
    
    public string HashAlgorithm { get; set; } = null!;          // "SHA256" or "BCRYPT" or "ARGON2ID"

    /* Pepper */
    public bool PepperEnabled { get; set; }                     // Pepper hashed passwords
    
    /* Rate Limiting */
    public bool RateLimitEnabled { get; set; }                  // Enable rate limitting
    public int? RateLimitWindowSeconds { get; set; }            // Whats the rate limit window
    public int? MaxAttemptsPerUser { get; set; }               
    
    /* Lockout */
    public bool LockoutEnabled { get; set; }                    // Lock out enabled?
    public int? LockoutThreshold { get; set; }                  // Lockout threshold
    public int? LockoutDurationSeconds { get; set; }            // Duration of lockout
    
    /* Captcha */
    public bool CaptchaEnabled { get; set; }                    // Is captcha requirment enabled?
    public int? CaptchaAfterFailedAttempts { get; set; }        // Captcha after this amount of failed attempts
    
}
