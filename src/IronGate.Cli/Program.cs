using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IronGate.Cli {
    internal static class Program {

        private const string url = "http://localhost:8080/";


        private const string RegisterRoute = "/api/auth/register";
        private const string ConfigRoute = "/api/config";
        private const string CaptchaRoute = "/api/captcha/token";

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        /*
         * IronGate.Cli Main Entry Point
         * Commands: register, config, captcha, attack
         * Examples: see PrintHelp()
         */
        public static async Task<int> Main(string[] args) {
            try {

                if (args.Length == 0 || IsHelp(args[0])) {
                    PrintHelp();
                    return 0;
                }

                // Replace using declaration with using statement for C# 7.3 compatibility
                using (var http = new HttpClient { BaseAddress = new Uri(url) }) {
                    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var command = args[0].ToLowerInvariant();

                    switch (command)
                    {
                        case "register":
                            return await RegisterAsync(http, args);
                        case "config":
                            return await ConfigAsync(http, args);
                        case "create":
                            return await CaptchaAsync(http, args);
                        case "attack":
                            return AttackStub(args);
                        default:
                            return Unknown(command);
                    }
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
        /*
         * Parses help flags
         */
        private static bool IsHelp(string s)
            => s.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("help", StringComparison.OrdinalIgnoreCase);

        /*
         * Parses unknown command
         */
        private static int Unknown(string cmd) {
            Console.Error.WriteLine($"Unknown command: {cmd}");
            PrintHelp();
            return 2;
        }

        /*
         * Parses register command
         * Uses the register endpoint to create a new user
         */
        private static async Task<int> RegisterAsync(HttpClient http, string[] args) {
            if (args.Length < 3) {
                Console.Error.WriteLine("Usage: register <username> <password>");
                return 2;
            }

            var username = args[1];
            var password = args[2];

            var payload = new { username, password };
            var json = JsonSerializer.Serialize(payload, JsonOpts);
            using (var content = new StringContent(json, Encoding.UTF8, "application/json")) {
                using (var resp = await http.PostAsync(RegisterRoute, content)) {
                    return await PrintResponseAsync(resp);
                }
            }
        }

        /*
         * Parses config command
         * Uses the config endpoint to get or set configuration
         */
        private static async Task<int> ConfigAsync(HttpClient http, string[] args) {
            if (args.Length < 2) {
                Console.Error.WriteLine("Usage: config get | config set <pathToJson>");
                return 2;
            }

            var sub = args[1].ToLowerInvariant();

            if (sub == "get") {
                using (var resp = await http.GetAsync(ConfigRoute)) {
                    return await PrintResponseAsync(resp);
                }
            }

            if (sub == "set") {
                if (args.Length < 3) {
                    Console.Error.WriteLine("Usage: config set <pathToJson>");
                    return 2;
                }

                var filePath = args[2];

                if (!File.Exists(filePath)) {
                    Console.Error.WriteLine($"File not found: {filePath}");
                    return 2;
                }

                var json = File.ReadAllText(filePath);

                // optional quick sanity check: fail fast if invalid JSON
                try { JsonDocument.Parse(json).Dispose(); }
                catch {
                    Console.Error.WriteLine("Invalid JSON file.");
                    return 2;
                }

                using (var content = new StringContent(json, Encoding.UTF8, "application/json")) {
                    // Using PUT as the default for "set"
                    using (var resp = await http.PutAsync(ConfigRoute, content)) {
                        return await PrintResponseAsync(resp);
                    }
                }
            }

            Console.Error.WriteLine("Usage: config get | config set <pathToJson>");
            return 2;
        }

        /*
         * Parses captcha command
         * Uses the captcha endpoint to get a token
         */
        private static async Task<int> CaptchaAsync(HttpClient http, string[] args) {
            if (args.Length < 2) {
                Console.Error.WriteLine("Usage: captcha token");
                return 2;
            }

            var sub = args[1].ToLowerInvariant();

            if (sub == "captcha") {
                using (var resp = await http.GetAsync(CaptchaRoute)) {
                    return await PrintResponseAsync(resp);
                }
            }

            Console.Error.WriteLine("Usage: create captcha");
            return 2;
        }



        /*
         * Parses attack command
         * Attacks the server via Bruteforce or Password spraying (stub for now)
         */
        private static int AttackStub(string[] args) {
            Console.Error.WriteLine("attack is a stub for now.");
            return 2;
        }

        /*
         * Prints the help/usage information
         */
        private static void PrintHelp() {
            Console.WriteLine("IronGate.Cli");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  register <username> <password>");
            Console.WriteLine("  config get");
            Console.WriteLine("  config set <pathToJson>");
            Console.WriteLine("  captcha token");
            Console.WriteLine("  attack (stub)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  IronGate.Cli.exe register alice \"Pass123!\"");
            Console.WriteLine("  IronGate.Cli.exe config get");
            Console.WriteLine("  IronGate.Cli.exe config set .\\config.json");
            Console.WriteLine("  IronGate.Cli.exe captcha token");
        }

        /*
         * Prints the HTTP response
         */
        private static async Task<int> PrintResponseAsync(HttpResponseMessage resp) {
            var body = await resp.Content.ReadAsStringAsync();

            Console.WriteLine($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");

            if (!string.IsNullOrWhiteSpace(body)) {
                if (TryPrettyPrintJson(body, out var pretty))
                    Console.WriteLine(pretty);
                else
                    Console.WriteLine(body);
            }

            return resp.IsSuccessStatusCode ? 0 : 3;
        }

        /*
         * Tries to pretty-print JSON text
         */
        private static bool TryPrettyPrintJson(string text, out string pretty) {
            pretty = string.Empty;

            var trimmed = text.TrimStart();
            if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
                return false;

            try {
                using (var doc = JsonDocument.Parse(text)) {
                    pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions {
                        WriteIndented = true
                    });
                    return true;
                }
            }
            catch {
                return false;
            }
        }

    }
}