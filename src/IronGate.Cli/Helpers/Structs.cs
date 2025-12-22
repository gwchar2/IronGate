
using System;
using System.Collections.Generic;
using System.Text.Json;
#nullable enable
namespace IronGate.Cli.Helpers {

    /*
     * Since we cant pass references in async classes, this class is just a small counter for the amount of http requests sent in the LoginAction class
     */
    internal sealed class Counter {
        internal int Value { get; set; }
        internal Counter(int initial = 0) => Value = initial;
    }
    /*
     * This is a http result class which we use to parse the results retrieved
     */
    internal sealed class HttpCallResult {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public bool IsSuccess => StatusCode >= 200 && StatusCode <= 299;

        public T? TryReadJson<T>(JsonSerializerOptions opts) {
            if (string.IsNullOrWhiteSpace(Body)) return default;
            try { return JsonSerializer.Deserialize<T>(Body, opts); }
            catch { return default; }
        }
    }
    internal sealed class UserSeed {
        internal string? GroupSeed { get; set; }
        internal Dictionary<string, string> TotpSecrets { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

}
