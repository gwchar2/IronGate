using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Captcha.CaptchaService;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Core.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace IronGate.Api.Features.Captcha;

public sealed class CaptchaActionFilter(AppDbContext db, IConfigService configService, ICaptchaService captchaService) : IAsyncActionFilter {
    private readonly AppDbContext _db = db;
    private readonly IConfigService _configService = configService;
    private readonly ICaptchaService _captchaService = captchaService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        // Extract the username from the action arguments
        var username = ExtractUsername(context.ActionArguments);
        if (username is null) {
            await next();
            return;
        }

        // Check if the user is not null
        var cancellationToken = context.HttpContext.RequestAborted;
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username, context.HttpContext.RequestAborted);
        if (user is null) {
            await next();
            return;
        }

        // Get the current captcha settings from the DB
        var captchaConfig = await _configService.GetConfigAsync();
        if (!captchaConfig.CaptchaEnabled) {
            await next();
            return;
        }

        // If the captcha settings are not null and the user failed attempts exceed the threshold, validate the captcha
        // If there is no captcha, return unauthorized and request captcha
        if ((captchaConfig.CaptchaAfterFailedAttempts.HasValue &&
            user.FailedAttemptsInWindow >= captchaConfig.CaptchaAfterFailedAttempts.Value) || user.CaptchaRequired){

            // Get the captcha token
            var captchaToken = ExtractCaptcha(context.ActionArguments);

            // No captcha token provided
            if (string.IsNullOrWhiteSpace(captchaToken)) {
                context.Result = new ObjectResult(new {
                    error = "captcha_required",
                    message = "Captcha token is required due to multiple failed login attempts."
                }) {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            // If captcha is not valid, return unauthorized
            if (!await _captchaService.ValidateTokenAsync(captchaToken, context.HttpContext.RequestAborted)) {
                context.Result = new ObjectResult(new {
                    error = "invalid_captcha",
                    message = "The provided captcha token is invalid or has expired."
                }) {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            // If it is is valid, continue to the action
            await next();
        }
        await next();

    }

    private static string? ExtractUsername(IDictionary<string, object?> actionArguments) {
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
    private static string? ExtractCaptcha(IDictionary<string, object?> actionArguments) {
        foreach (var arg in actionArguments.Values) {
            switch (arg) {
                case LoginRequest login:
                    return login.CaptchaToken;
                case LoginTotpRequest loginTotp:
                    return loginTotp.CaptchaToken;
            }
        }

        return null;
    }

}

