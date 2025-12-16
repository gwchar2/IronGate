using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace IronGate.Api.JsonlLogging.ResourceService;


/*
 * This is a background service that periodically logs system resource usage to a JSONL file.
 * It logs recource entries at intervals defined in JsonlLoggingOptions.
 */
public sealed class ResourcesJsonlService ( IOptions<JsonlLoggingOptions> opt) : BackgroundService {

    private readonly JsonlLoggingOptions _opt = opt.Value;

    private static readonly JsonSerializerOptions JsonOpts = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /*
     * This method is called when the background service starts.
     * It periodically samples system resource usage and appends the data to a JSONL file.
     */
    protected override async Task ExecuteAsync (CancellationToken stoppingToken) {
        if (!_opt.Enabled) return;

        Directory.CreateDirectory(_opt.BaseDirectory);

        var process = Process.GetCurrentProcess();
        var cpuCount = Environment.ProcessorCount;

        var lastWall = Stopwatch.GetTimestamp();
        var lastCpu = process.TotalProcessorTime;

        var interval = TimeSpan.FromMilliseconds(Math.Max(200, _opt.ResourcesSampleIntervalMs));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken)) {
            process.Refresh();

            var nowWall = Stopwatch.GetTimestamp();
            var nowCpu = process.TotalProcessorTime;

            var wallSeconds = (nowWall - lastWall) / (double)Stopwatch.Frequency;
            var cpuSeconds = (nowCpu - lastCpu).TotalSeconds;

            lastWall = nowWall;
            lastCpu = nowCpu;

            // CPU percent across all cores
            var cpuPercent = wallSeconds > 0
                ? (cpuSeconds / (wallSeconds * cpuCount)) * 100.0
                : 0.0;

            var entry = new ResourceEntry {
                TimestampUtc = DateTimeOffset.UtcNow,
                CpuPercent = Math.Max(0.0, cpuPercent),
                WorkingSetBytes = process.WorkingSet64,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                ManagedHeapBytes = GC.GetTotalMemory(forceFullCollection: false),
                Threads = process.Threads.Count
            };

            var dayFolder = Path.Combine(_opt.BaseDirectory, DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dayFolder);

            var filePath = Path.Combine(dayFolder, _opt.ResourcesFileName);

            await using var fs = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

            await using var sw = new StreamWriter(fs);

            var json = JsonSerializer.Serialize(entry, JsonOpts);
            await sw.WriteLineAsync(json);
            await sw.FlushAsync();
        }
    }
}