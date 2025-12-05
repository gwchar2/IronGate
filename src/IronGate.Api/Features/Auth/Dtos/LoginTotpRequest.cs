namespace IronGate.Api.Features.Auth.Dtos;

public sealed class LoginTotpRequest {
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string TotpCode { get; set; } = null!;
}
