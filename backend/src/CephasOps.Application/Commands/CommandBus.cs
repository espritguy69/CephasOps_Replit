using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Commands;

/// <summary>
/// Resolves handler and pipeline behaviors, runs pipeline (Validation -> Idempotency -> Logging -> Retry -> Handler), returns CommandResult.
/// </summary>
public class CommandBus : ICommandBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandBus> _logger;

    public CommandBus(IServiceProvider serviceProvider, ILogger<CommandBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<CommandResult<TResult>> SendAsync<TCommand, TResult>(
        TCommand command,
        CommandOptions? options = null,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var executionId = Guid.NewGuid();
        CommandPipelineContext.Set(options, executionId);

        try
        {
            var handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
            if (handler == null)
            {
                _logger.LogError("No handler registered for command {CommandType}", typeof(TCommand).FullName);
                return CommandResult<TResult>.Fail($"No handler registered for {typeof(TCommand).Name}.", executionId);
            }

            var behaviors = _serviceProvider.GetServices<ICommandPipelineBehavior<TCommand, TResult>>()
                .OrderBy(b => b.Order)
                .ToList();

            CommandHandlerDelegate<TResult> runHandler = async ct =>
            {
                var result = await handler.HandleAsync(command, ct).ConfigureAwait(false);
                return CommandResult<TResult>.Ok(result, executionId);
            };

            var pipeline = runHandler;
            for (var i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = pipeline;
                pipeline = ct => behavior.HandleAsync(command, next, ct);
            }

            var commandResult = await pipeline(cancellationToken).ConfigureAwait(false);

            if (!commandResult.Success && commandResult.ExecutionId == default)
                commandResult = CommandResult<TResult>.Fail(
                    commandResult.ErrorMessage ?? "Command failed.",
                    executionId);

            return commandResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command bus error. Type={CommandType}, ExecutionId={ExecutionId}", typeof(TCommand).Name, executionId);
            return CommandResult<TResult>.Fail(ex.Message, executionId);
        }
        finally
        {
            CommandPipelineContext.Clear();
        }
    }

    /// <inheritdoc />
    public async Task<CommandResult<object?>> SendAsync(
        object command,
        CommandOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            var executionId = Guid.NewGuid();
            return CommandResult<object?>.Fail("Command is null.", executionId);
        }
        var type = command.GetType();
        var iface = type.GetInterfaces().FirstOrDefault(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
        if (iface == null)
        {
            var executionId = Guid.NewGuid();
            _logger.LogError("Type {Type} does not implement ICommand<TResult>", type.FullName);
            return CommandResult<object?>.Fail($"Command type {type.Name} does not implement ICommand<TResult>.", executionId);
        }
        var resultType = iface.GetGenericArguments()[0];
        var generic = typeof(CommandBus)
            .GetMethods()
            .First(m => m.Name == nameof(SendAsync) && m.IsGenericMethod && m.GetGenericArguments().Length == 2)
            .MakeGenericMethod(type, resultType);
        var task = (Task)generic.Invoke(this, new object?[] { command, options, cancellationToken })!;
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task);
        // result is CommandResult<TResult>; map to CommandResult<object?>
        var success = (bool)result!.GetType().GetProperty("Success")!.GetValue(result)!;
        var res = result.GetType().GetProperty("Result")!.GetValue(result);
        var error = (string?)result.GetType().GetProperty("ErrorMessage")!.GetValue(result);
        var reused = (bool)result.GetType().GetProperty("IdempotencyReused")!.GetValue(result)!;
        var execId = (Guid)result.GetType().GetProperty("ExecutionId")!.GetValue(result)!;
        if (success)
            return CommandResult<object?>.Ok(res, execId, reused);
        return CommandResult<object?>.Fail(error ?? "Unknown", execId);
    }
}
