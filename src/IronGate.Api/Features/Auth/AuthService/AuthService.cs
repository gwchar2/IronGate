using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Auth.Dtos;
using IronGate.Api.Features.Auth.PasswordHasher;
using IronGate.Api.Features.Config.ConfigService;
using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database;
using IronGate.Core.Database.Entities;
using IronGate.Core.Security;
using IronGate.Core.Security.TotpValidator;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
namespace IronGate.Api.Features.Auth.AuthService;

public sealed class AuthService(AppDbContext db,IConfigService configService,IPasswordHasher passwordHasher,ITotpValidator totpValidator, string pepper) : IAuthService {
    private readonly AppDbContext _db = db;
    private readonly IConfigService _configService = configService;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITotpValidator _totpValidator = totpValidator;
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


        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)){ 
            attempt.Success = false;
            attempt.Result = AuthResultCode.Fail;
            return FinishAttempt(attempt, stopwatch);
        }

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
            // We mark the latest attempt to now, and Captcha status
            user.LastLoginAttemptAt = DateTime.UtcNow;
            attempt.CaptchaRequired = user.CaptchaRequired;

            // We grab the current hash variant key from config
            var hashVariant = GetHashVariantKey(config);

            // We get the specific hash row for this user matching the current config
            var userHash = await _db.UserHashes.SingleOrDefaultAsync(
                h => h.UserId == user.Id && 
                h.HashAlgorithm == hashVariant &&
                h.PepperEnabled == config.PepperEnabled, cancellationToken);

            // If there isnt a hashed password for the user..... WEEEEEIIIIRRDDDD
            if (userHash is null) {
                RegisterFailedAttempt(user, config);
                await _db.SaveChangesAsync(cancellationToken);

                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            // Verify password
            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash);

            if (!passwordValid) {
                RegisterFailedAttempt(user, config);
                await _db.SaveChangesAsync(cancellationToken);

                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }


            /* Up until now, password is valid */
            // If TOTP is enabled for the user, we require it
            if (user.TotpEnabled) {
                attempt.Success = false;
                attempt.TotpRequired = true;
                attempt.Result = AuthResultCode.TotpRequired;
                return FinishAttempt(attempt, stopwatch);
            }

            // Password only login is successful here
            attempt.Success = true;
            attempt.Result = AuthResultCode.Success;
            RegisterSuccessfulLogin(user);
            await _db.SaveChangesAsync(cancellationToken);
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

            attempt.CaptchaRequired = user.CaptchaRequired;

            /* PASSWORD  VERIFICATION */
            // We grab the current hash variant key from config
            var hashVariant = GetHashVariantKey(config);


            var userHash = await _db.UserHashes.SingleOrDefaultAsync(h => h.UserId == user.Id && h.HashAlgorithm == hashVariant, cancellationToken);

            // If there isnt a hashed password for the user..... WEEEEEIIIIRRDDDD
            if (userHash is null) {
                RegisterFailedAttempt(user, config);
                await _db.SaveChangesAsync(cancellationToken);

                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            var passwordValid = _passwordHasher.VerifyPassword(
                request.Password,
                userHash);

            if (!passwordValid) {
                RegisterFailedAttempt(user, config);
                await _db.SaveChangesAsync(cancellationToken);

                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }
            /* Up until now, password is valid. */

            /* TOTP VALIDATION */
            // If the user does not have TOTP enabled, return failure, since this is the wrong endpoint!
            if (!(user.TotpEnabled)) {
                RegisterFailedAttempt(user, config);
                await _db.SaveChangesAsync(cancellationToken);

                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            attempt.TotpRequired = user.TotpEnabled;            // The attempt requires a TOTP only if the user requires one. If this is false, it will fail this specific request later on (line 240)

            // Validate TOTP code
            var totpValid = _totpValidator.ValidateCode(
                user.TotpSecret!,
                request.TotpCode);

            if (!totpValid) {
                RegisterFailedAttempt(user, config);
                await _db.SaveChangesAsync(cancellationToken);

                attempt.Success = false;
                attempt.Result = AuthResultCode.Fail;
                return FinishAttempt(attempt, stopwatch);
            }

            /* FULL SUCCESS! Password + TOTP are valid */
            attempt.Success = true;
            attempt.Result = AuthResultCode.Success;
            RegisterSuccessfulLogin(user);
            await _db.SaveChangesAsync(cancellationToken);
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
     * This function registers a failed login attempt for lockout purposes.
     */
    private static void RegisterFailedAttempt(User user, AuthConfigDto config) {
        // Hard failures that reach here count as an attempt, important because we can have 
        // a situation where LockoutEnabled = false but CaptchaRequired = true
        user.FailedAttemptsInWindow++;

        // If the conditions for lockout are met, set the lockout time & reset failed attempts
        if (config.LockoutEnabled &&
            config.LockoutThreshold.HasValue &&
            config.LockoutDurationSeconds.HasValue &&
            user.FailedAttemptsInWindow >= config.LockoutThreshold.Value) {


            user.LockoutUntil = DateTime.UtcNow.AddSeconds(config.LockoutDurationSeconds.Value);
            user.FailedAttemptsInWindow = 0;
            user.CaptchaRequired = true;
        }
    }

    /*
     * This function registers a successful login, resetting failed attempt counters.
     */
    private static void RegisterSuccessfulLogin(User user) {
        user.FailedAttemptsInWindow = 0;
        user.LockoutUntil = null;
        user.LastLoginSuccessAt = DateTime.UtcNow;
        user.CaptchaRequired = false;
    }

    /*
     * This function builds the hash variant key used to look up UserHash entries based on the current config.
     */
    private static string GetHashVariantKey(AuthConfigDto config) {
        return config.HashAlgorithm; // "SHA256" / "BCRYPT" / "ARGON2ID"
    }

}
