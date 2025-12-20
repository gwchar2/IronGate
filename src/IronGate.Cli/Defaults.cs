

using System.Text.Json;

namespace IronGate.Cli.Constants {
    internal static class Defaults {

        internal static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
        internal const string url = "http://localhost:8080/";
        internal const string ConfigRoute = "/api/config";
        internal const string RegisterRoute = "/api/auth/register";
        internal const string LoginRoute = "/api/auth/login";
        internal const string LoginTotpRoute = "/api/auth/login/totp";
        internal const string CaptchaRoute = "/api/admin/get_captcha_token";
        internal const string QueryKey = "groupSeed";
        internal const string UserSeed = "user_seed.json";
        internal const string RockYou = "rockyou.txt";
        internal const int DefaultLimit = 50000;
        internal const int HardLimit = 1000000;
        internal const int TimeLimitSeconds = 7200;
    }
}
