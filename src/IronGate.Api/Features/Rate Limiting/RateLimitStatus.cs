namespace IronGate.Api.Features.Rate_Limiting;


public enum RateLimitStatus {
    Ok,
    CaptchaRequired,
    Blocked
}