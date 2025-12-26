
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test09 : TestCaseBase {
        public override string Name => "Test09";
        protected override string TestFolderName => "test09";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("medium")).ConfigureAwait(false);
        }
    }

}
