
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test02 : TestCaseBase {
        public override string Name => "Test02";
        protected override string TestFolderName => "test02";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunBruteForceAsync(http, Users("easy")).ConfigureAwait(false);
        }
    }
}
