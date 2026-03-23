namespace CephasOps.Application.Commands;

/// <summary>
/// Marker for commands. Use ICommand&lt;TResult&gt; for typed result.
/// </summary>
public interface ICommand
{
    /// <summary>Optional idempotency key. When set and RequireIdempotency, duplicate sends return cached result.</summary>
    string? IdempotencyKey { get; }

    /// <summary>Optional correlation id for tracing.</summary>
    string? CorrelationId { get; }

    /// <summary>Optional workflow instance id when command is part of a saga/orchestration.</summary>
    Guid? WorkflowInstanceId { get; }
}

/// <summary>
/// Command with a typed result.
/// </summary>
public interface ICommand<TResult> : ICommand
{
}
