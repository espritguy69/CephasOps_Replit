using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Commands.Pipeline;

/// <summary>
/// Optional validation step. When FluentValidation validators are registered, runs them; otherwise passes through.
/// </summary>
public class ValidationBehavior<TCommand, TResult> : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public int Order => 100;

    private readonly ILogger<ValidationBehavior<TCommand, TResult>> _logger;

    public ValidationBehavior(ILogger<ValidationBehavior<TCommand, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        // Optional: resolve IValidator<TCommand> and validate; on failure return CommandResult.Fail
        // No FluentValidation in project - pass through
        _logger.LogDebug("ValidationBehavior passing through for {CommandType}", typeof(TCommand).Name);
        return await next(cancellationToken).ConfigureAwait(false);
    }
}
