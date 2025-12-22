using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace IronGate.Cli {
    /*
     * This class handles all the captcha endpoints & parsing
     */
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
        
    }
}
