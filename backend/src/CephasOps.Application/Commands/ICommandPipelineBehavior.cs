namespace CephasOps.Application.Commands;

/// <summary>
/// Delegate that invokes the next behavior or the handler.
/// </summary>
public delegate Task<CommandResult<TResult>> CommandHandlerDelegate<TResult>(
    CancellationToken cancellationToken);

/// <summary>
/// Pipeline behavior that wraps command execution (validation, idempotency, logging, retry).
/// </summary>
public interface ICommandPipelineBehavior
{
    /// <summary>
    /// Order: lower runs first. Validation=100, Idempotency=200, Logging=300, Retry=400.
    /// </summary>
    int Order { get; }
}

/// <summary>
/// Typed pipeline behavior for commands with result TResult.
/// </summary>
public interface ICommandPipelineBehavior<TCommand, TResult> : ICommandPipelineBehavior
    where TCommand : ICommand<TResult>
{
    Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default);
}
