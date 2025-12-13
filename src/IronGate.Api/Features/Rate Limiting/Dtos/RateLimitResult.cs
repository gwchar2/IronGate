namespace IronGate.Api.Features.Rate_Limiting;

public sealed record RateLimitResult(
    RateLimitStatus Status,
    TimeSpan? RetryAfter
);