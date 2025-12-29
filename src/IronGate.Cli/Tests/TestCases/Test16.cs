
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test16 : TestCaseBase {
        public override string Name => "Test16";
        protected override string TestFolderName => "test16";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;
            await RunSprayAsync(http, "results").ConfigureAwait(false);
        }
    }
}
