namespace IronGate.Api.Features.Captcha.Dtos;


/*
 * This DTO represents the response containing a CAPTCHA token and its expiration time for the Admin Endpoint
 */
public sealed class CaptchaTokenResponse {
    public string CaptchaToken { get; set; } = null;
    public DateTimeOffset ExpiresAt { get; set; }

}