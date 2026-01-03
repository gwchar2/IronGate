using IronGate.Cli.Attacks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    /*
     * Base class for test cases
     */
    internal abstract class TestCaseBase : ITestCase {
        public abstract string Name { get; }

        protected abstract string TestFolderName { get; }

        public string TargetFolder => Path.Combine(PathUtil.DocsTestLogsRoot, TestFolderName);

        public virtual string ConfigToLoad => Path.Combine(TargetFolder, "config.json");

        protected const int DefaultDelayMs = 5000;
        protected const int DefaultThreadAmount = 10;

        public abstract Task RunAsync(HttpClient http);


        /*
         * Apply the DB Configuration for the test
         */
        protected async Task<bool> ApplyConfigAsync(HttpClient http) {
            Console.WriteLine($"Sending config: {ConfigToLoad}");
            (_, var resp) = await Config.ConfigAction(http, ["config", "set", ConfigToLoad]).ConfigureAwait(false);
            var ok = resp != null;
            if (!ok)
                Console.WriteLine("Failed to send configuration for test");
            return ok;
        }

        /*
         * Generate user names with a given prefix
         */
        protected static IEnumerable<string> Users(string prefix) {
            for (int i = 2; i <= 10; i++)
                yield return $"{prefix}_{i:00}";
        }

        /*
         * Run brute-force attack for each user in the list
         */
        protected async Task RunBruteForceAsync(HttpClient http, IEnumerable<string> users, int delayMs = DefaultDelayMs) {
            foreach (var user in users) {
                var subFolder = Path.Combine(TargetFolder, user);

                Console.WriteLine($"Attacking: {user}");
                await Task.Delay(delayMs).ConfigureAwait(false);
                _ = await Attack.AttackAction(http, ["attack", "brute-force", user]).ConfigureAwait(false);

                Console.WriteLine($"Moving files to: {subFolder}");
                TestLogMover.MoveFiles(subFolder);

                if (delayMs > 0)
                    await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }

        /*
         * Run password spray attack using the usernames file
         */
        protected async Task RunSprayAsync(HttpClient http,string subFolderName,int delayMs = DefaultDelayMs) {
            var subFolder = Path.Combine(TargetFolder, subFolderName);
            var usernamesFile = Path.Combine(PathUtil.ExeDir, "usernames.txt");

            _ = await Attack.AttackAction(http, ["attack", "spray", usernamesFile, DefaultThreadAmount.ToString()]).ConfigureAwait(false);

            TestLogMover.MoveFiles(subFolder);

            if (delayMs > 0)
                await Task.Delay(delayMs).ConfigureAwait(false);
        }
    }
}
