using IronGate.Api.Features.Config.Dtos;
using IronGate.Core.Database.Entities;

namespace IronGate.Api.Features.Auth.PasswordHasher;
public interface IPasswordHasher {
    (string Hash, string Salt) HashPassword(string password, AuthConfigDto config);
    bool VerifyPassword(string plainPassword, UserHash userHash);
}

