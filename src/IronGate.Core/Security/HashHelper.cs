
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace IronGate.Core.Security;

/*
 * This class is in charge of password hashing functions.
 */
public static class HashHelper {

    /* 
     * Generates a random salt of specified size in bytes, returned as Base64 string.
     */
    public static string GenerateSalt(int sizeBytes = 16) {
        var bytes = new byte[sizeBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /*
     * Generates SHA-256 + per-user salt
     * Returns (hashBase64, saltBase64)
     */
    public static (string Hash, string Salt) HashSha256(string password, string? existingSaltBase64 = null) {
        var saltBase64 = existingSaltBase64 ?? GenerateSalt(16);
        var saltBytes = Convert.FromBase64String(saltBase64);

        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var input = new byte[saltBytes.Length + passwordBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, input, 0, saltBytes.Length);
        Buffer.BlockCopy(passwordBytes, 0, input, saltBytes.Length, passwordBytes.Length);
        var hashBytes = SHA256.HashData(input);
        var hashBase64 = Convert.ToBase64String(hashBytes);

        return (hashBase64, saltBase64);
    }


    /*
     * Creates a bcrypt hash of the password.
     */
    public static (string Hash, string Salt) HashBcrypt(string password, int workFactor = 12) {

        // BCrypt creates its own salt, we will use it and store it as the given salt string.
        var salt = BCrypt.Net.BCrypt.GenerateSalt(workFactor);
        var hash = BCrypt.Net.BCrypt.HashPassword(password, salt);
        return (hash, salt);
    }

    /*
     * Creates a Argon2id hash of the password.
     * Uses parameters: time = 1, memory = 64 MB, parallelism = 1
     * Returns (hashBase64, saltbase64)
     */
    public static (string Hash, string Salt) HashArgon2id(string password, string? existingSaltBase64 = null) {
        var saltBase64 = existingSaltBase64 ?? GenerateSalt(16); // 128-bit salt is plenty
        var saltBytes = Convert.FromBase64String(saltBase64);

        var passwordBytes = Encoding.UTF8.GetBytes(password);

        using var argon2 = new Argon2id(passwordBytes) {
            Iterations = 1,
            MemorySize = 64 * 1024,
            DegreeOfParallelism = 1,
            Salt = saltBytes
        };

        var hashBytes = argon2.GetBytes(32);
        var hashBase64 = Convert.ToBase64String(hashBytes);

        return (hashBase64, saltBase64);
    }
}
