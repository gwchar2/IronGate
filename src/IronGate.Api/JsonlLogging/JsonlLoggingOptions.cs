namespace IronGate.Api.JsonlLogging;


/*
 * JSonlLoggingOptions configures the JSONL logging feature.
 * This includes settings for enabling logging, file names, sampling intervals, and flush rates.
 * Taken from IronGate.Api appsettings.json configuration section "JsonlLogging".
 */

public sealed class JsonlLoggingOptions {

    public bool Enabled { get; set; } = true;
    public string BaseDirectory { get; set; } = "logs";

    public string AttemptsFileName { get; set; } = "attempts.jsonl";
    public string ResourcesFileName { get; set; } = "resources.jsonl";
    public int ResourcesSampleIntervalMs { get; set; } = 1000;
    public int WriterFlush { get; set; } = 50;
}