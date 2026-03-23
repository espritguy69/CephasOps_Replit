using System.Text.Json;
using CephasOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Commands.Pipeline;

/// <summary>
/// Claims command execution by idempotency key; returns cached result when already completed.
/// Tenant-safe: when <see cref="TenantScope.CurrentTenantId"/> is set, the stored key is prefixed with company id so the same logical key in different tenants does not collide.
/// </summary>
public class IdempotencyBehavior<TCommand, TResult> : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public int Order => 200;

    private readonly ICommandProcessingLogStore _store;
    private readonly ILogger<IdempotencyBehavior<TCommand, TResult>> _logger;

    public IdempotencyBehavior(
        ICommandProcessingLogStore store,
        ILogger<IdempotencyBehavior<TCommand, TResult>> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken = default)
    {
        var options = CommandPipelineContext.CurrentOptions;
        var executionId = CommandPipelineContext.CurrentExecutionId;
        var rawKey = options?.IdempotencyKey ?? command.IdempotencyKey;

        if (string.IsNullOrWhiteSpace(rawKey))
        {
            if (options?.RequireIdempotency == true)
            {
                _logger.LogWarning("RequireIdempotency set but no IdempotencyKey on command or options");
                return CommandResult<TResult>.Fail("Idempotency key is required.", executionId);
            }
            return await next(cancellationToken).ConfigureAwait(false);
        }

        // Tenant-safe key: include CompanyId when in tenant scope so the same key in different tenants does not reuse the same result.
        var companyId = TenantScope.CurrentTenantId;
        var idempotencyKey = (companyId.HasValue && companyId.Value != Guid.Empty)
            ? $"{companyId.Value:N}:{rawKey}"
            : rawKey;

        var commandType = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var claimed = await _store.TryClaimAsync(
            executionId,
            idempotencyKey,
            commandType,
            command.CorrelationId,
            command.WorkflowInstanceId,
            cancellationToken).ConfigureAwait(false);

        if (!claimed)
        {
            var existing = await _store.TryGetCompletedResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
            // Backward compatibility: old records may have been stored with raw key (no tenant prefix)
            if (existing == null && companyId.HasValue && companyId.Value != Guid.Empty)
                existing = await _store.TryGetCompletedResultAsync(rawKey, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                var reused = existing.ExecutionId;
                _logger.LogInformation("Idempotency reuse for key {Key}, ExecutionId {ExecutionId}", idempotencyKey, reused);
                TResult? result = default;
                if (existing.ResultJson != null)
                {
                    try
                    {
                        result = JsonSerializer.Deserialize<TResult>(existing.ResultJson);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cached result for idempotency key {Key}", idempotencyKey);
                    }
                }
                return CommandResult<TResult>.Ok(result!, reused, idempotencyReused: true);
            }
            return CommandResult<TResult>.Fail("Command already in progress or idempotency conflict.", executionId);
        }

        try
        {
            var result = await next(cancellationToken).ConfigureAwait(false);
            if (result.Success && result.Result != null)
            {
                var resultJson = JsonSerializer.Serialize(result.Result);
                await _store.MarkCompletedAsync(executionId, resultJson, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _store.MarkFailedAsync(executionId, result.ErrorMessage ?? "Unknown error", cancellationToken).ConfigureAwait(false);
            }
            return result;
        }
        catch (Exception ex)
        {
            await _store.MarkFailedAsync(executionId, ex.Message, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
