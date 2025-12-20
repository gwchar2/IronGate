using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using IronGate.Cli.Helpers;
using IronGate.Cli.Attacks;
using IronGate.Cli.Constants;
#nullable enable

namespace IronGate.Cli {
    internal static class Program {

        /*
         * IronGate.Cli Main Entry Point
         * Commands: register, config, captcha, attack
         * Examples: see PrintHelp()
         */
        public static async Task<int> Main(string[] args) {
            try {

                if (args.Length == 0 || IsHelp(args[0])) {
                    Printers.PrintHelp();
                    return 0;
                }

                using var http = new HttpClient { BaseAddress = new Uri(Defaults.url) };
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                var command = args[0].ToLowerInvariant();

                (bool printHelp, HttpCallResult? resp) result = (true, null);
                switch (command) {
                    case "register":
                        result = await Register.RegisterAction(http, args);
                        break;
                    case "config":
                        result = await Config.ConfigAction(http, args);
                        break;
                    case "captcha":
                        result = await Captcha.CaptchaAction(http, args);
                        break;
                    case "login":
                        result = await Login.LoginAction(http, args);
                        break;
                    case "attack":
                        result = await Attack.AttackAction(http, args);
                        break;
                    default:
                        Unknown(command);
                        break;
                }

                if (result.printHelp)
                    Printers.PrintHelp();
                if (result.resp is not null) Printers.PrintHttpResult(result.resp);

                return 1;
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
        /*
         * Parses help flags
         */
        private static bool IsHelp(string s)
            => s.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
               s.Equals("help", StringComparison.OrdinalIgnoreCase);

        /*
         * Parses unknown command
         */
        private static void Unknown(string cmd) {
            Console.Error.WriteLine($"Unknown command: {cmd}");
            Printers.PrintHelp();
        }

    }
}