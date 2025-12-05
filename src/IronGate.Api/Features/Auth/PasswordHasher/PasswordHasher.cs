using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database.Entities;
using IronGate.Core.Security;
using System.Security.Cryptography;
using System.Text;

namespace IronGate.Api.Features.Auth.PasswordHasher;

public class PasswordHasher(string pepper) : IPasswordHasher {
    private readonly string _pepper = pepper;
    public (string Hash, string Salt) HashPassword(string password, AuthConfigDto config) {
        if (config.HashAlgorithm == "bcrypt") {
            return HashHelper.HashBcrypt(password);
        } else if (config.HashAlgorithm == "argon2id") {
            return HashHelper.HashArgon2id(password);
        } else {
            return HashHelper.HashSha256(password);
        }
    }
    public bool VerifyPassword(string plainPassword, UserHash userHash, string? pepper = null) {
        ArgumentNullException.ThrowIfNull(userHash);

        var algo = userHash.HashAlgorithm.ToLowerInvariant();
        var usePepper = algo.EndsWith("+pepper", StringComparison.Ordinal);

        var baseAlgo = usePepper
            ? algo[..algo.IndexOf("+pepper", StringComparison.Ordinal)]
            : algo;

        if (usePepper) {
            if (pepper is null)
                throw new InvalidOperationException(
                    $"Hash variant {userHash.HashAlgorithm} expects pepper but none was provided.");

            plainPassword = pepper + plainPassword;
        }

        return baseAlgo switch {
            "sha256" => VerifySha256(plainPassword, userHash),
            "bcrypt" => VerifyBcrypt(plainPassword, userHash),
            "argon2id" => VerifyArgon2id(plainPassword, userHash),
            _ => throw new InvalidOperationException(
                    $"Unsupported hash algorithm variant: {userHash.HashAlgorithm}")
        };
    }

    /*
     * Verify SHA-256 hashed password
     */
    private bool VerifySha256(string password, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);
        var (computed, _) = HashHelper.HashSha256(password, userHash.Salt);

        return FixedTimeEquals(computed, userHash.Hash);
    }

    /*
     * Verify bcrypt hashed password
     */

    private bool VerifyBcrypt(string password, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);

        return string.IsNullOrWhiteSpace(userHash.Hash) ? false : BcryptVerify(password, userHash.Hash);
    }

    /*
     * Verify Argon2id hashed password
     */
    private bool VerifyArgon2id(string password, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);
        var (computed, _) = HashHelper.HashArgon2id(password, userHash.Salt);

        return FixedTimeEquals(computed, userHash.Hash);
    }

    /*
     * Compares two strings in fixed time to prevent timing attacks.
     */
    private bool FixedTimeEquals(string left, string right) {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    /*
     * Placeholder for bcrypt verification, replace with actual library call.
     */
    private bool BcryptVerify(string password, string hash) {
        // Example if you use BCrypt.Net-Next:
        // return BCrypt.Net.BCrypt.Verify(password, hash);
        throw new NotImplementedException("Wire BcryptVerify to your bcrypt library.");
    }
}