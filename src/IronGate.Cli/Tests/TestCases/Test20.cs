
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {
    internal sealed class Test20 : TestCaseBase {
        public override string Name => "Test20";
        protected override string TestFolderName => "test20";

        public override async Task RunAsync(HttpClient http) {
            if (!await ApplyConfigAsync(http).ConfigureAwait(false)) return;

            var passwordsFile = Path.Combine(PathUtil.ExeDir, "rockyou.txt");
            await RunSprayAsync(http, passwordsFile).ConfigureAwait(false);
        }
    }
}
