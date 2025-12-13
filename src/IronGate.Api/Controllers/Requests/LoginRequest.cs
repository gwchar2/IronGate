

namespace IronGate.Api.Controllers.Requests;


/*
 * Regular log in request
 */
public sealed class LoginRequest {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
