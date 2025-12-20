using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IronGate.Cli {
    internal class Captcha {
        internal static async Task CaptchaAction(HttpClient http, string[] args) {

            if (args.Length < 3) {
                Printers.PrintHelp();
                return;
            }

            var create = (args[1] ?? string.Empty).Trim().ToLowerInvariant();
            var groupSeed = (args[2] ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(groupSeed) || string.IsNullOrWhiteSpace(create)) {
                Printers.PrintHelp();
                return;
            }

            try {

                var resp = await GetCaptchaTokenAsync(http, groupSeed).ConfigureAwait(false);
                Printers.PrintHttpResult(resp);
                return;
            }catch (Exception ex) {
                Console.WriteLine($"Error: {ex}");
                return;
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
