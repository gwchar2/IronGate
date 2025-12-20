using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IronGate.Cli {
    internal class Captcha {
        internal static async Task<(bool printHelp, HttpCallResult? http)> CaptchaAction(HttpClient http, string[] args) {

            if (args.Length < 3) {
                //Printers.PrintHelp();
                return (true, null);
            }

            var create = (args[1] ?? string.Empty).Trim().ToLowerInvariant();
            var groupSeed = (args[2] ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(groupSeed) || string.IsNullOrWhiteSpace(create)) {
                //Printers.PrintHelp();
                return (true, null);
            }

            try {

                var resp = await GetCaptchaTokenAsync(http, groupSeed).ConfigureAwait(false);
                //Printers.PrintHttpResult(resp);
                return (false, resp);
            }catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return (true, null);
            }
        }

        /*
         * A wrapper for the HTTPUtil sender for captcha token
         */
        internal static async Task<HttpCallResult> GetCaptchaTokenAsync(HttpClient http, string groupSeed, CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var resp = await HttpUtil.GetAsync(http, Defaults.CaptchaRoute, groupSeed, ct).ConfigureAwait(false);

            return resp;
        }
        internal static bool TryReadCaptcha(HttpCallResult resp, out CaptchaTokenResponse? attempt) {
            attempt = null;
            if (string.IsNullOrWhiteSpace(resp.Body)) return false;
            try {
                attempt = JsonSerializer.Deserialize<CaptchaTokenResponse>(resp.Body, Defaults.JsonOpts);
                return attempt != null;
            }
            catch {
                return false;
            }
        }
        /*
         * Used to parse the Captcha result returned from the endpoint
         */
        internal static bool TryGetCaptchaToken(HttpCallResult resp, out string? token) {
            token = null;

            if (string.IsNullOrWhiteSpace(resp.Body))
                return false;

            try {
                using var doc = JsonDocument.Parse(resp.Body);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object) {
                    if (root.TryGetProperty("captchaToken", out var p) && p.ValueKind == JsonValueKind.String) {
                        token = p.GetString();
                        return !string.IsNullOrWhiteSpace(token);
                    }
                }

                if (root.ValueKind == JsonValueKind.String) {
                    token = root.GetString();
                    return !string.IsNullOrWhiteSpace(token);
                }

                return false;
            }
            catch {
                return false;
            }
        }
    }
}
