using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace IronGate.Cli.Attacks {
    
    // attack brute-force <username> 
    //  attack spray <usernamesFile> 
    internal class Attack {

        internal static async Task<(bool printHelp, HttpCallResult? http)> AttackAction(HttpClient http, string[] args) {
            
            if (args.Length < 3) 
                return (true, null);

            var mode = (args[1] ?? string.Empty).Trim().ToLowerInvariant();

            try {
                var confResp = await Config.GetConfigAsync(http).ConfigureAwait(false);
                var config = Config.TryReadConfig(confResp);

                if (config is null) {
                    Console.WriteLine("Failed to read the server configuration!");
                    return (true, null);
                }

                var seed = LoadUserSeed(Defaults.UserSeed);

                switch (mode) {

                    case "brute-force": {
                            var username = (args[2] ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(username))
                                return (true, null);

                            await BruteForce.RunAsync(http, config, seed, username).ConfigureAwait(false);

                            return (false, null);
                        }

                    case "spray": {
                            var usernamesFile = (args[2] ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(usernamesFile) || !File.Exists(usernamesFile)) {
                                Console.WriteLine($"Usernames file not found: {usernamesFile}");
                                return (true, null);
                            }

                            //await RunSprayAsync(http, config, seed, usernamesFile, Defaults.RockYou, maxHttpAttempts, maxRunTime).ConfigureAwait(false);
                            return (false, null);
                        }

                    default:
                        return (true, null);
                }
            }catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex}");
                return (false, null);
            }

        }
        private static UserSeed LoadUserSeed(string path) {
            var seed = new UserSeed();

            var full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if (!File.Exists(full))
                return seed;

            try {
                var json = File.ReadAllText(full, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                    return seed;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object) {
                    if (TryGetString(root, "seed_group", out var sg))
                        seed.GroupSeed = sg;

                    if (root.TryGetProperty("users", out var users) && users.ValueKind == JsonValueKind.Array) {
                        foreach (var u in users.EnumerateArray())
                            ReadUserEntry(seed, u);
                    }
                } else if (root.ValueKind == JsonValueKind.Array) {
                    foreach (var u in root.EnumerateArray())
                        ReadUserEntry(seed, u);
                }
            }
            catch {
                // ignore
            }

            return seed;
        }
        private static bool TryGetString(JsonElement root, string name, out string? value) {
            value = null;
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (!root.TryGetProperty(name, out var p)) return false;
            if (p.ValueKind != JsonValueKind.String) return false;
            value = p.GetString();
            return !string.IsNullOrWhiteSpace(value);
        }
        private static void ReadUserEntry(UserSeed seed, JsonElement u) {
            if (u.ValueKind != JsonValueKind.Object) return;

            if (!TryGetString(u, "username", out var username) || string.IsNullOrWhiteSpace(username))
                return;

            if (TryGetString(u, "secret_totp", out var secret) && !string.IsNullOrWhiteSpace(secret))
                seed.TotpSecrets[username!] = secret!;
        }

    }
}
