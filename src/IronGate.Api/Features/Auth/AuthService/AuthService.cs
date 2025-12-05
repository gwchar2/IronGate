using IronGate.Api.Features.Auth.Dtos;
using IronGate.Api.Features.Auth.PasswordHasher;
using IronGate.Api.Features.Auth.TotpValidator;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database;
using IronGate.Core.Database.Entities;
using IronGate.Core.Security;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace IronGate.Api.Features.Auth.AuthService;

public sealed class AuthService(AppDbContext db,IConfigService configService,IPasswordHasher passwordHasher,ITotpValidator totpValidator, string pepper) : IAuthService {
    private readonly AppDbContext _db = db;
    private readonly IConfigService _configService = configService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITotpValidator _totpValidator = totpValidator;
    private readonly string _pepper = pepper;

    public async Task<AuthAttemptDto> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default) {

        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "REGISTER",
            config
            );

        try {
            var existing = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (existing is not null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            var user = new User {
                Id = Guid.NewGuid(),
                Username = request.Username,
                PlainPassword = request.Password  
            };

            _db.Users.Add(user);

            // Hashes WITHOUT pepper
            var (shaHash, shaSalt) = HashHelper.HashSha256(request.Password);
            var (bcryptHash, _) = HashHelper.HashBcrypt(request.Password);
            var (argonHash, argonSalt) = HashHelper.HashArgon2id(request.Password);

            _db.UserHashes.AddRange(
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "sha256",
                    Salt = shaSalt,
                    Hash = shaHash
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "bcrypt",
                    Salt = string.Empty, 
                    Hash = bcryptHash
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "argon2id",
                    Salt = argonSalt,
                    Hash = argonHash
                }
            );

            // Hashes WITH pepper (same salts as non-peppered variants)
            var pepperedPassword = _pepper + request.Password;

            var (shaPepperHash, _) = HashHelper.HashSha256(pepperedPassword, shaSalt);
            var (bcryptPepperHash, _) = HashHelper.HashBcrypt(pepperedPassword);
            var (argonPepperHash, _) = HashHelper.HashArgon2id(pepperedPassword, argonSalt);

            _db.UserHashes.AddRange(
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "sha256+pepper",
                    Salt = shaSalt,
                    Hash = shaPepperHash
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "bcrypt+pepper",
                    Salt = string.Empty,
                    Hash = bcryptPepperHash
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "argon2id+pepper",
                    Salt = argonSalt,
                    Hash = argonPepperHash
                }
            );

            await _db.SaveChangesAsync(cancellationToken);

            attempt.Success = true;
            attempt.Result = AuthResultCode.Success;
            return FinishAttempt(attempt, stopwatch);
        }
        catch {
            attempt.Success = false;
            attempt.Result = AuthResultCode.Fail;
            return FinishAttempt(attempt, stopwatch);
        }
    }

    public async Task<AuthAttemptDto> LoginAsync(
        LoginRequest request,CancellationToken cancellationToken = default) {
        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "LOGIN",
            config
        );

        try {
            var user = await _db.Users
                .SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // We grab the current hash variant key from config
            var hashVariant = BuildHashVariantKey(config);

            // We get the specific hash row for this user matching the current config
            var userHash = await _db.UserHashes
                .SingleOrDefaultAsync(
                    h => h.UserId == user.Id && h.HashAlgorithm == hashVariant,
                    cancellationToken);

            // If there isnt a hashed password for the user..... WEEEEEIIIIRRDDDD
            if (userHash is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // Verify password
            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash,
                config.PepperEnabled ? _pepper : null);

            if (!passwordValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // If TOTP is enabled, this step just says: "password OK, but TOTP needed"
            if (user.TotpEnabled || config.TotpRequired) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.TotpRequired;
                return FinishAttempt(attempt, stopwatch);
            }

            /*
             * TODO: Implement here what if the user REQUIRES CAPTCHA or LOCKOUT steps in future.
             */

            // Password only login is enough under current profile
            attempt.Success = true;
            attempt.Result = AuthResultCode.Success;
            return FinishAttempt(attempt, stopwatch);
        }
        catch {
            attempt.Success = false;
            attempt.Result = AuthResultCode.Fail;
            return FinishAttempt(attempt, stopwatch);
        }
    }

    public async Task<AuthAttemptDto> LoginTotpAsync(
        LoginTotpRequest request, CancellationToken cancellationToken = default) {
        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "LOGIN_TOTP",
            config
            );

        try {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // We grab the current hash variant key from config
            var hashVariant = BuildHashVariantKey(config);

            // We get the specific hash row for this user matching the current config
            var userHash = await _db.UserHashes
                .SingleOrDefaultAsync(
                    h => h.UserId == user.Id && h.HashAlgorithm == hashVariant,
                    cancellationToken);

            // If there isnt a hashed password for the user..... WEEEEEIIIIRRDDDD
            if (userHash is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash,
                config.PepperEnabled ? _pepper : null);

            if (!passwordValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // If the user does not have TOTP enabled, and TOTP is not required for him
            if (!(user.TotpEnabled || config.TotpRequired)) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // Validate TOTP code
            var totpValid = _totpValidator.ValidateCode(
                user.TotpSecret!,
                request.TotpCode);

            if (!totpValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            attempt.Success = true;
            attempt.Result = AuthResultCode.Success;
            return FinishAttempt(attempt, stopwatch);
        }
        catch {
            attempt.Success = false;
            attempt.Result = AuthResultCode.Fail;
            return FinishAttempt(attempt, stopwatch);
        }
    }

    private static (AuthAttemptDto attempt, Stopwatch stopwatch) CreateAttemptSkeleton(
        string username, string operation, AuthConfigDto config) {
        var now = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        var attempt = new AuthAttemptDto {
            Username = username,
            Operation = operation,
            Timestamp = now,
            Success = false,
            Result = AuthResultCode.Fail,
            HashAlgorithm = config.HashAlgorithm,
            Defences = new DefenceSnapshotDto {
                PepperEnabled = config.PepperEnabled,
                CaptchaEnabled = config.CaptchaEnabled,
                TotpRequired = config.TotpRequired,
                RateLimitEnabled = config.RateLimitEnabled,
                LockoutEnabled = config.LockoutEnabled
            },
        };

        return (attempt, stopwatch);
    }

    private static AuthAttemptDto FinishAttempt(AuthAttemptDto attempt, Stopwatch stopwatch) {
        stopwatch.Stop();
        attempt.LatencyMs = (int)stopwatch.Elapsed.TotalMilliseconds;
        return attempt;
    }
    private static string BuildHashVariantKey(AuthConfigDto config) {
        var algo = config.HashAlgorithm.ToLowerInvariant();  // "sha256" / "bcrypt" / "argon2id"
        return config.PepperEnabled ? $"{algo}+pepper" : algo;
    }

}
