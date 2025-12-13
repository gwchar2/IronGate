using System.Collections.Concurrent;

namespace IronGate.Api.Features.Rate_Limiting;


/*
 * This class implements a simple in-memory rate limiter.
 * It takes a key (e.g., user ID or IP address), a time window in seconds, and a maximum number of attempts allowed within that window.
 */
public sealed class RateLimiter() : IRateLimiter {
    private sealed class RateLimitState(DateTime windowStart, int count) {
        public DateTime WindowStart = windowStart;
        public int Count = count;
    }

    private readonly ConcurrentDictionary<string, RateLimitState> _states = new();

    public RateLimitResult CheckAndConsume(string key,int windowSeconds, int maxAttempts, int? captchaAfterAttempts, DateTime nowUtc) {

        var state = _states.GetOrAdd(key, _ => new RateLimitState(nowUtc, 0));

        lock (state) {
            // New time window
            if ((nowUtc - state.WindowStart).TotalSeconds >= windowSeconds) {
                state.WindowStart = nowUtc;
                state.Count = 0;
            }
            // Consume a request
            state.Count++;
            if (state.Count > maxAttempts) {
                var windowEnd = state.WindowStart.AddSeconds(windowSeconds);
                var retryAfter = windowEnd - nowUtc;
                if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.Zero;

                return new RateLimitResult(
                    RateLimitStatus.Blocked,
                    retryAfter);
            }

            return new RateLimitResult(
                RateLimitStatus.Ok,
                null);
        }
    }
}
