using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IronGate.Cli {
    internal class Config {
        internal static async Task ConfigAction(HttpClient http, string[] args) {

            if (args.Length < 2) {
                Printers.PrintHelp();
                return;
            }

            var cmd = (args[1] ?? string.Empty).Trim().ToLowerInvariant();

            try {
                switch (cmd) {
                    case "get": {
                            var resp = await GetConfigAsync(http);
                            Printers.PrintHttpResult(resp);
                            return;
                        }
                    case "set": {

                            if (args.Length < 3) {
                                Printers.PrintHelp();
                                return;
                            }

                            var configFile = ( args[2] ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(configFile)){
                                Printers.PrintHelp();
                                return;
                            }

                            if (!File.Exists(configFile)) {
                                Console.WriteLine($"Config file does not exist in the path {configFile}");
                                return;
                            }

                            using var streamReader = new StreamReader(configFile, Encoding.UTF8);
                            string jsonText = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                            
                            if (string.IsNullOrWhiteSpace(jsonText)) {
                                Console.WriteLine("Config file is empty");
                                return;
                            }

                            using var doc = JsonDocument.Parse(jsonText);
                            var payload = doc.RootElement.Clone();

                            var resp = await SetConfigAsync(http, payload).ConfigureAwait(false);

                            Printers.PrintHttpResult(resp);
                            return;
                        }
                    default:
                        Printers.PrintHelp();
                        return;
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
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
    }
}
