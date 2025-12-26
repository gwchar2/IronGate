
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test17 : TestCaseBase {
        public override string Name => "Test17";
        protected override string TestFolderName => "test17";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("medium")).ConfigureAwait(false);
        }
    }
}
