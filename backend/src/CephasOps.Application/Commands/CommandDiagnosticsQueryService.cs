using CephasOps.Application.Commands.DTOs;
using CephasOps.Domain.Commands;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Commands;

/// <summary>
/// Reads from CommandProcessingLogs for operator diagnostics.
/// </summary>
public class CommandDiagnosticsQueryService : ICommandDiagnosticsQueryService
{
    private readonly ApplicationDbContext _context;

    public CommandDiagnosticsQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<CommandExecutionListItemDto> Items, int TotalCount)> GetExecutionsAsync(
        string? status = null,
        string? commandType = null,
        string? correlationId = null,
        Guid? workflowInstanceId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CommandProcessingLogs.AsNoTracking();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);
        if (!string.IsNullOrEmpty(commandType))
            query = query.Where(e => e.CommandType == commandType);
        if (!string.IsNullOrEmpty(correlationId))
            query = query.Where(e => e.CorrelationId == correlationId);
        if (workflowInstanceId.HasValue)
            query = query.Where(e => e.WorkflowInstanceId == workflowInstanceId.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(e => e.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(e => new CommandExecutionListItemDto
            {
                Id = e.Id,
                IdempotencyKey = e.IdempotencyKey,
                CommandType = e.CommandType,
                CorrelationId = e.CorrelationId,
                WorkflowInstanceId = e.WorkflowInstanceId,
                Status = e.Status,
                ErrorMessage = e.ErrorMessage,
                CreatedAtUtc = e.CreatedAtUtc,
                CompletedAtUtc = e.CompletedAtUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<CommandExecutionDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await _context.CommandProcessingLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
        return e == null ? null : new CommandExecutionDetailDto
        {
            Id = e.Id,
            IdempotencyKey = e.IdempotencyKey,
            CommandType = e.CommandType,
            CorrelationId = e.CorrelationId,
            WorkflowInstanceId = e.WorkflowInstanceId,
            Status = e.Status,
            ResultJson = e.ResultJson,
            ErrorMessage = e.ErrorMessage,
            CreatedAtUtc = e.CreatedAtUtc,
            CompletedAtUtc = e.CompletedAtUtc
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommandExecutionListItemDto>> GetFailedAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await _context.CommandProcessingLogs
            .AsNoTracking()
            .Where(e => e.Status == CommandProcessingLog.Statuses.Failed)
            .OrderByDescending(e => e.CreatedAtUtc)
            .Take(take)
            .Select(e => new CommandExecutionListItemDto
            {
                Id = e.Id,
                IdempotencyKey = e.IdempotencyKey,
                CommandType = e.CommandType,
                CorrelationId = e.CorrelationId,
                WorkflowInstanceId = e.WorkflowInstanceId,
                Status = e.Status,
                ErrorMessage = e.ErrorMessage,
                CreatedAtUtc = e.CreatedAtUtc,
                CompletedAtUtc = e.CompletedAtUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
