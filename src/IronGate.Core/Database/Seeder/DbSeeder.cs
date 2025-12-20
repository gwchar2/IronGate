using System.Text.Json;
using IronGate.Core.Database.Entities;
using IronGate.Core.Security;
using Microsoft.EntityFrameworkCore;
using IronGate.Core.Database.Seeder.DTO;

namespace IronGate.Core.Database.Seeder;

public static class DbSeeder {

    private const string SeedFilePath = "/app/SeedData/user_seed.json";
    public static async Task SeedAsync(AppDbContext db, string pepper) {
        
        // If already seeded we skip
        if (await db.Users.AnyAsync())
            return;

        // Search for the file
        if (!File.Exists(SeedFilePath))
            throw new FileNotFoundException("Seed file not found", SeedFilePath);

        var json = await File.ReadAllTextAsync(SeedFilePath);

        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };

        var seed = JsonSerializer.Deserialize<UsersSeedFile>(json, options)
                   ?? throw new InvalidOperationException("Failed to deserialize users seed file.");

        var now = DateTime.UtcNow;

        foreach (var entry in seed.Users) {

            var user = new User {
                Id = Guid.NewGuid(),
                Username = entry.Username,
                PlainPassword = entry.Password,
                PasswordStrengthCategory = entry.Category,
                TotpEnabled = entry.TotpEnabled,
                TotpSecret = entry.TotpEnabled ? entry.SecretTotp : null,
                TotpRegisteredAt = entry.TotpEnabled ? now : null,
                CreatedAt = now
            };

            db.Users.Add(user);

            // hashes WITHOUT pepper
            var (shaHash, shaSalt) = HashHelper.HashSha256(entry.Password);
            var (bcryptHash, _) = HashHelper.HashBcrypt(entry.Password);
            var (argonHash, argonSalt) = HashHelper.HashArgon2id(entry.Password);

            db.UserHashes.AddRange(
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "sha256",
                    Salt = shaSalt,
                    Hash = shaHash,
                    CreatedAt = now
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "bcrypt",
                    Salt = "",          // bcrypt does not use a separate salt
                    Hash = bcryptHash,
                    CreatedAt = now
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "argon2id",
                    Salt = argonSalt,
                    Hash = argonHash,
                    CreatedAt = now
                }
            );

            // hashes WITH pepper (pepper + password)
            var (shaPepperHash, _) = HashHelper.HashSha256(pepper + entry.Password, shaSalt);
            var (bcryptPepperHash, _) = HashHelper.HashBcrypt(pepper + entry.Password);
            var (argonPepperHash, _) = HashHelper.HashArgon2id(pepper + entry.Password, argonSalt);

            db.UserHashes.AddRange(
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "sha256",
                    Salt = shaSalt,
                    Hash = shaPepperHash,
                    CreatedAt = now,
                    PepperEnabled = true
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "bcrypt",
                    Salt = "",
                    Hash = bcryptPepperHash,
                    CreatedAt = now,
                    PepperEnabled = true
                },
                new UserHash {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    HashAlgorithm = "argon2id",
                    Salt = argonSalt,
                    Hash = argonPepperHash,
                    CreatedAt = now,
                    PepperEnabled = true
                }
            );
        }


        // Default config proile
        db.ConfigProfile.Add(new DbConfigProfile {
            Id = Guid.NewGuid(),
            Name = "Default Profile",
            HashAlgorithm = "ARGON2ID",
            PepperEnabled = true,
            RateLimitEnabled = true,
            RateLimitWindowSeconds = 60,
            MaxAttemptsPerUser = 5,
            LockoutEnabled = true,
            LockoutThreshold = 5,
            LockoutDurationSeconds = 300,
            CaptchaEnabled = true,
            CaptchaAfterFailedAttempts = 3

        });

        await db.SaveChangesAsync();
    }
}
