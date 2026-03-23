using System.Text.Json;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Loads template profiles from ParserTemplates (ProfileConfig in Description as "PROFILE_JSON: {...}").
/// Resolves best match by specificity: partnerId &gt; filenameRegex &gt; senderDomain &gt; subjectContains.
/// </summary>
public class TemplateProfileService : ITemplateProfileService
{
    public const string ProfileJsonPrefix = "PROFILE_JSON:";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<TemplateProfileService> _logger;

    public TemplateProfileService(ApplicationDbContext context, ILogger<TemplateProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TemplateProfileContext?> GetBestMatchProfileAsync(
        string? senderEmail,
        string? subject,
        string? attachmentFileName,
        Guid? partnerId,
        Guid? companyId,
        CancellationToken cancellationToken = default)
    {
        var templates = await _context.ParserTemplates
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderByDescending(t => t.Priority)
            .ToListAsync(cancellationToken);

        var candidates = new List<(ParserTemplate Entity, TemplateProfileConfig Config, int Specificity)>();

        foreach (var t in templates)
        {
            var config = TryParseProfileConfig(t.Description, t.Id, t.Name);
            if (config == null || !config.Enabled) continue;

            int specificity = MatchSpecificity(config, senderEmail, subject, attachmentFileName, partnerId);
            if (specificity <= 0) continue;

            candidates.Add((t, config, specificity));
        }

        if (candidates.Count == 0) return null;

        var best = candidates.OrderByDescending(c => c.Specificity).ThenByDescending(c => c.Entity.Priority).First();
        return ToContext(best.Entity, best.Config);
    }

    /// <summary>
    /// Parse ProfileConfig from Description. Expects "PROFILE_JSON: { ... }". Invalid JSON is ignored and logged.
    /// </summary>
    internal static TemplateProfileConfig? TryParseProfileConfig(string? description, Guid templateId, string templateName)
    {
        if (string.IsNullOrWhiteSpace(description) || !description.TrimStart().StartsWith(ProfileJsonPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var json = description.TrimStart().Substring(ProfileJsonPrefix.Length).Trim();
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var config = JsonSerializer.Deserialize<TemplateProfileConfig>(json);
            if (config == null) return null;
            if (config.ProfileId == Guid.Empty) config.ProfileId = templateId;
            if (string.IsNullOrEmpty(config.ProfileName)) config.ProfileName = templateName;
            return config;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int MatchSpecificity(TemplateProfileConfig config, string? senderEmail, string? subject, string? fileName, Guid? partnerId)
    {
        var rules = config.MatchRules;
        if (rules == null) return 0;

        int score = 0;
        bool any = false;

        if (partnerId.HasValue && rules.PartnerIds != null && rules.PartnerIds.Count > 0)
        {
            if (rules.PartnerIds.Contains(partnerId.Value))
            {
                score += 1000;
                any = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(fileName) && rules.FilenameRegex != null && rules.FilenameRegex.Count > 0)
        {
            foreach (var pattern in rules.FilenameRegex)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                try
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(fileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        score += 100;
                        any = true;
                        break;
                    }
                }
                catch (ArgumentException) { /* invalid regex */ }
            }
        }

        var domain = GetDomain(senderEmail);
        if (!string.IsNullOrWhiteSpace(domain) && rules.SenderDomains != null && rules.SenderDomains.Count > 0)
        {
            var domainLower = domain.ToLowerInvariant();
            if (rules.SenderDomains.Any(d => string.Equals((d ?? "").Trim().ToLowerInvariant(), domainLower)))
            {
                score += 10;
                any = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(subject) && rules.SubjectContains != null && rules.SubjectContains.Count > 0)
        {
            var subjectLower = subject.ToLowerInvariant();
            if (rules.SubjectContains.Any(s => subjectLower.Contains((s ?? "").Trim().ToLowerInvariant())))
            {
                score += 1;
                any = true;
            }
        }

        return any ? score : 0;
    }

    private static string? GetDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var idx = email.IndexOf('@');
        return idx >= 0 && idx < email.Length - 1 ? email.Substring(idx + 1).Trim() : null;
    }

    public async Task<(TemplateProfileConfig Config, Guid TemplateId)?> GetProfileConfigByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var template = await _context.ParserTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == profileId && !t.IsDeleted, cancellationToken);
        if (template == null) return null;
        var config = TryParseProfileConfig(template.Description, template.Id, template.Name);
        if (config == null) return null;
        return (config, template.Id);
    }

    public async Task<IReadOnlyList<(TemplateProfileConfig Config, Guid TemplateId)>> GetAllProfileConfigsAsync(bool enabledOnly, CancellationToken cancellationToken = default)
    {
        var query = _context.ParserTemplates.AsNoTracking().Where(t => !t.IsDeleted);
        if (enabledOnly) query = query.Where(t => t.IsActive);
        var templates = await query.OrderByDescending(t => t.Priority).ToListAsync(cancellationToken);
        var list = new List<(TemplateProfileConfig Config, Guid TemplateId)>();
        foreach (var t in templates)
        {
            var config = TryParseProfileConfig(t.Description, t.Id, t.Name);
            if (config == null) continue;
            if (enabledOnly && !config.Enabled) continue;
            list.Add((config, t.Id));
        }
        return list;
    }

    public async Task<IReadOnlyList<Guid>> ResolvePackAttachmentIdsAsync(ProfilePackConfig pack, CancellationToken cancellationToken = default)
    {
        var ids = new HashSet<Guid>();
        if (pack.AttachmentIds != null)
        {
            foreach (var id in pack.AttachmentIds)
                ids.Add(id);
        }
        if (pack.ParseSessionIds != null && pack.ParseSessionIds.Count > 0)
        {
            foreach (var sessionId in pack.ParseSessionIds)
            {
                var session = await _context.ParseSessions.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
                if (session?.EmailMessageId == null) continue;
                var drafts = await _context.ParsedOrderDrafts.AsNoTracking()
                    .Where(d => d.ParseSessionId == sessionId)
                    .Select(d => d.SourceFileName)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                foreach (var fileName in drafts)
                {
                    if (string.IsNullOrEmpty(fileName)) continue;
                    var attId = await _context.EmailAttachments.AsNoTracking()
                        .Where(a => a.EmailMessageId == session.EmailMessageId && a.FileName == fileName)
                        .Select(a => a.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (attId != default) ids.Add(attId);
                }
            }
        }
        return ids.ToList();
    }

    private static TemplateProfileContext ToContext(ParserTemplate entity, TemplateProfileConfig config)
    {
        var hints = config.ParseHints;
        return new TemplateProfileContext
        {
            ProfileId = config.ProfileId,
            ProfileName = config.ProfileName,
            PreferredSheetNames = hints?.PreferredSheetNames,
            HeaderRowRange = hints?.HeaderRowRange != null
                ? (hints.HeaderRowRange.Min, hints.HeaderRowRange.Max)
                : null,
            RequiredFieldSynonymOverrides = hints?.RequiredFieldSynonymOverrides != null
                ? new Dictionary<string, IReadOnlyList<string>>(hints.RequiredFieldSynonymOverrides.ToDictionary(k => k.Key, v => (IReadOnlyList<string>)v.Value))
                : null,
            DriftThresholds = config.DriftThresholds
        };
    }
}
