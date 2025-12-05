namespace IronGate.Api.Features.Auth.Dtos;

public enum AuthResultCode {
    Success = 0,
    Fail = 1,
    TotpRequired = 2,
    CaptchaRequired = 3,
    RateLimited = 4,
    LockedOut = 5
}

