
using IronGate.Api.Features.Captcha.CaptchaService;
using IronGate.Api.Features.Captcha.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IronGate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AdminController(ICaptchaService captchaService) : ControllerBase {
    private readonly ICaptchaService _captchaService = captchaService;

    // GET /api/admin/get_captcha_token?groupSeed=...
    [HttpGet("get_captcha_token")]
    public async Task<ActionResult<CaptchaTokenResponse>> GetCaptchaToken(
        [FromQuery] string groupSeed, CancellationToken cancellationToken) {
        
        if (string.IsNullOrWhiteSpace(groupSeed))
            return BadRequest(new { 
                error = "groupSeed is required." 
            });

        var token = await _captchaService.IssueTokenAsync(groupSeed, cancellationToken);
        return Ok(token);
    }
}
