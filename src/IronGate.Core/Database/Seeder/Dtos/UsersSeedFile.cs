

using System.Text.Json.Serialization;

namespace IronGate.Core.Database.Seeder.DTO;

/*
 * DTO Matching the structure of the users_seed.json file
 */
public sealed class UsersSeedFile {

    [JsonPropertyName("seed_group")]
    public long SeedGroup { get; set; }

    [JsonPropertyName("users")]
    public List<SeedUser> Users { get; set; } = [];
}


