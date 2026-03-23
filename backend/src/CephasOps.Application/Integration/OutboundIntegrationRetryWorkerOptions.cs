namespace CephasOps.Application.Integration;

/// <summary>
/// Options for the outbound integration retry worker. Configurable via configuration section "OutboundIntegrationRetryWorker".
/// </summary>
public sealed class OutboundIntegrationRetryWorkerOptions
{
    public const string SectionName = "OutboundIntegrationRetryWorker";

    /// <summary>Polling interval in seconds. Default 60.</summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>Max deliveries to process per poll. Default 20.</summary>
    public int MaxDeliveriesPerPoll { get; set; } = 20;

    /// <summary>When true, the worker runs. Default true.</summary>
    public bool Enabled { get; set; } = true;
}
