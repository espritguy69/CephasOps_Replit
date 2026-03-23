using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Replays historical EmailAttachments through the parser and records regression/improvement.
/// Read-only for Orders and Drafts; writes only to ParserReplayRuns.
/// </summary>
public interface IParserReplayService
{
    /// <summary>
    /// Replay a single attachment by its ID. Finds original draft by session + filename for comparison.
    /// Phase 9: optional lifecycleContext is stored in ResultSummary (profileVersion, owner, packName, etc.).
    /// </summary>
    Task<ParserReplayResult> ReplayByAttachmentIdAsync(Guid attachmentId, string triggeredBy, ProfileLifecycleContext? lifecycleContext = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replay all attachments from a parse session. One replay run per attachment that has a matching draft.
    /// </summary>
    Task<IReadOnlyList<ParserReplayResult>> ReplayByParseSessionIdAsync(Guid parseSessionId, string triggeredBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replay all failed or low-confidence parses in the last N days. Returns aggregate counts and exits with regressions count.
    /// </summary>
    Task<ParserReplayPackResult> ReplayPackAsync(int days, string triggeredBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 9: Get distinct attachment IDs for drafts that have Profile=&lt;profileId&gt; in ValidationNotes audit (recent N days).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAttachmentIdsForProfileAsync(Guid profileId, int days, CancellationToken cancellationToken = default);
}
