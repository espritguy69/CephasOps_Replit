namespace CephasOps.Application.Events;

/// <summary>
/// Options for building platform event envelope metadata (SourceService, SourceModule, etc.).
/// </summary>
public class PlatformEventEnvelopeOptions
{
    public const string SectionName = "EventBus:PlatformEnvelope";

    /// <summary>Service name that publishes events (e.g. CephasOps.Api).</summary>
    public string? SourceService { get; set; }

    /// <summary>Bounded context or module (e.g. Workflow, Orders).</summary>
    public string? SourceModule { get; set; }

    /// <summary>Default priority when not set on the event (e.g. Normal).</summary>
    public string? DefaultPriority { get; set; } = "Normal";
}
