namespace IronGate.Cli.Helpers.Dto;

public enum AuthResultCode {
    Success = 0,
    Fail = 1,
    TotpRequired = 2,
    CaptchaRequired = 3,
    RateLimited = 4,
    LockedOut = 5,
    InvalidCaptcha = 6,
    InvalidTotp = 7
}

