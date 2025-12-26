
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test14 : TestCaseBase {
        public override string Name => "Test14";
        protected override string TestFolderName => "test14";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("medium")).ConfigureAwait(false);
        }
    }
}
