
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {

    internal sealed class Test06 : TestCaseBase {
        public override string Name => "Test06";
        protected override string TestFolderName => "test06";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("weak")).ConfigureAwait(false);
        }
    }
}
