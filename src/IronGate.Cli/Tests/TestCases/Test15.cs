
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test15 : TestCaseBase {
        public override string Name => "Test15";
        protected override string TestFolderName => "test15";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("easy")).ConfigureAwait(false);
        }
    }
}
