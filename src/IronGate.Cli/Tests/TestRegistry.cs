using IronGate.Cli.Constants;
using IronGate.Cli.Helpers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests
{
    internal static class TestRegistry
    {
        internal static IReadOnlyList<ITestCase> All { get; } =
        [
            new Test01(),
            new Test02(),
            new Test03(),
            new Test04(),
            new Test05(),
            new Test06(),
            new Test07(),
            new Test08(),
            new Test09(),
            new Test10(),
            new Test11(),
            new Test12(),
            new Test13(),
            new Test14(),
            new Test15(),
            new Test16(),
            new Test17(),
            new Test18(),
            new Test19(),
            new Test20(),
            new Test21(),
            new Test22(),
            new Test23(),
        ];
    }

    internal static class TestRunner
    {
        internal static async Task RunAllAsync(HttpClient http)
        {

            foreach (var t in TestRegistry.All)
            {
                await HttpUtil.ResetUserStatesAsync(http, Defaults.JsonOpts).ConfigureAwait(false);
                Console.WriteLine($"Running {t.Name}...");
                await t.RunAsync(http).ConfigureAwait(false);
                Console.WriteLine($"Done {t.Name}.");
            }


        }

        internal static async Task RunOneAsync(HttpClient http, string testName)
        {
            foreach (var t in TestRegistry.All)
            {
                if (string.Equals(t.Name, testName, StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine($"Running {t.Name}...");
                    await t.RunAsync(http).ConfigureAwait(false);
                    Console.WriteLine($"Done {t.Name}.");
                    return;
                }
            }

            Console.WriteLine($"Unknown test: {testName}");
        }
    }
}
