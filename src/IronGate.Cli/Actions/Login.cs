
using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static IronGate.Cli.BruteForce;
#nullable enable


namespace IronGate.Cli {
    // login <username> <password> <totp> <captcha>
    // login <username> <password> <totp> -
    // login <username> <password> - <captcha>
    /*
     * This class handles ALL the different login requests
     */
    internal static class Login {

        /*
         * This task handles the parsing of Login action initiated by user, or by an attack
         */
        internal static async Task<(bool printHelp, HttpCallResult? http)> 
            LoginAction(HttpClient http, string[] args, string groupSeed = "", string totpSec = "", Counter? httpAttempts = null, StreamWriter? log = null) {

            if (args.Length < 5)
                return (true, null);

            var username = (args[1] ?? string.Empty).Trim();
            var password = (args[2] ?? string.Empty).Trim();
            var totpArg = (args[3] ?? string.Empty).Trim();
            var captchaArg = (args[4] ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) 
                return (true, null);

            string? totpSecret = IsDash(totpArg) ? null : totpArg;
            string? captcha = IsDash(captchaArg) ? null : captchaArg;

            // We put the totpSecret from CLI if it isnt null, if it is, than either totpSec or null
            var secret = !string.IsNullOrWhiteSpace(totpSecret) ?
                totpSecret : (!string.IsNullOrWhiteSpace(totpSec) ? totpSec : null);

            /*
             * Flow of command is:
             * 1. Try to login normally
             * 2. Parse the response and send with required options later
             */
            try {
                var resp = await LoginAsync(http, username, password).ConfigureAwait(false);
                CountAndLog(httpAttempts, log, "login", username, password, resp);
                
                // Get the auth result for the login action
                if (!HttpUtil.TryGetAuthResult(resp, out var response))
                    return (false, resp);
                
                // According to the response we received, we parse. Technically we only need to
                // parse the captcha required or totp required, for the rest we print normally.
                if (response == AuthResultCode.CaptchaRequired) {
                    // If we need a captcha and we didn't receive one manually
                    if (string.IsNullOrWhiteSpace(captcha)) {
                        // Printers.PrintHttpResult(resp);
                        if (!string.IsNullOrWhiteSpace(groupSeed)) {

                            var captchaResp = await Captcha.GetCaptchaTokenAsync(http, groupSeed).ConfigureAwait(false);
                            CountAndLog(httpAttempts, log, "captcha_request", username, password, captchaResp);

                            if (!HttpUtil.TryGetCaptchaToken(captchaResp, out var token) || string.IsNullOrWhiteSpace(token)) 
                                return (false, resp);
                            captcha = token;
                        }
                        else 
                            return (false, resp);
                    }

                    var resp2 = await LoginWithCaptchaAsync(http, username, password, captcha!).ConfigureAwait(false);
                    CountAndLog(httpAttempts, log, "login_with_captcha", username, password, resp2);
                    
                    if (!HttpUtil.TryGetAuthResult(resp2, out var response2)) 
                        return (false, resp2);

                    // If now the captcha also wants a totp (the action filter comes first)
                    if (response2 == AuthResultCode.TotpRequired && !string.IsNullOrWhiteSpace(secret)) {

                        var resp3 = await LoginTotpAsync(http, username, password, TotpCreator.GenerateCode(secret), captcha!).ConfigureAwait(false);
                        CountAndLog(httpAttempts, log, "login_captcha_with_totp", username, password, resp3);
                        
                        return (false, resp3);
                    }

                    return (false, resp2);
                }

                // If we require a TOTP
                if (response == AuthResultCode.TotpRequired) {

                    // If the secret is null -> return false.
                    if (string.IsNullOrWhiteSpace(secret))
                        return (false, resp);

                    //Console.WriteLine($"Token created: {token}");
                    HttpCallResult resp2;
                    if (!string.IsNullOrWhiteSpace(captcha)) {
                        resp2 = await LoginTotpAsync(http, username, password, TotpCreator.GenerateCode(secret), captcha!).ConfigureAwait(false);
                        CountAndLog(httpAttempts, log, "login_with_totp_and_captcha", username, password, resp2);
                    }
                    else {
                        resp2 = await LoginTotpAsync(http, username, password, TotpCreator.GenerateCode(secret)).ConfigureAwait(false);
                        CountAndLog(httpAttempts, log, "login_totp", username, password, resp2);
                    }

                    return (false, resp2);
                }
                // If it isn't one of the special cases...(It is either a success or fail with specific reason)
                return (false, resp);
            }catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return (true, null);
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
         * When we call the LoginAction from a brute force or from a password spray attack,
         * We still need to log the http attempts in the log file, and to increment the http attempts counter!
         */
        private static void CountAndLog(Counter? httpAttempts, StreamWriter? log, string phase, string username, string password, HttpCallResult resp) {

            if (httpAttempts != null)
                httpAttempts.Value++;

            if (log is null)
                return;

            if (phase == "captcha_request"){
                //We try to parse the Captcha response
                if (HttpUtil.TryReadCaptcha(resp, out var cap) && cap != null) {
                    Printers.WriteJsonl(log, new {
                        attackType = "brute-force",
                        attackTimeUtc = DateTimeOffset.UtcNow,
                        attemptNumber = httpAttempts?.Value,
                        phase,
                        username,
                        password,
                        httpStatus = resp.StatusCode,
                        captcha = cap
                    });
                    return;
                }
            }

            // We try to parse the basic AuthAttemptDto
            if (HttpUtil.TryReadAuthAttempt(resp, out var attempt) && attempt != null) {
                Printers.WriteJsonl(log, new {
                    attackType = "brute-force",
                    attackTimeUtc = DateTimeOffset.UtcNow,
                    attemptNumber = httpAttempts?.Value,
                    phase,                 
                    username,
                    password,
                    httpStatus = resp.StatusCode,
                    attempt
                });
                return;
            }

            // If nothing worked........................
            Printers.WriteJsonl(log, new {
                attackType = "brute-force",
                attackTimeUtc = DateTimeOffset.UtcNow,
                attemptNumber = httpAttempts?.Value,
                phase,
                username,
                password,
                httpStatus = resp.StatusCode,
                rawBody = resp.Body
            });
        }
    }
}
 