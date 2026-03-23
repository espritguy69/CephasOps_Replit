using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Phase 10: Builds drift insight reports from ParsedOrderDrafts ValidationNotes and optional replay runs.
/// </summary>
public interface IDriftReportService
{
    Task<DriftReportResult> BuildReportAsync(int days, Guid? profileIdFilter = null, bool includeReplayStats = false, CancellationToken cancellationToken = default);
}
