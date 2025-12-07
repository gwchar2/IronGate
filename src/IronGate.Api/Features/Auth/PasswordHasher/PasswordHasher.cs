using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database.Entities;
using IronGate.Core.Security;
using System.Security.Cryptography;
using System.Text;

namespace IronGate.Api.Features.Auth.PasswordHasher;

/*
 * This class implements the password hashing and verification logic.
 */
public class PasswordHasher(string pepper) : IPasswordHasher {
    private readonly string _pepper = pepper;           // Technically can delete... passed in through DI
    public (string Hash, string Salt) HashPassword(string password, AuthConfigDto config) {
        switch (config.HashAlgorithm.ToUpperInvariant()) {
            case "BCRYPT":
                return HashHelper.HashBcrypt(password);

            case "ARGON2ID":
                return HashHelper.HashArgon2id(password);

            case "SHA256":
                return HashHelper.HashSha256(password);

            default:
                throw new InvalidOperationException(
                    $"Unsupported hash algorithm in auth config: '{config.HashAlgorithm}'.");
        }
    }
    public bool VerifyPassword(string plainPassword, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);

        var algo = userHash.HashAlgorithm;
        var usePepper = userHash.PepperEnabled;

        // If pepper is used, we attach the pepper to the the plain password for checking
        if (usePepper) {
            // Kind of a pointless check.... But we do it anyway since I got an error for it somehow...
            if (_pepper is null)
                throw new InvalidOperationException(
                    $"Hash variant {userHash.HashAlgorithm} expects pepper but none was provided.");

            plainPassword = _pepper + plainPassword;
        }

        return algo switch {
            "SHA256" => VerifySha256(plainPassword, userHash),
            "BCRYPT" => VerifyBcrypt(plainPassword, userHash),
            "ARGON2ID" => VerifyArgon2id(plainPassword, userHash),
            _ => throw new InvalidOperationException(
                    $"Unsupported hash algorithm variant: {userHash.HashAlgorithm}")
        };
    }

    /*
     * Verify SHA-256 hashed password
     */
    private static bool VerifySha256(string password, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);
        var (computed, _) = HashHelper.HashSha256(password, userHash.Salt);

        return FixedTimeEquals(computed, userHash.Hash);
    }

    /*
     * Calls our helper function to verify a bcrypt hashed password
     */
    private static bool VerifyBcrypt(string password, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);
        return !string.IsNullOrWhiteSpace(userHash.Hash) && BcryptVerify(password, userHash.Hash);
    }

    /*
     * Verify Argon2id hashed password
     */
    private static bool VerifyArgon2id(string password, UserHash userHash) {
        ArgumentNullException.ThrowIfNull(userHash);
        var (computed, _) = HashHelper.HashArgon2id(password, userHash.Salt);

        return FixedTimeEquals(computed, userHash.Hash);
    }

    /*
     * Compares two strings in fixed time to prevent timing attacks.
     */
    private static bool FixedTimeEquals(string left, string right) {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    /*
     * We just call the BCrypt library password verification function
     */
    private static bool BcryptVerify(string password, string hash) {

        return !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(password, hash);
    }
}