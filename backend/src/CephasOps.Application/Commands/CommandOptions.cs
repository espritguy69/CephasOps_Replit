namespace CephasOps.Application.Commands;

/// <summary>
/// Options when sending a command.
/// </summary>
public class CommandOptions
{
    /// <summary>Override idempotency key (takes precedence over command.IdempotencyKey when set).</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>When true, idempotency is required (command must have IdempotencyKey or options.IdempotencyKey).</summary>
    public bool RequireIdempotency { get; set; }

    /// <summary>When true, command may be enqueued for async processing (future use).</summary>
    public bool EnqueueAsync { get; set; }

    /// <summary>Optional timeout for execution.</summary>
    public TimeSpan? Timeout { get; set; }
}
