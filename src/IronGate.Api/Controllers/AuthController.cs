using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Auth.AuthService;
using IronGate.Api.Features.Auth.Dtos;
using IronGate.Api.Features.Auth.Filters;
using IronGate.Api.Features.Lockout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace IronGate.Api.Controllers;


/*
 * TODO: Implement Endpointfilters for Rate Limiting + Logic
 * TODO: Implement Endpointfilters for Lockout + Logic
 * TODO: Implement in log-in (all types) what if the user REQUIRES CAPTCHA + TOTP combination?
 */

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase {
    private readonly IAuthService _authService = authService;

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthAttemptDto>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken) {
        try {
            var attempt = await _authService.RegisterAsync(request, cancellationToken);

            if (!attempt.Success) {
                return Conflict(attempt);
            }

            return Created(string.Empty, attempt);
        }
        catch (Exception ex) {

            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Unexpected error during registration.",
                detail = ex.Message
            });
        } 
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [ServiceFilter(typeof(LockoutActionFilter))]
    [ServiceFilter(typeof(RateLimitActionFilter))]
    public async Task<ActionResult<AuthAttemptDto>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken) {
        try {
            var attempt = await _authService.LoginAsync(request, cancellationToken);

            return attempt.Result switch {
                AuthResultCode.Success => Ok(attempt),
                AuthResultCode.TotpRequired => Unauthorized(attempt),               // 401, client must call /login/totp
                AuthResultCode.CaptchaRequired => StatusCode(StatusCodes.Status429TooManyRequests, attempt),
                AuthResultCode.Fail => Unauthorized(attempt),
                _ => StatusCode(StatusCodes.Status500InternalServerError, attempt)
            };
        }
        catch (Exception ex) {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Unexpected error during login.",
                detail = ex.Message
            });
        }
    }

    // POST: /api/auth/login/totp
    [HttpPost("login/totp")]
    [ServiceFilter(typeof(LockoutActionFilter))]
    public async Task<ActionResult<AuthAttemptDto>> LoginTotp(
        [FromBody] LoginTotpRequest request, CancellationToken cancellationToken) {
        try {
            var attempt = await _authService.LoginTotpAsync(request, cancellationToken);

            return attempt.Result switch {
                AuthResultCode.Success => Ok(attempt),
                AuthResultCode.Fail => Unauthorized(attempt),
                _ => StatusCode(StatusCodes.Status500InternalServerError, attempt)
            };
        }
        catch (Exception ex) {
            // log ex
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Unexpected error during TOTP login.",
                detail = ex.Message
            });
        }
    }
    
    // POST: /api/auth/login/captcha
    [HttpPost("login/captcha")]
    [ServiceFilter(typeof(LockoutActionFilter))]
    public async Task<ActionResult<AuthAttemptDto>> LoginWithCaptcha(
        [FromBody] LoginCaptchaRequest request,CancellationToken cancellationToken) {
        try {
            var attempt = await _authService.LoginWithCaptchaAsync(request, cancellationToken);

            return attempt.Result switch {
                AuthResultCode.Success => Ok(attempt),
                AuthResultCode.CaptchaRequired => StatusCode(StatusCodes.Status429TooManyRequests, attempt),
                AuthResultCode.TotpRequired => Unauthorized(attempt),
                _ => Unauthorized(attempt)
            };
        }
        catch (Exception ex) {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Unexpected error during captcha login.",
                detail = ex.Message
            });
        }
    }



}
