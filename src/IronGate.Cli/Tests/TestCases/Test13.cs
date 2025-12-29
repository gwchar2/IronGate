
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test13 : TestCaseBase {
        public override string Name => "Test13";
        protected override string TestFolderName => "test13";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("weak")).ConfigureAwait(false);
        }
    }
}
