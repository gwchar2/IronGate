using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test01 : TestCaseBase {
        public override string Name => "Test01";
        protected override string TestFolderName => "test01";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("easy")).ConfigureAwait(false);
        }
    }
}
