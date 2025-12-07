
using System.Collections.Concurrent;
using IronGate.Api.Features.Captcha.Dtos;

namespace IronGate.Api.Features.Captcha.CaptchaService;

public sealed class CaptchaService : ICaptchaService {

    private readonly ConcurrentDictionary<string, DateTimeOffset> _tokens = new();
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromMinutes(5);

    /*
     * This function issues a new captcha token with an expiration time.
     */
    public Task<CaptchaTokenResponse> IssueTokenAsync(string groupSeed, CancellationToken cancellationToken = default) {

        var token = Guid.NewGuid().ToString();

        var expiresAt = DateTimeOffset.UtcNow.Add(_tokenLifetime);
        _tokens[token] = expiresAt;

        var resonse = new CaptchaTokenResponse {
            CaptchaToken = token,
            ExpiresAt = expiresAt
        };

        return Task.FromResult(resonse);
    }

    /*
     * This function validates a captcha token by checking its existence and expiration.
     */
    public Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default) {

        if (string.IsNullOrWhiteSpace(token) || !_tokens.TryGetValue(token, out var expiresAt)) {
            return Task.FromResult(false);
        }

        if (DateTimeOffset.UtcNow > expiresAt) {
            _tokens.TryRemove(token, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }


}



