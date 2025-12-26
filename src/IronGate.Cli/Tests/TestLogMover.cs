using System;
using System.IO;

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

            var targetPath = Path.Combine(targetFolderPath, Path.GetFileName(sourcePath));

            var sourceFull = Path.GetFullPath(sourcePath);
            var targetFull = Path.GetFullPath(targetPath);
            if (string.Equals(sourceFull, targetFull, StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(targetFull))
                File.Delete(targetFull);

            File.Move(sourceFull, targetFull);
        }
    }
}
