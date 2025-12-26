
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test25 : TestCaseBase {
        public override string Name => "Test25";
        protected override string TestFolderName => "test25";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("hard")).ConfigureAwait(false);
        }
    }
}
