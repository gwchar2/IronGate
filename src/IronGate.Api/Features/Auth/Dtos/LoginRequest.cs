namespace IronGate.Api.Features.Auth.Dtos;

public sealed class LoginRequest {
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
