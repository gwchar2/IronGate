
using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace IronGate.Cli {
    // login <username> <password> <totp> <captcha>
    // login <username> <password> <totp> -
    // login <username> <password> - <captcha>
    internal static class Login {
        internal static async Task LoginAction(HttpClient http, string[] args) {

            if (args.Length < 5) {
                Printers.PrintHelp();
                return;
            }

            var username = (args[1] ?? string.Empty).Trim();
            var password = (args[2] ?? string.Empty).Trim();
            var totpArg = (args[3] ?? string.Empty).Trim();
            var captchaArg = (args[4] ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
                Printers.PrintHelp();
                return;
            }

            string? totpSecret = IsDash(totpArg) ? null : totpArg;
            string? captcha = IsDash(captchaArg) ? null : captchaArg;

            /*
             * Flow of command is:
             * 1. Try to login normally
             * 2. Parse the response and send with required options later
             */
            try {
                var resp = await LoginAsync(http, username, password).ConfigureAwait(false);

                // Get the auth result for the login action
                if (!TryGetAuthResult(resp, out var response)) {
                    Printers.PrintHttpResult(resp);
                    return;
                }

                // According to the response we received, we parse. Technically we only need to
                // parse the captcha required or totp required, for the rest we print normally.
                if (response == AuthResultCode.CaptchaRequired) {
                    // If we need a captcha and we didn't receive one manually
                    if (string.IsNullOrWhiteSpace(captcha)) {
                        Printers.PrintHttpResult(resp);
                        return;
                    }

                    var resp2 = await LoginWithCaptchaAsync(http, username, password, captcha).ConfigureAwait(false);
                        
                    if (!TryGetAuthResult(resp2, out var response2)) {
                        Printers.PrintHttpResult(resp2);
                        return;
                    }

                    // If now the captcha also wants a totp (the action filter comes first)
                    if (response2 == AuthResultCode.TotpRequired && !string.IsNullOrWhiteSpace(totpSecret)) {
                        var resp3 = await LoginTotpAsync(http, username, password, TotpCreator.GenerateCode(totpSecret), captcha).ConfigureAwait(false);
                        Printers.PrintHttpResult(resp3);
                        return;
                    }

                    Printers.PrintHttpResult(resp2);
                    return;
                }

                // If we require a TOTP
                if (response == AuthResultCode.TotpRequired) {

                    if (string.IsNullOrWhiteSpace(totpSecret)) {
                        Printers.PrintHttpResult(resp);
                        return;
                    }

                    // We just send the request wit ha captcha if its available, since the endpoint will just ignore the captcha if it isnt required
                    var token = TotpCreator.GenerateCode(totpSecret);
                    Console.WriteLine($"Token created: {token}");
                    HttpCallResult resp2;
                    if (!string.IsNullOrWhiteSpace(captcha)) 
                        resp2 = await LoginTotpAsync(http, username, password, token, captcha).ConfigureAwait(false);
                    else 
                        resp2 = await LoginTotpAsync(http, username, password, token).ConfigureAwait(false);
                    

                    Printers.PrintHttpResult(resp2);
                    return;
                }
                
                // If it isn't one of the special cases...(It is either a success or fail with specific reason)
                Printers.PrintHttpResult(resp);

            }catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }

        }
        private static bool IsDash(string s)
            => string.Equals(s, "-", StringComparison.Ordinal);

        /*
         * Regular login request
         */
        internal static async Task<HttpCallResult> LoginAsync(HttpClient http, string username, string password, CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var payload = new {
                username,
                password
            };

            return await HttpUtil.SendJsonAsync(http, HttpMethod.Post, Defaults.LoginRoute, payload, Defaults.JsonOpts).ConfigureAwait(false);

        }

        /*
         * Login + Captcha request
         */
        internal static async Task<HttpCallResult> LoginWithCaptchaAsync(HttpClient http, string username, string password, string captchaToken, CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var payload = new {
                username,
                password,
                captchaToken,
            };

            return await HttpUtil.SendJsonAsync(http, HttpMethod.Post, Defaults.LoginRoute, payload, Defaults.JsonOpts).ConfigureAwait(false);
        }
        /*
         * Login + Totp + Captcha request
         */
        internal static async Task<HttpCallResult> LoginTotpAsync(HttpClient http, string username, string password, string totpCode, string captchaToken = "", CancellationToken ct = default) {

            ct.ThrowIfCancellationRequested();

            var payload = new {
                username,
                password,
                totpCode,
                captchaToken
            };

            return await HttpUtil.SendJsonAsync(http, HttpMethod.Post, Defaults.LoginTotpRoute, payload, Defaults.JsonOpts).ConfigureAwait(false);
        }

        /*
         * Used to parse AuthAttemptDto result returned from teh endpoint
         */
        private static bool TryGetAuthResult(HttpCallResult resp, out AuthResultCode result) {
            result = default;

            if (string.IsNullOrWhiteSpace(resp.Body))
                return false;

            try {
                using var doc = JsonDocument.Parse(resp.Body);
                var root = doc.RootElement;

                if (!TryGetProperty(root, "result", out var res))
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
            }catch {
                return false;
            }
        }

        /*
         * Tries to get a certain property from a Json, returns it to a variable names value
         */
        private static bool TryGetProperty(JsonElement root, string name, out JsonElement value) {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out value))
                return true;
            value = default;
            return false;
        }

    }
}
 