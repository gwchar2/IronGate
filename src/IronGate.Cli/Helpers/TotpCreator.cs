using System;
using System.Security.Cryptography;
using System.Text;

namespace IronGate.Cli.Helpers {
    internal static class TotpCreator {

        private const int TimeStepSeconds = 30;
        private const int Digits = 6;

        /*
         * Generates a valid TOTP from the received secret 
         */
        internal static string GenerateCode(string base32Secret) {
            if (string.IsNullOrWhiteSpace(base32Secret))
                throw new ArgumentException($"TOTP secret is required. {nameof(base32Secret)}");

            var key = Base32Decode(base32Secret);

            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timeStep = (ulong)(unixTime / TimeStepSeconds);

            return GenerateTotpCode(key, timeStep, Digits);
        }

        /*
         * Generates the totp code using a base 32 decoded secret, timestep and digits
         */
        private static string GenerateTotpCode(byte[] key, ulong timeStep, int digits) {
            // 8-byte big-endian counter
            Span<byte> counter = stackalloc byte[8];
            for (int i = 7; i >= 0; i--) {
                counter[i] = (byte)(timeStep & 0xFF);
                timeStep >>= 8;
            }

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counter.ToArray());

            var offset = hash[hash.Length - 1] & 0x0F;
            var binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            var otp = binaryCode % (int)Math.Pow(10, digits);
            return otp.ToString(new string('0', digits));
        }


        private static byte[] Base32Decode(string base32) {
            if (base32 == null) throw new ArgumentNullException(nameof(base32));

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
                if (charIndex < 0)
                    throw new FormatException($"Invalid base32 character '{c}'");

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
}
