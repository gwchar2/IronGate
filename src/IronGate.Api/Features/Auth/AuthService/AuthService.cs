using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Auth.Dtos;
using IronGate.Api.Features.Auth.PasswordHasher;
using IronGate.Api.Features.Captcha.CaptchaService;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database;
using IronGate.Core.Database.Entities;
using IronGate.Core.Security;
using IronGate.Core.Security.TotpValidator;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace IronGate.Api.Features.Auth.AuthService;

public sealed class AuthService(AppDbContext db,IConfigService configService,IPasswordHasher passwordHasher,ITotpValidator totpValidator,ICaptchaService captchaService,string pepper) : IAuthService {
    private readonly AppDbContext _db = db;
    private readonly IConfigService _configService = configService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITotpValidator _totpValidator = totpValidator;
    private readonly ICaptchaService _captchaService = captchaService;
    private readonly string _pepper = pepper;

    /*
     * Registration process
     * Receives: username, password
     * Returns: AuthAttemptDto
     */
    public async Task<AuthAttemptDto> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default) {

        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "REGISTER",
            config);

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
                    HashAlgorithm = "SHA256",
                    Salt = shaSalt,
                    Hash = shaHash
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "BCRYPT",
                    Salt = string.Empty, 
                    Hash = bcryptHash
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "ARGON2ID",
                    Salt = argonSalt,
                    Hash = argonHash
                }
            );

            // Hashes WITH pepper 
            var pepperedPassword = _pepper + request.Password;

            var (shaPepperHash, _) = HashHelper.HashSha256(pepperedPassword, shaSalt);
            var (bcryptPepperHash, _) = HashHelper.HashBcrypt(pepperedPassword);
            var (argonPepperHash, _) = HashHelper.HashArgon2id(pepperedPassword, argonSalt);

            _db.UserHashes.AddRange(
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "SHA256",
                    Salt = shaSalt,
                    Hash = shaPepperHash,
                    PepperEnabled = true
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "BCRYPT",
                    Salt = string.Empty,
                    Hash = bcryptPepperHash,
                    PepperEnabled = true
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "ARGON2ID",
                    Salt = argonSalt,
                    Hash = argonPepperHash,
                    PepperEnabled = true
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


    /*
     * Login process
     * Receives: username, password
     * Returns: AuthAttemptDto
     */
    public async Task<AuthAttemptDto> LoginAsync(
        LoginRequest request,CancellationToken cancellationToken = default) {
        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "LOGIN",
            config);

        try {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // We grab the current hash variant key from config
            var hashVariant = GetHashVariantKey(config);

            // We get the specific hash row for this user matching the current config
            var userHash = await _db.UserHashes.SingleOrDefaultAsync(h => h.UserId == user.Id && h.HashAlgorithm == hashVariant, cancellationToken);

            // If there isnt a hashed password for the user..... WEEEEEIIIIRRDDDD
            if (userHash is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // Verify password
            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash);

            if (!passwordValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // If TOTP is enabled, returns TotpRequired resultcode.
            if (user.TotpEnabled) {
                attempt.Success = false;
                attempt.TotpRequired = true;
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

    /*
     * Login with TOTP process
     * Receives: username, password, totpCode
     * Returns: AuthAttemptDto
     */
    public async Task<AuthAttemptDto> LoginTotpAsync(
        LoginTotpRequest request, CancellationToken cancellationToken = default) {
        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "LOGIN_TOTP",
            config);

        try {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            attempt.TotpRequired = user.TotpEnabled;            // The attempt requires a TOTP only if the user requires one. If this is false, it will fail this specific request later on (line 240)

            /* PASSWORD  VERIFICATION */
            // We grab the current hash variant key from config
            var hashVariant = GetHashVariantKey(config);


            var userHash = await _db.UserHashes.SingleOrDefaultAsync(h => h.UserId == user.Id && h.HashAlgorithm == hashVariant, cancellationToken);

            // If there isnt a hashed password for the user..... WEEEEEIIIIRRDDDD
            if (userHash is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash);

            if (!passwordValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            /* TOTP VALIDATION */
            // If the user does not have TOTP enabled, return failure.
            if (!(user.TotpEnabled)) {
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

    /*
     * Login with CAPTCHA process
     * Receives: username, password, captchaToken
     * Returns: AuthAttemptDto
     */
    public async Task<AuthAttemptDto> LoginWithCaptchaAsync(
       LoginCaptchaRequest request,CancellationToken cancellationToken = default) {
        
        var config = await _configService.GetConfigAsync(cancellationToken);

        var (attempt, stopwatch) = CreateAttemptSkeleton(
            username: request.Username,
            operation: "LOGIN_CAPTCHA",
            config: config);

        try {
            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

            if (user is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            /* Captcha Validation */
            var captchaValid = await _captchaService.ValidateTokenAsync(
                request.CaptchaToken,
                cancellationToken);

            if (!captchaValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.CaptchaRequired;
                attempt.CaptchaRequired = true;     
                return FinishAttempt(attempt, stopwatch);
            }

            /* Password verification */
            var hashVariant = GetHashVariantKey(config);

            var userHash = await _db.UserHashes.SingleOrDefaultAsync(h => h.UserId == user.Id && h.HashAlgorithm == hashVariant,cancellationToken);

            if (userHash is null) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash);

            if (!passwordValid) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            /* If TOTP is also required */
            if (user.TotpEnabled) {
                attempt.Success = false;
                attempt.Result = AuthResultCode.TotpRequired;
                return FinishAttempt(attempt, stopwatch);
            }

            // Captcha + password are OK
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



    /*
     * This function creates the skeleton of an AuthAttemptDto, filling in the required basic fields.
     */
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
                //TOTP Required is filled by the user - by default false
                RateLimitEnabled = config.RateLimitEnabled,
                LockoutEnabled = config.LockoutEnabled
            },
        };

        return (attempt, stopwatch);
    }

    /*
     * This function finalizes an AuthAttemptDto by stopping the stopwatch and calculating latency.
     */
    private static AuthAttemptDto FinishAttempt(AuthAttemptDto attempt, Stopwatch stopwatch) {
        stopwatch.Stop();
        attempt.LatencyMs = (int)stopwatch.Elapsed.TotalMilliseconds;
        return attempt;
    }
    /*
     * This function builds the hash variant key used to look up UserHash entries based on the current config.
     */
    private static string GetHashVariantKey(AuthConfigDto config) {
        return config.HashAlgorithm; // "SHA256" / "BCRYPT" / "ARGON2ID"
    }
}
