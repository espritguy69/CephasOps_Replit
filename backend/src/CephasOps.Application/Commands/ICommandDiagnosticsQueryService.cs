using CephasOps.Application.Commands.DTOs;

namespace CephasOps.Application.Commands;

/// <summary>
/// Query service for command execution history (operator diagnostics).
/// </summary>
public interface ICommandDiagnosticsQueryService
{
    /// <summary>
    /// List command executions with optional filters. Ordered by CreatedAtUtc descending.
    /// </summary>
    Task<(IReadOnlyList<CommandExecutionListItemDto> Items, int TotalCount)> GetExecutionsAsync(
        string? status = null,
        string? commandType = null,
        string? correlationId = null,
        Guid? workflowInstanceId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single command execution by id.
    /// </summary>
    Task<CommandExecutionDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// List failed command executions (Status = Failed). For retry and investigation.
    /// </summary>
    Task<IReadOnlyList<CommandExecutionListItemDto>> GetFailedAsync(
        int take = 100,
        CancellationToken cancellationToken = default);
}
