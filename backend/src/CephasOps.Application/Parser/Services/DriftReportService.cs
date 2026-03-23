using System.Text.Json;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Phase 10: Builds drift reports from ParsedOrderDrafts audit tokens and applies recommendation rules.
/// PII-safe: only ValidationNotes (parsed tokens), ValidationStatus, and CreatedAt are read from drafts;
/// no draft fields such as ServiceId, CustomerName, AddressText, CustomerPhone are included in the report.
/// </summary>
public class DriftReportService : IDriftReportService
{
    private const int TopNSignatures = 5;
    private const int TopNMissingFields = 10;

    private readonly ApplicationDbContext _context;
    private readonly ITemplateProfileService _profileService;
    private readonly ILogger<DriftReportService> _logger;

    public DriftReportService(
        ApplicationDbContext context,
        ITemplateProfileService profileService,
        ILogger<DriftReportService> logger)
    {
        _context = context;
        _profileService = profileService;
        _logger = logger;
    }

    public async Task<DriftReportResult> BuildReportAsync(int days, Guid? profileIdFilter = null, bool includeReplayStats = false, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var query = _context.ParsedOrderDrafts
            .AsNoTracking()
            .Where(d => d.CreatedAt >= since && d.ValidationNotes != null && d.ValidationNotes.Contains(AuditNotesTokenParser.AuditPrefix));
        if (profileIdFilter.HasValue)
            query = query.Where(d => d.ValidationNotes != null && d.ValidationNotes.Contains("Profile=" + profileIdFilter.Value.ToString()));
        var drafts = await query.ToListAsync(cancellationToken);

        var byProfile = new Dictionary<Guid, List<(ParsedOrderDraft Draft, AuditNotesTokens Tokens)>>();
        foreach (var draft in drafts)
        {
            var tokens = AuditNotesTokenParser.Parse(draft.ValidationNotes);
            if (tokens == null) continue;
            var key = tokens.ProfileId ?? Guid.Empty;
            if (!byProfile.ContainsKey(key)) byProfile[key] = new List<(ParsedOrderDraft, AuditNotesTokens)>();
            byProfile[key].Add((draft, tokens));
        }

        var summaries = new List<ProfileDriftSummary>();
        foreach (var kv in byProfile)
        {
            var profileId = kv.Key;
            var list = kv.Value;
            var firstTokens = list[0].Tokens;
            var profileName = firstTokens.ProfileName ?? (profileId == Guid.Empty ? "Unassigned" : profileId.ToString());
            if (profileId != Guid.Empty && string.IsNullOrEmpty(firstTokens.ProfileName))
            {
                var configOpt = await _profileService.GetProfileConfigByIdAsync(profileId, cancellationToken);
                if (configOpt.HasValue) profileName = configOpt.Value.Config.ProfileName;
            }

            var total = list.Count;
            var needsReview = list.Count(x => string.Equals(x.Draft.ValidationStatus, "NeedsReview", StringComparison.OrdinalIgnoreCase));
            var categoryCounts = list
                .Where(x => !string.IsNullOrEmpty(x.Tokens.Category))
                .GroupBy(x => x.Tokens.Category!)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            var driftCount = list.Count(x => x.Tokens.DriftDetected == true);
            var signatureCounts = list
                .Where(x => !string.IsNullOrEmpty(x.Tokens.DriftSignature))
                .GroupBy(x => x.Tokens.DriftSignature!)
                .OrderByDescending(g => g.Count())
                .Take(TopNSignatures)
                .Select(g => new SignatureCount { Signature = g.Key, Count = g.Count() })
                .ToList();
            var missingFlat = list.Where(x => x.Tokens.Missing != null).SelectMany(x => x.Tokens.Missing!);
            var missingCounts = missingFlat
                .GroupBy(f => f)
                .OrderByDescending(g => g.Count())
                .Take(TopNMissingFields)
                .Select(g => new FieldCount { FieldName = g.Key, Count = g.Count() })
                .ToList();
            var headerScores = list.Where(x => x.Tokens.HeaderScore.HasValue).Select(x => x.Tokens.HeaderScore!.Value).ToList();
            var bestScores = list.Where(x => x.Tokens.BestSheetScore.HasValue).Select(x => x.Tokens.BestSheetScore!.Value).ToList();
            double? avgHeader = headerScores.Count > 0 ? headerScores.Average() : null;
            double? avgBest = bestScores.Count > 0 ? bestScores.Average() : null;

            bool recentChange = false;
            string? version = null;
            string? effectiveFrom = null;
            string? owner = null;
            if (profileId != Guid.Empty)
            {
                var configOpt = await _profileService.GetProfileConfigByIdAsync(profileId, cancellationToken);
                if (configOpt.HasValue)
                {
                    var cfg = configOpt.Value.Config;
                    version = cfg.ProfileVersion;
                    effectiveFrom = cfg.EffectiveFrom;
                    owner = cfg.Owner;
                    if (!string.IsNullOrEmpty(cfg.EffectiveFrom) && DateTime.TryParse(cfg.EffectiveFrom, out var effDate))
                        recentChange = effDate >= since;
                }
            }

            var summary = new ProfileDriftSummary
            {
                ProfileId = profileId,
                ProfileName = profileName,
                TotalDrafts = total,
                NeedsReviewCount = needsReview,
                CountByCategory = categoryCounts,
                DriftDetectedCount = driftCount,
                TopDriftSignatures = signatureCounts,
                TopMissingFields = missingCounts,
                AvgHeaderScore = avgHeader,
                AvgBestSheetScore = avgBest,
                RecentProfileChange = recentChange,
                ProfileVersion = version,
                EffectiveFrom = effectiveFrom,
                Owner = owner
            };
            DriftReportRecommendations.Apply(summary);
            summaries.Add(summary);
        }

        summaries = summaries.OrderByDescending(s => s.TotalDrafts).ThenBy(s => s.ProfileId).ToList();

        Dictionary<Guid, int>? replayRegressions = null;
        if (includeReplayStats)
        {
            var runs = await _context.ParserReplayRuns
                .AsNoTracking()
                .Where(r => r.CreatedAt >= since && r.RegressionDetected && r.ResultSummary != null)
                .Select(r => r.ResultSummary)
                .ToListAsync(cancellationToken);
            replayRegressions = new Dictionary<Guid, int>();
            foreach (var json in runs)
            {
                if (string.IsNullOrEmpty(json)) continue;
                try
                {
                    var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("newProfileId", out var pidEl) && pidEl.ValueKind == JsonValueKind.String)
                    {
                        var sid = pidEl.GetString();
                        if (!string.IsNullOrEmpty(sid) && Guid.TryParse(sid, out var pid))
                        {
                            replayRegressions.TryGetValue(pid, out var c);
                            replayRegressions[pid] = c + 1;
                        }
                    }
                }
                catch { /* ignore */ }
            }
        }

        return new DriftReportResult
        {
            Days = days,
            GeneratedAtUtc = DateTime.UtcNow,
            TotalDrafts = drafts.Count,
            ProfilesWithDrafts = summaries.Count,
            ProfileIdFilter = profileIdFilter?.ToString(),
            ProfileSummaries = summaries,
            ReplayRegressionsByProfile = replayRegressions
        };
    }

}
