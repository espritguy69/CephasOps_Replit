namespace CephasOps.Domain.PlatformSafety;

/// <summary>
/// In-memory buffer of recent platform guard violations for operator observability.
/// Implementations are bounded (e.g. last N entries). Process restart clears the buffer.
/// </summary>
public interface IGuardViolationBuffer
{
    /// <summary>Record a violation (called when a guard is about to throw).</summary>
    void Record(GuardViolationEntry entry);

    /// <summary>Get the most recent violations, newest first. Limit to <paramref name="maxCount"/>.</summary>
    IReadOnlyList<GuardViolationEntry> GetRecent(int maxCount);
}
