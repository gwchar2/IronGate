
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test24 : TestCaseBase {
        public override string Name => "Test24";
        protected override string TestFolderName => "test24";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("hard")).ConfigureAwait(false);
        }
    }
}
