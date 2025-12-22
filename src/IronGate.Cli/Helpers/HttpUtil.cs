using IronGate.Cli.Constants;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#nullable enable


namespace IronGate.Cli.Helpers {
    /*
     * Handles the GET / POST endpoints, and the parsing of certain DTO's
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

        /*
         * Tries to parse the response into an AuthAttemptDto out variable named attempt
         */
        internal static bool TryReadAuthAttempt(HttpCallResult resp, out AuthAttemptDto? attempt) {
            attempt = null;
            if (string.IsNullOrWhiteSpace(resp.Body)) return false;
            try {
                attempt = JsonSerializer.Deserialize<AuthAttemptDto>(resp.Body, Defaults.JsonOpts);
                return attempt != null;
            }
            catch {
                return false;
            }
        }

        /*
         * Used to parse AuthAttemptDto result returned from teh endpoint
         */
        internal static bool TryGetAuthResult(HttpCallResult resp, out AuthResultCode result) {
            result = default;

            if (string.IsNullOrWhiteSpace(resp.Body))
                return false;

            try {
                using var doc = JsonDocument.Parse(resp.Body);
                var root = doc.RootElement;

                if (!HttpUtil.TryGetProperty(root, "result", out var res))
                    return false;

                if (res.ValueKind == JsonValueKind.Number) {
                    if (res.TryGetInt32(out var val)) {
                        result = (AuthResultCode)val;
                        return true;
                    }
                    return false;
                }

                if (res.ValueKind == JsonValueKind.String) {
                    var strRes = res.GetString();
                    if (string.IsNullOrWhiteSpace(strRes)) return false;

                    if (Enum.TryParse<AuthResultCode>(strRes, ignoreCase: true, out var parsed)) {
                        result = parsed;
                        return true;
                    }
                    return false;
                }
                return false;
            }
            catch {
                return false;
            }
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
