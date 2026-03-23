using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Commands.Pipeline;

/// <summary>
/// Logs command start and completion (success or failure).
/// </summary>
public class LoggingBehavior<TCommand, TResult> : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public int Order => 300;

    private readonly ILogger<LoggingBehavior<TCommand, TResult>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TCommand, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        var executionId = CommandPipelineContext.CurrentExecutionId;
        _logger.LogInformation(
            "Command started. Type={CommandType}, ExecutionId={ExecutionId}, CorrelationId={CorrelationId}",
            typeof(TCommand).Name, executionId, command.CorrelationId);

        var result = await next(cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            _logger.LogInformation(
                "Command completed. Type={CommandType}, ExecutionId={ExecutionId}, IdempotencyReused={Reused}",
                typeof(TCommand).Name, executionId, result.IdempotencyReused);
        }
        else
        {
            _logger.LogWarning(
                "Command failed. Type={CommandType}, ExecutionId={ExecutionId}, Error={Error}",
                typeof(TCommand).Name, executionId, result.ErrorMessage);
        }

        return result;
    }
}
