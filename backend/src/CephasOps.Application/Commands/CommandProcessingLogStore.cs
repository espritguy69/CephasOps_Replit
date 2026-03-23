using CephasOps.Domain.Commands;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Commands;

/// <summary>
/// Idempotency store for commands using CommandProcessingLog. One successful completion per IdempotencyKey.
/// </summary>
public class CommandProcessingLogStore : ICommandProcessingLogStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommandProcessingLogStore> _logger;

    public CommandProcessingLogStore(ApplicationDbContext context, ILogger<CommandProcessingLogStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TryClaimAsync(
        Guid commandId,
        string idempotencyKey,
        string commandType,
        string? correlationId,
        Guid? workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.CommandProcessingLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            if (existing.Status == CommandProcessingLog.Statuses.Completed)
            {
                _logger.LogDebug("Command already completed for key {Key}", idempotencyKey);
                return false;
            }
            if (existing.Status == CommandProcessingLog.Statuses.Pending)
            {
                _logger.LogDebug("Command already in progress for key {Key}", idempotencyKey);
                return false;
            }
            // Failed: delete and re-insert so we can use new commandId (PK cannot be updated)
            var deleted = await _context.CommandProcessingLogs
                .Where(e => e.IdempotencyKey == idempotencyKey && e.Status == CommandProcessingLog.Statuses.Failed)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            if (deleted == 0) return false;
            _context.CommandProcessingLogs.Add(new CommandProcessingLog
            {
                Id = commandId,
                IdempotencyKey = idempotencyKey,
                CommandType = commandType,
                CorrelationId = correlationId,
                WorkflowInstanceId = workflowInstanceId,
                Status = CommandProcessingLog.Statuses.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Command claim (retry after failed). Key={Key}, CommandId={CommandId}", idempotencyKey, commandId);
            return true;
        }

        try
        {
            _context.CommandProcessingLogs.Add(new CommandProcessingLog
            {
                Id = commandId,
                IdempotencyKey = idempotencyKey,
                CommandType = commandType,
                CorrelationId = correlationId,
                WorkflowInstanceId = workflowInstanceId,
                Status = CommandProcessingLog.Statuses.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Command claim (first). Key={Key}, CommandId={CommandId}", idempotencyKey, commandId);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogDebug(ex, "Command claim failed (concurrent insert). Key={Key}", idempotencyKey);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task MarkCompletedAsync(Guid commandId, string resultJson, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var truncated = resultJson.Length > 8000 ? resultJson[..8000] : resultJson;
        var rows = await _context.CommandProcessingLogs
            .Where(e => e.Id == commandId && e.Status == CommandProcessingLog.Statuses.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, CommandProcessingLog.Statuses.Completed)
                .SetProperty(e => e.ResultJson, truncated)
                .SetProperty(e => e.ErrorMessage, (string?)null)
                .SetProperty(e => e.CompletedAtUtc, now),
                cancellationToken)
            .ConfigureAwait(false);
        if (rows > 0)
            _logger.LogInformation("Command completed. CommandId={CommandId}", commandId);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid commandId, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var error = errorMessage != null ? Truncate(errorMessage, 2000) : null;
        var rows = await _context.CommandProcessingLogs
            .Where(e => e.Id == commandId && e.Status == CommandProcessingLog.Statuses.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, CommandProcessingLog.Statuses.Failed)
                .SetProperty(e => e.ErrorMessage, error)
                .SetProperty(e => e.CompletedAtUtc, now),
                cancellationToken)
            .ConfigureAwait(false);
        if (rows > 0)
            _logger.LogWarning("Command failed. CommandId={CommandId}, Error={Error}", commandId, error);
    }

    /// <inheritdoc />
    public async Task<CommandProcessingLogResult?> TryGetCompletedResultAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var row = await _context.CommandProcessingLogs
            .AsNoTracking()
            .Where(e => e.IdempotencyKey == idempotencyKey && e.Status == CommandProcessingLog.Statuses.Completed)
            .Select(e => new { e.Id, e.ResultJson })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (row == null) return null;
        return new CommandProcessingLogResult { ExecutionId = row.Id, ResultJson = row.ResultJson };
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        while (inner != null)
        {
            var msg = inner.Message;
            if (msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                (inner.GetType().Name.Contains("Npgsql") && msg.Contains("23505")))
                return true;
            inner = inner.InnerException;
        }
        return false;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
