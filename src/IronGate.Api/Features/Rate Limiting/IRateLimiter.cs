namespace IronGate.Api.Features.Rate_Limiting;

public interface IRateLimiter {
    RateLimitResult CheckAndConsume(string key, int windowSeconds, int maxAttempts, int? captchaAfterAttempts, DateTime nowUtc);
}