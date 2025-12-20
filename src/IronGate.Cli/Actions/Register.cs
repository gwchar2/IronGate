using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IronGate.Cli {
    internal class Register {
        internal static async Task<(bool printHelp, HttpCallResult? http)> RegisterAction(HttpClient http, string[] args) {

            if (args.Length < 3) 
                return (true, null);

            string username = args[1]?.Trim() ?? string.Empty;
            string password = args[2] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return (true, null);

            try {
                var resp = await RegisterAsync(http, username, password).ConfigureAwait(false);
                return (false, resp);
            }catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return (true, null);
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
