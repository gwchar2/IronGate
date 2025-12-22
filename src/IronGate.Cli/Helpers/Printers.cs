using IronGate.Cli.Constants;
using System;
using System.IO;
using System.Text.Json;

namespace IronGate.Cli.Helpers {
    internal class Printers {

        /*
         * A private serializer with WriteIndented = true.
         * The other serializer in defaults will write compacted JSON for attack log purposes mostly.
         */
        private static readonly JsonSerializerOptions PrettyJsonOpts =
            new(JsonSerializerDefaults.Web) { WriteIndented = true };

        internal static void PrintHelp() {
            Console.WriteLine("IronGate.Cli");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  register <username> <password>");
            Console.WriteLine("  config get");
            Console.WriteLine("  config set <pathToJson>");
            Console.WriteLine("  captcha create <groupSeed>");
            Console.WriteLine("  login <username> <password> <totp_secret> <captcha>");
            Console.WriteLine("  login <username> <password> <totp_secret> -");
            Console.WriteLine("  login <username> <password> - <captcha>");
            Console.WriteLine("  attack brute-force <username> ");
            Console.WriteLine("  attack spray <usernamesFile> <passwordFile> <thread_amount>");
            Console.WriteLine("Note: All attacks are executed until either the limit or a success is reached.");
            Console.WriteLine();
            Console.WriteLine("Defaults:");
            Console.WriteLine($"  Default limit per run: {Defaults.DefaultLimit}");
            Console.WriteLine($"  Hard limit:            {Defaults.HardLimit}");
            Console.WriteLine($"  Time limit (seconds):  { Defaults.TimeLimitSeconds}");
            Console.WriteLine();
            Console.WriteLine("Files:");
            Console.WriteLine($"  Users seed: {Defaults.UserSeed}");
            Console.WriteLine($"  Wordlist:   {Defaults.RockYou}");
        }

        public static void PrintHttpResult(HttpCallResult resp) {
            Console.WriteLine($"HTTP {resp.StatusCode} {resp.ReasonPhrase}");

            if (string.IsNullOrWhiteSpace(resp.Body)) return;

            if (TryFormatJson(resp.Body, out var prettyPrint))
                Console.WriteLine(prettyPrint); 
            else
                Console.Write(resp.Body);
        }

        private static bool TryFormatJson(string body, out string prettyPrint) {
            try {
                using var doc = JsonDocument.Parse(body);
                prettyPrint = JsonSerializer.Serialize(doc.RootElement, PrettyJsonOpts);
                return true;
            }catch {
                prettyPrint = string.Empty;
                return false;
            }
        }

        internal static void WriteJsonl(StreamWriter w, object obj) {
            var line = JsonSerializer.Serialize(obj, Defaults.JsonOpts);
            w.WriteLine(line);
            w.Flush();
        }
    }
}
