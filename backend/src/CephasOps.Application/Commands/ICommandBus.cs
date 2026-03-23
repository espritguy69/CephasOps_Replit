namespace CephasOps.Application.Commands;

/// <summary>
/// Sends commands through the pipeline and returns a result.
/// </summary>
public interface ICommandBus
{
    Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command,
        CommandOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;

    /// <summary>
    /// Send a command (runtime type). Used by process managers that return a list of commands.
    /// </summary>
    Task<CommandResult<object?>> SendAsync(
        object command,
        CommandOptions? options = null,
        CancellationToken cancellationToken = default);
}
