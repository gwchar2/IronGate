using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database;
using IronGate.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace IronGate.Api.Features.Config.ConfigService;


/*
 * This service manages authentication configuration settings
 */
public sealed class ConfigService(AppDbContext db) : IConfigService {
    private readonly AppDbContext _db = db;

    public async Task<AuthConfigDto> GetConfigAsync(CancellationToken cancellationToken = default) {
        var entity = await _db.ConfigProfile.SingleAsync(cancellationToken);
        return MapToDto(entity);
    }

    public async Task<AuthConfigDto> UpdateConfigAsync(
        AuthConfigDto request, CancellationToken cancellationToken = default) {
       
        var entity = await _db.ConfigProfile.SingleAsync(cancellationToken); 
        if (request.HashAlgorithm is not ("SHA256" or "BCRYPT" or "ARGON2ID")) {
            throw new InvalidOperationException($"Unsupported hash algorithm: {request.HashAlgorithm}");
        }

        entity.HashAlgorithm = request.HashAlgorithm;
        entity.PepperEnabled = request.PepperEnabled;
        entity.RateLimitEnabled = request.RateLimitEnabled;
        entity.RateLimitWindowSeconds = request.RateLimitWindowSeconds;
        entity.MaxAttemptsPerUser = request.MaxAttemptsPerUser;
        entity.LockoutEnabled = request.LockoutEnabled;
        entity.LockoutThreshold = request.LockoutThreshold;
        entity.LockoutDurationSeconds = request.LockoutDurationSeconds;
        entity.CaptchaEnabled = request.CaptchaEnabled;
        entity.CaptchaAfterFailedAttempts = request.CaptchaAfterFailedAttempts;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    private static AuthConfigDto MapToDto(DbConfigProfile entity) {
        return new AuthConfigDto {
            Id = entity.Id,
            Name = entity.Name,
            HashAlgorithm = entity.HashAlgorithm,
            PepperEnabled = entity.PepperEnabled,
            RateLimitEnabled = entity.RateLimitEnabled,
            RateLimitWindowSeconds = entity.RateLimitWindowSeconds,
            MaxAttemptsPerUser = entity.MaxAttemptsPerUser,
            LockoutEnabled = entity.LockoutEnabled,
            LockoutThreshold = entity.LockoutThreshold,
            LockoutDurationSeconds = entity.LockoutDurationSeconds,
            CaptchaEnabled = entity.CaptchaEnabled,
            CaptchaAfterFailedAttempts = entity.CaptchaAfterFailedAttempts
        };
    }
}
