
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test03 : TestCaseBase {
        public override string Name => "Test03";
        protected override string TestFolderName => "test03";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("easy")).ConfigureAwait(false);
        }
    }
}
