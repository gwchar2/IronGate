
using IronGate.Api.Features.Captcha.Dtos;

namespace IronGate.Api.Features.Captcha.CaptchaService;

public interface  ICaptchaService{ 
    
    Task<CaptchaTokenResponse> IssueTokenAsync (string groupSeed, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string groupSeed, CancellationToken cancellationToken = default);
}

