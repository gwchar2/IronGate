using System.Net.Http;
using System.Threading.Tasks;

namespace IronGate.Cli.Tests {

    /*
     * Represents a test case for the IronGate CLI application.
     * Each test case has a name, target folder for logs, and a configuration file to load.
     * The RunAsync method executes the test case using the provided HttpClient.
     */
    internal interface ITestCase {
        string Name { get; }                 // "Test02"
        string TargetFolder { get; }         // \docs\Test Logs\test02
        string ConfigToLoad { get; }         // \test02\config.json

        Task RunAsync(HttpClient http);
    }
}
