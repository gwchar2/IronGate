using System;
using System.Collections.Generic;
#nullable enable
namespace IronGate.Cli.Helpers.Dto;

internal sealed class UserSeed {
    internal string? GroupSeed { get; set; }
    internal Dictionary<string, string> TotpSecrets { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}