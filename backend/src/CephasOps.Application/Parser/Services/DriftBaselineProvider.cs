using System.Text.RegularExpressions;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Loads drift baseline from latest successful (Valid) ParsedOrderDraft for the given ParserTemplateId (profile).
/// Parses ValidationNotes audit segment for Sheet=, HeaderRow=, HeaderScore=, BestSheetScore=.
/// </summary>
public class DriftBaselineProvider : IDriftBaselineProvider
{
    private readonly ApplicationDbContext _context;

    public DriftBaselineProvider(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DriftDetector.DriftBaseline?> GetBaselineAsync(Guid templateProfileId, CancellationToken cancellationToken = default)
    {
        var draft = await _context.ParsedOrderDrafts
            .AsNoTracking()
            .Where(d => d.ValidationStatus == "Valid")
            .Join(_context.ParseSessions,
                d => d.ParseSessionId,
                s => s.Id,
                (d, s) => new { Draft = d, Session = s })
            .Where(x => x.Session.ParserTemplateId == templateProfileId)
            .OrderByDescending(x => x.Draft.CreatedAt)
            .Select(x => new { x.Draft.ValidationNotes })
            .FirstOrDefaultAsync(cancellationToken);

        if (draft?.ValidationNotes == null) return null;

        return ParseBaselineFromAudit(draft.ValidationNotes);
    }

    /// <summary>
    /// Parse audit segment " | [Audit] ParseStatus=...; Sheet=...; HeaderRow=...; HeaderScore=...; BestSheetScore=..." into baseline.
    /// </summary>
    internal static DriftDetector.DriftBaseline? ParseBaselineFromAudit(string validationNotes)
    {
        const string auditPrefix = "[Audit] ";
        var idx = validationNotes.IndexOf(auditPrefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var segment = validationNotes.Substring(idx + auditPrefix.Length);
        var baseline = new DriftDetector.DriftBaseline();

        baseline.SelectedSheetName = MatchOne(segment, @"Sheet=([^;]+)");
        var headerRow = MatchOne(segment, @"HeaderRow=(\d+)");
        if (headerRow != null && int.TryParse(headerRow, out var hr)) baseline.DetectedHeaderRow = hr;
        var headerScore = MatchOne(segment, @"HeaderScore=(\d+)");
        if (headerScore != null && int.TryParse(headerScore, out var hs)) baseline.HeaderScore = hs;
        var bestSheet = MatchOne(segment, @"BestSheetScore=(\d+)");
        if (bestSheet != null && int.TryParse(bestSheet, out var bs)) baseline.SheetScoreBest = bs;

        return baseline.SelectedSheetName != null || baseline.DetectedHeaderRow.HasValue || baseline.HeaderScore.HasValue || baseline.SheetScoreBest.HasValue
            ? baseline
            : null;
    }

    private static string? MatchOne(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }
}
