

using System.Text.Json.Serialization;

namespace IronGate.Core.Database.Seeder.DTO;

/*
 * DTO Representing a single user in the users_seed.json file
 */
public sealed class SeedUser {
    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("password")]
    public string Password { get; set; } = null!;

    [JsonPropertyName("category")]
    public string Category { get; set; } = null!;

    [JsonPropertyName("totp_enabled")]
    public bool TotpEnabled { get; set; }

    [JsonPropertyName("secret_totp")]
    public string? SecretTotp { get; set; }
}