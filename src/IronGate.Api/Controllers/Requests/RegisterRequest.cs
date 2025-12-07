namespace IronGate.Api.Controllers.Requests;

/*
 * Register Request 
 */
public sealed class RegisterRequest {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
