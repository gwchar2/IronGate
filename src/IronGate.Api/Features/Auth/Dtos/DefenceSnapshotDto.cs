namespace IronGate.Api.Features.Auth.Dtos;

public sealed class DefenceSnapshotDto {
    public bool PepperEnabled { get; set; }
    public bool CaptchaEnabled { get; set; }
    public bool TotpRequired { get; set; }
    public bool RateLimitEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
}
