using System;

namespace IronGate.Cli.Helpers.Dto;

public sealed class AuthAttemptDto {
    /* Username and operation */
    public string Username { get; set; } = null!;
    public string Operation { get; set; } = null!;          // "REGISTER","LOGIN","LOGIN_TOTP"

    /* Time metrics */
    public DateTimeOffset Timestamp { get; set; }           // Start time
    public int LatencyMs { get; set; }                      // Total time for this request

    /* Result */
    public bool Success { get; set; }
    public AuthResultCode Result { get; set; }
    public DateTime? LockOutUntil { get; set; }

    /* Password Hashing Details */
    public string HashAlgorithm { get; set; } = null!;      // "SHA256","BCRYPT","ARGON2ID"

    /* TOTP Requirement */
    public bool TotpRequired { get; set; } = false;
    public bool CaptchaRequired { get; set; } = false;

    /* Database Defence Details */
    public DefenceSnapshotDto Defences { get; set; } = new();
}
