
using IronGate.Api.Features.Config.Dtos;

namespace IronGate.Api.Features.Config.ConfigService;

public interface IConfigService {
    Task<AuthConfigDto> GetConfigAsync(CancellationToken cancellationToken = default);

    Task<AuthConfigDto> UpdateConfigAsync(AuthConfigDto request, CancellationToken cancellationToken = default);
}
