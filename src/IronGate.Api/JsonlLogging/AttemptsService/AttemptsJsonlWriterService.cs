using IronGate.Api.Features.Auth.Dtos;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Channels;

namespace IronGate.Api.JsonlLogging.AttemptsService;

public sealed class AttemptsJsonlWriterService(IOptions<JsonlLoggingOptions> opt, Channel<AuthAttemptDto> channel) : BackgroundService {
    private readonly JsonlLoggingOptions _opt = opt.Value;
    private readonly ChannelReader<AuthAttemptDto> _reader = channel.Reader;

    private static readonly JsonSerializerOptions JsonOpts = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /*
     * ExecuteAsync runs the background service that writes authentication attempts to JSONL files.
     * It creates daily folders, appends serialized AuthAttemptDto objects to files,
     * and flushes the writer based on configured intervals.
     */
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_opt.Enabled) return;

        Directory.CreateDirectory(_opt.BaseDirectory);

        int linesSinceFlush = 0;

        while (!stoppingToken.IsCancellationRequested) {
            AuthAttemptDto attempt;
            try {
                attempt = await _reader.ReadAsync(stoppingToken);
            }
            catch (OperationCanceledException) {
                break;
            }

            // Daily folder (UTC)
            var dayFolder = Path.Combine(_opt.BaseDirectory, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dayFolder);

            var filePath = Path.Combine(dayFolder, _opt.AttemptsFileName);

            await using var fs = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

            await using var sw = new StreamWriter(fs);

            var json = JsonSerializer.Serialize(attempt, JsonOpts);
            await sw.WriteLineAsync(json);

            linesSinceFlush++;
            if (linesSinceFlush >= _opt.WriterFlush) {
                await sw.FlushAsync(stoppingToken);
                linesSinceFlush = 0;
            }
        }
    }
}
