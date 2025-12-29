using System;
using System.IO;
using System.Threading;

namespace IronGate.Cli.Tests {
    internal static class TestLogMover {

        /*
         *  Moves the log files generated during tests to the specified target folder. 
         */
        internal static void MoveFiles(string targetFolderPath) {
            Directory.CreateDirectory(targetFolderPath);

            var dayFolder = DateTime.Today.ToString("yyyy-MM-dd");

            var resourcesPath = Path.Combine(PathUtil.ApiLogsRoot, dayFolder, "resources.jsonl");
            var attemptsPath = Path.Combine(PathUtil.ApiLogsRoot, dayFolder, "attempts.jsonl");

            var bruteForceLogPath = Path.Combine(PathUtil.ExeDir, "brute_force_log.jsonl");
            var sprayLogPath = Path.Combine(PathUtil.ExeDir, "spray_log.jsonl");
            Console.WriteLine($"Target folder: {targetFolderPath}");
            Console.WriteLine($"Day folder: {dayFolder}");
            Console.WriteLine($"Resources path: {resourcesPath}");
            Console.WriteLine($"attempts path: {attemptsPath}");
            Console.WriteLine($"BruteForceLog path: {bruteForceLogPath}");
            Console.WriteLine($"Spraylog path: {sprayLogPath}");
            MoveIfExists(resourcesPath, targetFolderPath);
            MoveIfExists(attemptsPath, targetFolderPath);
            MoveIfExists(bruteForceLogPath, targetFolderPath);
            MoveIfExists(sprayLogPath, targetFolderPath);
        }


        /*
         * Moves a file from sourcePath to targetFolderPath if it exists.
         */
        private static void MoveIfExists(string sourcePath, string targetFolderPath) {
            if (!File.Exists(sourcePath))
                return;

            Directory.CreateDirectory(targetFolderPath);

            var targetPath = Path.Combine(targetFolderPath, Path.GetFileName(sourcePath));

            var sourceFull = Path.GetFullPath(sourcePath);
            var targetFull = Path.GetFullPath(targetPath);

            if (string.Equals(sourceFull, targetFull, StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(targetFull))
                File.Delete(targetFull);

            WaitUntilFileIsFree(sourceFull, timeoutMs: 10_000, pollMs: 200);
            File.Move(sourceFull, targetFull);
        }
        private static void WaitUntilFileIsFree(string path, int timeoutMs = 10_000, int pollMs = 200) {
            var start = Environment.TickCount;

            while (true) {
                if (IsFileFree(path))
                    return;

                if (Environment.TickCount - start >= timeoutMs)
                    throw new IOException($"Timed out waiting for file to be released: {path}");

                Thread.Sleep(pollMs);
            }
        }

        private static bool IsFileFree(string path) {
            try {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return true;
            }
            catch (IOException) {
                return false;
            }
            catch (UnauthorizedAccessException) {
                return false;
            }
        }

    }
}
