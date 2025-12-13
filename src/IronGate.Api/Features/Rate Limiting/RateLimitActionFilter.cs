
using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Rate_Limiting;
using IronGate.Api.Features.Config.ConfigService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IronGate.Api.Features.Auth.Filters;

public sealed class RateLimitActionFilter(IRateLimiter rateLimiter, IConfigService configService) : IAsyncActionFilter {
    private readonly IRateLimiter _rateLimiter = rateLimiter;
    private readonly IConfigService _configService = configService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next) {

        // Extract the username from the action arguments
        var username = ExtractUsername(context.ActionArguments);
        if (username is null) {
            await next();
            return;
        }

        // Get the current database configuration
        var config = await _configService.GetConfigAsync(context.HttpContext.RequestAborted);

        if (!config.RateLimitEnabled ||
            !config.RateLimitWindowSeconds.HasValue ||
            !config.MaxAttemptsPerUser.HasValue) {
            await next();
            return;
        }

        // Get the client's IP address and construct a unique key of username + IP
        var nowUtc = DateTime.UtcNow;
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"{ip}:{username}";

        var captchaThreshold = config.CaptchaEnabled
          ? config.CaptchaAfterFailedAttempts
          : null;

        var result = _rateLimiter.CheckAndConsume(
            key,
            config.RateLimitWindowSeconds.Value,
            config.MaxAttemptsPerUser.Value,
            captchaThreshold,
            nowUtc);

        switch (result.Status) {
            case RateLimitStatus.Ok:
                await next();
                return;

            case RateLimitStatus.CaptchaRequired:
                context.HttpContext.Items["CaptchaRequiredByRateLimit"] = true;
                await next();
                return;

            case RateLimitStatus.Blocked:
                var detail = result.RetryAfter.HasValue
                    ? $"Rate limit exceeded. Try again in {(int)result.RetryAfter.Value.TotalSeconds} seconds."
                    : "Rate limit exceeded.";

                context.Result = new ObjectResult(new {
                    error = "rate_limited",
                    detail
                }) {
                    StatusCode = StatusCodes.Status429TooManyRequests
                };
                return;

            default:
                await next();
                return;
        }
    }
    public static string? ExtractUsername(IDictionary<string, object?> actionArguments) {
        foreach (var arg in actionArguments.Values) {
            switch (arg) {
                case LoginRequest login:
                    return login.Username;
                case LoginTotpRequest loginTotp:
                    return loginTotp.Username;
            }
        }

        return null;
    }

}
