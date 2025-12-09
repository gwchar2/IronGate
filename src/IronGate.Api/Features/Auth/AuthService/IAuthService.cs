using IronGate.Api.Controllers.Requests;
using IronGate.Api.Features.Auth.Dtos;

namespace IronGate.Api.Features.Auth.AuthService;

public interface IAuthService {
    Task<AuthAttemptDto> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthAttemptDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthAttemptDto> LoginTotpAsync(LoginTotpRequest request, CancellationToken cancellationToken = default);
    Task<AuthAttemptDto> LoginWithCaptchaAsync(LoginCaptchaRequest request, CancellationToken cancellationToken = default);
}
