using System.Threading.Channels;
using IronGate.Api.Features.Auth.Dtos;
using Microsoft.Extensions.Options;

namespace IronGate.Api.JsonlLogging.AttemptsService;


public sealed class AttemptsJsonlSink(IOptions<JsonlLoggingOptions> opt, Channel<AuthAttemptDto> channel) : IAttemptsJsonlSink {

    private readonly JsonlLoggingOptions _opt = opt.Value;
    private readonly ChannelWriter<AuthAttemptDto> _writer = channel.Writer;


    public bool TryEnqueue(AuthAttemptDto attempt) {
        if (!_opt.Enabled || attempt is null) return false;
        
        return _writer.TryWrite(attempt);
    }


}