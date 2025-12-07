using IronGate.Api.Features.Auth.Dtos;

namespace IronGate.Api.Features.Config.Dtos;

public sealed class AuthConfigDto {
    /* Profile Info (Ignored on request) */
    public Guid Id { get; set; }
    public string Name { get; set; } = "default_name";                 // "baseline", "full_protection", "attempt_1"

    public string HashAlgorithm { get; set; } = null!;        // "sha256_salt", "bcrypt_cost12", "argon2id_m64_t1_p1"

    /* Pepper */
    public bool PepperEnabled { get; set; }

    /* Rate Limiting */
    public bool RateLimitEnabled { get; set; }
    public int? RateLimitWindowSeconds { get; set; }
    public int? MaxAttemptsPerUser { get; set; }

    /* Lockout */
    public bool LockoutEnabled { get; set; }
    public int? LockoutThreshold { get; set; }
    public int? LockoutDurationSeconds { get; set; }

    /* Captcha */
    public bool CaptchaEnabled { get; set; }
    public int? CaptchaAfterFailedAttempts { get; set; }
}