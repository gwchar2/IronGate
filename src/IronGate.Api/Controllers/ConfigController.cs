using IronGate.Api.Features.Config.ConfigService;
using IronGate.Api.Features.Config.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IronGate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConfigController(IConfigService configService) : ControllerBase {
    private readonly IConfigService _configService = configService;

    // GET: /api/config
    [HttpGet]
    public async Task<ActionResult<AuthConfigDto>> Get(CancellationToken cancellationToken) {
        try {
            var config = await _configService.GetConfigAsync(cancellationToken);
            return Ok(config);
        }
        catch (Exception ex) {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Failed to load configuration.",
                detail = ex.Message
            });
        }
    }

    // PUT: /api/config
    [HttpPut]
    public async Task<ActionResult<AuthConfigDto>> Update(
        [FromBody] AuthConfigDto request, CancellationToken cancellationToken) {
        try {
            var updated = await _configService.UpdateConfigAsync(request, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException ex) {
            // Bad config values
            return BadRequest(new {
                error = "Invalid configuration.",
                detail = ex.Message
            });
        }
        catch (Exception ex) {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Failed to update configuration.",
                detail = ex.Message
            });
        }
    }
}
