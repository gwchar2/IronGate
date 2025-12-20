
using System.Security.Cryptography;
using System.Text;

namespace IronGate.Core.Security.TotpValidator;

/*
 * TOTP Validator
 * timeStep = floor( UnixTimeSeconds / 30 )
 * Compute: HMAC-SHA1(secret, timeStepAsByteArrayBigEndian)
 * Dynamic truncation for the HMAC Result
 * Take binaryCode % 10^digits (digits = 6)
 * Format / return a zero-padded string "012345"
 */

/*
 * On login with TOTP:
 * User sends:
 * username
 * password
 * TOTP code only, e.g. "028394"
 */


public sealed class TotpValidator : ITotpValidator {
    
    private readonly int _timeStepSeconds = 30;
    private readonly int _totpDigits = 6;
    private readonly int _allowedDriftSteps = 1;

    public bool ValidateCode(string secret, string code) {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim();
        if (code.Length != _totpDigits || !code.All(char.IsDigit))
            return false;

        byte[] key;
        try {
            key = Base32Decode(secret);
        }catch {
            return false;
        }

        // Calculate the current time step
        var unixTime  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var currentStep = unixTime / _timeStepSeconds;

        // Check current step and a few steps before and after to allow for clock drift
        for (var stepOffset = -_allowedDriftSteps; stepOffset <= _allowedDriftSteps; stepOffset++) {
            var step = currentStep + stepOffset;

            var expected = GenerateTotpCode(key, (ulong)step, _totpDigits);
            if (FixedTimeEquals(code, expected)) return true;
        }

        return false;

    }

    /*
     * A public generator, called by the CLI
     */
    public string GenerateCode(string secret) {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException($"TOTP secret is required. {nameof(secret)}");

        byte[] key = Base32Decode(secret);

        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var currentStep = (ulong)(unixTime / _timeStepSeconds);

        return GenerateTotpCode(key, currentStep, _totpDigits);
    }
    /*
     * Generates a binary TOTP code string based on the provided key and time step.
     */
    private static string GenerateTotpCode(byte[] key, ulong timeStep, int digits) {

        // 8-Byte big-endian time step
        Span<byte> counter = stackalloc byte[8];
        for (int i = 7; i >= 0; i--) {
            counter[i] = (byte)(timeStep & 0xFF);
            timeStep >>= 8;
        }


        // HMAC-SHA1(key,counter)
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter.ToArray());

        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                       | ((hash[offset + 1] & 0xFF) << 16)
                       | ((hash[offset + 2] & 0xFF) << 8)
                       | (hash[offset + 3] & 0xFF);

        var otp = binaryCode % (int)Math.Pow(10, digits);
        return otp.ToString(new string('0', digits));


    }

    private static bool FixedTimeEquals(string left, string right) {

        if (left is null || right is null) return false;

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        if (leftBytes.Length != rightBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
    
    /*
     * This function decodes a base 32 string and returns a byte array value
     */
    private static byte[] Base32Decode(string base32) {

        ArgumentNullException.ThrowIfNull(base32);

        var sb = new StringBuilder(base32.Length);

        foreach (var ch in base32.ToUpperInvariant()) {
        
            if (ch == '=' || char.IsWhiteSpace(ch)) continue;
            sb.Append(ch);

        }

        var cleaned = sb.ToString();
        if (cleaned.Length == 0) return [];

        const string abc = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var bitBuffer = 0;
        var bitBufferLength = 0;
        var output = new byte[(cleaned.Length * 5 + 7) / 8];
        var outputIndex = 0;

        foreach (var c in cleaned) {
            var charIndex = abc.IndexOf(c);
            if (charIndex < 0) throw new FormatException($"Invalid base 32 character '{c}'");


            bitBuffer = (bitBuffer << 5) | charIndex;
            bitBufferLength += 5;

            if (bitBufferLength >= 8) {
                bitBufferLength -= 8;
                var value = (byte)((bitBuffer >> bitBufferLength) & 0xFF);
                if (outputIndex < output.Length)
                    output[outputIndex++] = value;

            }

        }
        if (outputIndex == output.Length) return output;

        var result = new byte[outputIndex];
        Array.Copy(output, result, outputIndex);
        return result;
    }


}