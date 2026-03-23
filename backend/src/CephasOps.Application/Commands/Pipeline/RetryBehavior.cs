using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CephasOps.Application.Commands.Pipeline;

/// <summary>
/// Retries transient failures using Polly WaitAndRetryAsync.
/// </summary>
public class RetryBehavior<TCommand, TResult> : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public int Order => 400;

    private readonly ILogger<RetryBehavior<TCommand, TResult>> _logger;

    private static readonly ResiliencePipeline<CommandResult<TResult>> Pipeline = new ResiliencePipelineBuilder<CommandResult<TResult>>()
        .AddRetry(new RetryStrategyOptions<CommandResult<TResult>>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder<CommandResult<TResult>>()
                .HandleResult(r => !r.Success && IsTransient(r.ErrorMessage)),
            OnRetry = _ => ValueTask.CompletedTask
        })
        .Build();

    public RetryBehavior(ILogger<RetryBehavior<TCommand, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        var context = ResilienceContextPool.Shared.Get(cancellationToken);
        var result = await Pipeline.ExecuteAsync(
            async ctx => await next(ctx.CancellationToken).ConfigureAwait(false),
            context).ConfigureAwait(false);
        if (!result.Success)
            _logger.LogWarning("Command failed (after retries if transient). Type={CommandType}, Error={Error}", typeof(TCommand).Name, result.ErrorMessage);
        return result;
    }

    private static bool IsTransient(string? error)
    {
        if (string.IsNullOrEmpty(error)) return false;
        var lower = error.ToLowerInvariant();
        return lower.Contains("timeout") || lower.Contains("deadlock") || lower.Contains("connection") ||
               lower.Contains("transient") || lower.Contains("busy") || lower.Contains("locked");
    }
}
