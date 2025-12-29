
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {

    internal sealed class Test04 : TestCaseBase {
        public override string Name => "Test04";
        protected override string TestFolderName => "test04";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("weak")).ConfigureAwait(false);
        }
    }
}
