namespace IronGate.Api.Controllers.Requests;


/*
 * Login with CAPTCHA request
 */
public sealed class LoginCaptchaRequest {

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string CaptchaToken { get; set; } = string.Empty;
}

