using System;
using System.IO;

namespace IronGate.Cli.Tests {

    /*
     * Utility class for resolving important directory paths within the IronGate repository.
     */
    internal static class PathUtil {
        // exeDir = ...\src\IronGate.Cli\bin\Debug\
        internal static string ExeDir => AppContext.BaseDirectory;

        // cliProjectDir = ...\src\IronGate.Cli\
        internal static string CliProjectDir =>
            Path.GetFullPath(Path.Combine(ExeDir, "..", "..", ".."));

        // srcRoot = ...\src\
        internal static string SrcRoot =>
            Path.GetFullPath(Path.Combine(CliProjectDir, ".."));

        // repoRoot = ...\IronGate\
        internal static string RepoRoot =>
            Path.GetFullPath(Path.Combine(SrcRoot, ".."));

        // ...\IronGate\docs\Test Logs\
        internal static string DocsTestLogsRoot =>
            Path.Combine(SrcRoot, "docs", "Test Logs");

        // ...\IronGate\src\IronGate.Api\logs\
        internal static string ApiLogsRoot =>
            Path.Combine(CliProjectDir, "IronGate.Api", "logs");
    }
}
