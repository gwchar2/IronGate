using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IronGate.Cli {
    /*
     * This class handles all the config requests
     */
    internal class Config {
        internal static async Task<(bool printHelp, HttpCallResult? http)> ConfigAction(HttpClient http, string[] args) {

            if (args.Length < 2)
                return (true, null);

            var cmd = (args[1] ?? string.Empty).Trim().ToLowerInvariant();

            try {
                switch (cmd) {
                    case "get": {
                            var resp = await GetConfigAsync(http);
                            return (false, resp);
                        }
                    case "set": {

                            if (args.Length < 3)
                                return (true, null);

                            var configFile = ( args[2] ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(configFile))
                                return (true, null);

                            if (!File.Exists(configFile))
                                return (true, null);

                            using var streamReader = new StreamReader(configFile, Encoding.UTF8);
                            string jsonText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                            
                            if (string.IsNullOrWhiteSpace(jsonText))
                                return (true, null);

                            using var doc = JsonDocument.Parse(jsonText);
                            var payload = doc.RootElement.Clone();

                            var resp = await SetConfigAsync(http, payload).ConfigureAwait(false);

                            return (false, resp);
                        }
                    default:
                        return (true, null);
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return (true, null);
            }
        }

        /*
         * This function gets the current DB defense configuration, uses the HTTP Util sender. basically this is just a wrapper...
         */
        internal static async Task<HttpCallResult> GetConfigAsync(HttpClient http, CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var resp = await HttpUtil.GetAsync(http, Defaults.ConfigRoute).ConfigureAwait(false);

            return resp;

        }

        /*
         * This class is a wrapper for the HTTP Utils, it sets a current DB Defensive configuration.
         */
        internal static async Task<HttpCallResult> SetConfigAsync(HttpClient http, object payload, CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var resp = await HttpUtil.SendJsonAsync(http, HttpMethod.Put, Defaults.ConfigRoute, payload, Defaults.JsonOpts).ConfigureAwait(false);
            return resp;

        }

        internal static AuthConfigDto? TryReadConfig(HttpCallResult resp) {
            if (string.IsNullOrWhiteSpace(resp.Body)) return null;
            try {
                return JsonSerializer.Deserialize<AuthConfigDto>(resp.Body, Defaults.JsonOpts);
            }
            catch {
                return null;
            }
        }

    }
}
