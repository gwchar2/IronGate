using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
#nullable enable

namespace IronGate.Cli.Tests {
    internal static class TestLogMover {

        /*
         *  Moves the log files generated during tests to the specified target folder. 
         */
        internal static void MoveFiles(string targetFolderPath) {
            Directory.CreateDirectory(targetFolderPath);

            var dayFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");

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
            Directory.CreateDirectory(targetFolderPath);

            // If it doesn't exist, nothing to do (maybe already rotated)
            if (!File.Exists(sourcePath))
                return;

            // Rotate-by-rename
            var rotatedPath = TryRotate(sourcePath, tries: 10, delayMs: 150);
            if (rotatedPath is null)
                return; // couldn't rotate (still being opened, or disappeared) -> skip

            // Move the rotated file to the target folder
            var targetPath = Path.Combine(targetFolderPath, Path.GetFileName(rotatedPath));

            var targetFull = Path.GetFullPath(targetPath);
            if (File.Exists(targetFull))
                File.Delete(targetFull);

            File.Move(rotatedPath, targetFull);
        }

        private static string? TryRotate(string sourcePath, int tries, int delayMs) {
            var dir = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(dir))
                return null;

            var baseName = Path.GetFileNameWithoutExtension(sourcePath);
            var ext = Path.GetExtension(sourcePath);

            for (int i = 0; i < tries; i++) {
                // File might disappear between attempts
                if (!File.Exists(sourcePath))
                    return null;

                var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fffffff");
                var rotatedName = $"{baseName}.{stamp}.{Process.GetCurrentProcess().Id}{ext}";
                var rotatedPath = Path.Combine(dir, rotatedName);

                try {
                    File.Move(sourcePath, rotatedPath);
                    return rotatedPath;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                    Thread.Sleep(delayMs);
                }
            }

            return null;
        }


    }
}
