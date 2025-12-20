using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IronGate.Cli {
    internal class Register {
        internal static async Task RegisterAction(HttpClient http, string[] args) {

            if (args.Length < 3) {
                Printers.PrintHelp();
                return;
            }

            string username = args[1]?.Trim() ?? string.Empty;
            string password = args[2] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
                Printers.PrintHelp();
                return;
            }

            try {
                var resp = await RegisterAsync(http, username, password).ConfigureAwait(false);
                Printers.PrintHttpResult(resp);
            }catch (Exception ex) {
                Console.WriteLine($"Error: {ex}");
            }

        }


        internal static async Task<HttpCallResult> RegisterAsync(HttpClient http, string username, string password, CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var payload = new {
                username,
                password
            };

            var resp = await HttpUtil.SendJsonAsync(http, HttpMethod.Post, Defaults.RegisterRoute, payload, Defaults.JsonOpts).ConfigureAwait(false);

            return resp;

        }

    }
}
