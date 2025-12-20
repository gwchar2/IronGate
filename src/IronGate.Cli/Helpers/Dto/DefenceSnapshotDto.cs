namespace IronGate.Cli.Helpers.Dto;

/*
 * DefenceSnapshotDto represents the current state of authentication defence mechanisms.
 * It indicates whether various security features are enabled or disabled.
 */
public sealed class DefenceSnapshotDto {
    public bool PepperEnabled { get; set; }
    public bool CaptchaEnabled { get; set; }
    public bool RateLimitEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
}
