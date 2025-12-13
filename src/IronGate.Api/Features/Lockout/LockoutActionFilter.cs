using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Core.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace IronGate.Api.Features.Lockout;

public sealed class LockoutActionFilter(AppDbContext db, IConfigService configService) : IAsyncActionFilter {
    private readonly AppDbContext _db = db;
    private readonly IConfigService _configService = configService;

    public async Task OnActionExecutionAsync(ActionExecutingContext context,ActionExecutionDelegate next) {
        // Extract the username from the action arguments
        var username = ExtractUsername(context.ActionArguments);
        if (username is null) {
            await next();
            return;
        }

        // Get the current db config, if lockout is not enabled, skip
        var config = await _configService.GetConfigAsync(context.HttpContext.RequestAborted);
        if (!config.LockoutEnabled) {
            await next();
            return;
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == username, context.HttpContext.RequestAborted);

        // We do not handle non-existing users here, we let the auth service do that
        if (user is null) {
            await next();
            return;
        }

        // Check if the user is currently locked out, if the user is not locked out, continue
        // We change the LockoutUntil value only in the AuthService when the lockout conditions are met
        // Therefor we only need to check if the current time is before LockoutUntil
        var nowUtc = DateTime.UtcNow;
        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > nowUtc) {
            context.Result = new ObjectResult(new {
                error = "account_locked",
                lockedUntil = user.LockoutUntil
            }) {
                StatusCode = StatusCodes.Status423Locked
            };
            return;
        }

        await next();
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
