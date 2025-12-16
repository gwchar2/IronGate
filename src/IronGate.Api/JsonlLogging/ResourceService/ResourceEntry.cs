namespace IronGate.Api.JsonlLogging.ResourceService;


/*
 * This class represents a single sample entry of system resource usage.
 * According to the project isntructions, we must keep a log of the system resources during attacks.
 * This information will be logged each few seconds as configured in JsonlLoggingOptions.
 */
public sealed class ResourceEntry {
    public DateTimeOffset TimestampUtc { get; set; }

    /* CPU Percent */
    public double CpuPercent { get; set; }

    /* Memory Bytes */
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }

    /* Managed Heap Bytes */
    public long ManagedHeapBytes { get; set; }

    /* Threads used */
    public int Threads { get; set; }
}