namespace IronGate.Api.Controllers.Requests;

/*
 * Login TOTP Request
 */
public sealed class LoginTotpRequest {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string TotpCode { get; set; } = string.Empty;
    public string CaptchaToken { get; set; } = string.Empty;
}
