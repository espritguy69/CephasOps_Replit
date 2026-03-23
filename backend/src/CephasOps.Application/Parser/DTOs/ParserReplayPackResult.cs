namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Aggregate result of replay-pack (replay all failed/low-confidence in last N days).
/// </summary>
public class ParserReplayPackResult
{
    public int Total { get; set; }
    public int Regressions { get; set; }
    public int Improvements { get; set; }
    public int NoChange { get; set; }
    public int Errors { get; set; }
    public IReadOnlyList<ParserReplayResult> Results { get; set; } = Array.Empty<ParserReplayResult>();
}
