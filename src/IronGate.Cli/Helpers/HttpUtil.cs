using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#nullable enable


namespace IronGate.Cli.Helpers {

    /*
     * This is a http result class which we use to parse the results retrieved
     */
    internal sealed class HttpCallResult {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public bool IsSuccess => StatusCode >= 200 && StatusCode <= 299;

        public T? TryReadJson<T>(JsonSerializerOptions opts) {
            if (string.IsNullOrWhiteSpace(Body)) return default;
            try { return JsonSerializer.Deserialize<T>(Body, opts); }
            catch { return default; }
        }
    }

    /*
     * Handles the POST/PUT endpoints
     */
    internal static class HttpUtil {
        public static async Task<HttpCallResult> SendJsonAsync(HttpClient http, HttpMethod method, string route, object payload, JsonSerializerOptions jsonOpts) {

            var json = JsonSerializer.Serialize(payload, jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpResponseMessage resp;

            /*
             * We only have Post (login + register), Put, Get (Get is handled in a different task)
             */
            if (method == HttpMethod.Post)
                resp = await http.PostAsync(route, content).ConfigureAwait(false);
            //else if (method == HttpMethod.Put)
            else
                resp = await http.PutAsync(route, content).ConfigureAwait(false);

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
           
            return new HttpCallResult {
                StatusCode = (int)resp.StatusCode,
                ReasonPhrase = resp.ReasonPhrase ?? string.Empty,
                Body = body ?? string.Empty
            };
        }

        /*
         * Handles the GET endpoints (config / captcha)
         */
        internal static async Task<HttpCallResult> GetAsync(HttpClient http,string route,string? groupSeed = null,CancellationToken ct = default) {
            
            var fullRoute = route;
            if (!string.IsNullOrEmpty(groupSeed)) fullRoute += "?groupSeed=" + Uri.EscapeDataString(groupSeed);

            using var resp = await http.GetAsync(fullRoute, ct).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            return new HttpCallResult {
                StatusCode = (int)resp.StatusCode,
                ReasonPhrase = resp.ReasonPhrase ?? string.Empty,
                Body = body ?? string.Empty
            };
        }

        /*
         * Tries to get a certain property from a Json, returns it to a variable named value
         */
        internal static bool TryGetProperty(JsonElement root, string name, out JsonElement value) {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out value))
                return true;
            value = default;
            return false;
        }

    }


}
