using IronGate.Api.Features.Auth.Dtos;

namespace IronGate.Api.JsonlLogging.AttemptsService;

public interface IAttemptsJsonlSink{
    bool TryEnqueue(AuthAttemptDto attempt);
}

