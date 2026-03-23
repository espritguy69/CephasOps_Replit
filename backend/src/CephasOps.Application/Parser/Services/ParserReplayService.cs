using System.Text.Json;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Parser.Services.Converters;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Replays historical EmailAttachments through the parser and records regression/improvement.
/// Does not modify Orders or Drafts; writes only to ParserReplayRuns.
/// </summary>
public class ParserReplayService : IParserReplayService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly ITimeExcelParserService _parser;
    private readonly ILogger<ParserReplayService> _logger;

    private const decimal ConfidenceRegressionThreshold = 0.10m;
    private const decimal LowConfidenceThreshold = 0.70m;

    public ParserReplayService(
        ApplicationDbContext context,
        IFileService fileService,
        ITimeExcelParserService parser,
        ILogger<ParserReplayService> logger)
    {
        _context = context;
        _fileService = fileService;
        _parser = parser;
        _logger = logger;
    }

    public async Task<ParserReplayResult> ReplayByAttachmentIdAsync(Guid attachmentId, string triggeredBy, ProfileLifecycleContext? lifecycleContext = null, CancellationToken cancellationToken = default)
    {
        var attachment = await _context.EmailAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        if (attachment == null)
        {
            _logger.LogWarning("Replay: Attachment {AttachmentId} not found", attachmentId);
            return new ParserReplayResult { AttachmentId = attachmentId, Error = "Attachment not found." };
        }

        if (!attachment.FileId.HasValue)
        {
            _logger.LogWarning("Replay: Attachment {AttachmentId} has no FileId, cannot load content", attachmentId);
            return new ParserReplayResult { AttachmentId = attachmentId, FileName = attachment.FileName, Error = "Attachment has no stored file (FileId missing)." };
        }

        var content = await _fileService.GetFileContentAsync(attachment.FileId.Value, attachment.CompanyId, cancellationToken);
        if (content == null || content.Length == 0)
        {
            _logger.LogWarning("Replay: No content for attachment {AttachmentId} (FileId {FileId})", attachmentId, attachment.FileId);
            return new ParserReplayResult { AttachmentId = attachmentId, FileName = attachment.FileName, Error = "File content empty or not found." };
        }

        // Find original draft(s) for this attachment: same email message, same filename
        var oldDraft = await FindOriginalDraftForAttachmentAsync(attachment.EmailMessageId, attachment.FileName, attachment.CompanyId, cancellationToken);
        var (oldStatus, oldConfidence, oldMissing, oldSheet, oldHeader) = GetOldResultFromDraft(oldDraft);

        var formFile = new InMemoryFormFile(content, attachment.FileName, attachment.ContentType ?? "application/octet-stream");
        TimeExcelParseResult newResult;
        try
        {
            newResult = await _parser.ParseAsync(formFile, null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replay: Parser threw for attachment {AttachmentId}", attachmentId);
            return new ParserReplayResult
            {
                AttachmentId = attachmentId,
                FileName = attachment.FileName,
                OldParseStatus = oldStatus,
                OldConfidence = oldConfidence,
                NewParseStatus = "ParseError",
                NewConfidence = 0,
                Error = ex.Message
            };
        }

        var newStatus = newResult.ParseStatus ?? (newResult.Success ? "Success" : "Unknown");
        var newConfidence = newResult.OrderData?.ConfidenceScore ?? newResult.ParseReport?.FinalConfidenceScore ?? 0;
        var newMissing = newResult.ParseReport?.MissingRequiredFields ?? new List<string>();
        var newSheet = newResult.ParseReport?.SelectedSheetName;
        var newHeader = newResult.ParseReport?.DetectedHeaderRow;

        var (regression, improvement) = DetectRegressionAndImprovement(
            oldStatus, oldConfidence, oldMissing,
            newStatus, newConfidence, newMissing);

        var (oldCategory, oldHeaderScore, oldBestSheetScore, oldRequiredFoundBy, oldProfileId, oldProfileName, oldDriftDetected, oldDriftSignature) = ParseValidationNotesDiagnostics(oldDraft?.ValidationNotes);
        var newCategory = newResult.ParseReport?.ParseFailureCategory ?? "NONE";
        var newHeaderScore = newResult.ParseReport?.HeaderScore;
        var newBestSheetScore = newResult.ParseReport?.SheetScoreBest;
        var newRequiredFoundBy = SummarizeFieldDiagnostics(newResult.ParseReport?.FieldDiagnostics);
        var newProfileId = newResult.ParseReport?.TemplateProfileId;
        var newProfileName = newResult.ParseReport?.TemplateProfileName;
        var newDriftDetected = newResult.ParseReport?.DriftDetected;
        var newDriftSignature = newResult.ParseReport?.DriftSignature;
        var reasonForChange = BuildReasonForChange(oldStatus, newStatus, oldCategory, newCategory, oldHeader, newHeader, oldHeaderScore, newHeaderScore, oldBestSheetScore, newBestSheetScore, newCategory, newDriftSignature);

        var run = new ParserReplayRun
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            TriggeredBy = triggeredBy,
            OriginalParseSessionId = oldDraft?.ParseSessionId,
            AttachmentId = attachmentId,
            OldParseStatus = oldStatus,
            OldConfidence = oldConfidence,
            NewParseStatus = newStatus,
            NewConfidence = newConfidence,
            OldMissingFields = SerializeMissing(oldMissing),
            NewMissingFields = SerializeMissing(newMissing),
            OldSheetName = oldSheet,
            NewSheetName = newSheet,
            OldHeaderRow = oldHeader,
            NewHeaderRow = newHeader,
            RegressionDetected = regression,
            ImprovementDetected = improvement,
            ResultSummary = BuildResultSummaryJson(oldStatus, newStatus, oldConfidence, newConfidence, oldMissing, newMissing,
                oldCategory, newCategory, oldHeaderScore, newHeaderScore, oldBestSheetScore, newBestSheetScore,
                oldRequiredFoundBy, newRequiredFoundBy, reasonForChange,
                oldProfileId, oldProfileName, oldDriftDetected, oldDriftSignature,
                newProfileId, newProfileName, newDriftDetected, newDriftSignature,
                lifecycleContext)
        };
        _context.ParserReplayRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Replay recorded: AttachmentId={AttachmentId}, Old={OldStatus} ({OldConf}) -> New={NewStatus} ({NewConf}), Regression={Reg}, Improvement={Imp}",
            attachmentId, oldStatus, oldConfidence, newStatus, newConfidence, regression, improvement);

        return new ParserReplayResult
        {
            ReplayRunId = run.Id,
            AttachmentId = attachmentId,
            OriginalParseSessionId = run.OriginalParseSessionId,
            FileName = attachment.FileName,
            OldParseStatus = oldStatus,
            OldConfidence = oldConfidence,
            NewParseStatus = newStatus,
            NewConfidence = newConfidence,
            OldMissingFields = oldMissing,
            NewMissingFields = newMissing,
            OldSheetName = oldSheet,
            NewSheetName = newSheet,
            OldHeaderRow = oldHeader,
            NewHeaderRow = newHeader,
            RegressionDetected = regression,
            ImprovementDetected = improvement,
            OldParseFailureCategory = oldCategory,
            NewParseFailureCategory = newCategory,
            ReasonForChange = reasonForChange,
            NewDriftSignature = newDriftSignature
        };
    }

    public async Task<IReadOnlyList<ParserReplayResult>> ReplayByParseSessionIdAsync(Guid parseSessionId, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var session = await _context.ParseSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == parseSessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Replay: ParseSession {ParseSessionId} not found", parseSessionId);
            return Array.Empty<ParserReplayResult>();
        }

        // Get drafts for this session to know which attachments (by SourceFileName) to replay
        var drafts = await _context.ParsedOrderDrafts
            .AsNoTracking()
            .Where(d => d.ParseSessionId == parseSessionId)
            .ToListAsync(cancellationToken);

        if (session.EmailMessageId == null)
        {
            _logger.LogWarning("Replay: ParseSession {ParseSessionId} has no EmailMessageId (file upload?), cannot resolve attachments", parseSessionId);
            return Array.Empty<ParserReplayResult>();
        }

        var attachments = await _context.EmailAttachments
            .AsNoTracking()
            .Where(a => a.EmailMessageId == session.EmailMessageId)
            .ToListAsync(cancellationToken);

        var results = new List<ParserReplayResult>();
        foreach (var draft in drafts)
        {
            var fileName = draft.SourceFileName;
            if (string.IsNullOrEmpty(fileName)) continue;
            var attachment = attachments.FirstOrDefault(a => string.Equals(a.FileName, fileName, StringComparison.OrdinalIgnoreCase));
            if (attachment == null) continue;
            var result = await ReplayByAttachmentIdAsync(attachment.Id, triggeredBy, null, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<ParserReplayPackResult> ReplayPackAsync(int days, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        // Failed or low-confidence: drafts with NeedsReview, Rejected, or ConfidenceScore < threshold
        var draftIds = await _context.ParsedOrderDrafts
            .AsNoTracking()
            .Where(d => d.CreatedAt >= since &&
                (d.ValidationStatus == "NeedsReview" || d.ValidationStatus == "Rejected" || d.ConfidenceScore < LowConfidenceThreshold))
            .Select(d => new { d.ParseSessionId, d.SourceFileName, d.CompanyId })
            .Distinct()
            .ToListAsync(cancellationToken);

        // Resolve to attachment IDs (session -> EmailMessageId -> attachments by filename)
        var attachmentIds = new List<Guid>();
        foreach (var d in draftIds)
        {
            var session = await _context.ParseSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == d.ParseSessionId, cancellationToken);
            if (session?.EmailMessageId == null) continue;
            var att = await _context.EmailAttachments
                .AsNoTracking()
                .Where(a => a.EmailMessageId == session.EmailMessageId && a.FileName == d.SourceFileName)
                .Select(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (att != default) attachmentIds.Add(att);
        }

        var total = attachmentIds.Count;
        var regressions = 0;
        var improvements = 0;
        var noChange = 0;
        var errors = 0;
        var results = new List<ParserReplayResult>();

        foreach (var aid in attachmentIds.Distinct())
        {
            var r = await ReplayByAttachmentIdAsync(aid, triggeredBy, null, cancellationToken);
            results.Add(r);
            if (r.Error != null) errors++;
            else if (r.RegressionDetected) regressions++;
            else if (r.ImprovementDetected) improvements++;
            else noChange++;
        }

        return new ParserReplayPackResult
        {
            Total = total,
            Regressions = regressions,
            Improvements = improvements,
            NoChange = noChange,
            Errors = errors,
            Results = results
        };
    }

    public async Task<IReadOnlyList<Guid>> GetAttachmentIdsForProfileAsync(Guid profileId, int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var profileMarker = "Profile=" + profileId.ToString();
        var drafts = await _context.ParsedOrderDrafts
            .AsNoTracking()
            .Where(d => d.CreatedAt >= since && d.ValidationNotes != null && d.ValidationNotes.Contains(profileMarker))
            .Select(d => new { d.ParseSessionId, d.SourceFileName })
            .Distinct()
            .ToListAsync(cancellationToken);

        var attachmentIds = new List<Guid>();
        foreach (var d in drafts)
        {
            if (string.IsNullOrEmpty(d.SourceFileName)) continue;
            var session = await _context.ParseSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == d.ParseSessionId, cancellationToken);
            if (session?.EmailMessageId == null) continue;
            var attId = await _context.EmailAttachments
                .AsNoTracking()
                .Where(a => a.EmailMessageId == session.EmailMessageId && a.FileName == d.SourceFileName)
                .Select(a => a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (attId != default) attachmentIds.Add(attId);
        }
        return attachmentIds.Distinct().ToList();
    }

    private async Task<ParsedOrderDraft?> FindOriginalDraftForAttachmentAsync(Guid emailMessageId, string fileName, Guid? companyId, CancellationToken ct)
    {
        var sessions = await _context.ParseSessions
            .AsNoTracking()
            .Where(s => s.EmailMessageId == emailMessageId && (companyId == null || s.CompanyId == companyId))
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (sessions.Count == 0) return null;

        return await _context.ParsedOrderDrafts
            .AsNoTracking()
            .Where(d => sessions.Contains(d.ParseSessionId) && d.SourceFileName == fileName)
            .OrderBy(d => d.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    private static (string status, decimal confidence, IReadOnlyList<string> missing, string? sheet, int? header) GetOldResultFromDraft(ParsedOrderDraft? draft)
    {
        if (draft == null)
            return ("Unknown", 0, Array.Empty<string>(), null, null);

        var status = draft.ValidationStatus ?? "Unknown";
        var confidence = draft.ConfidenceScore;
        var (missing, sheet, header) = ParseValidationNotes(draft.ValidationNotes);
        return (status, confidence, missing, sheet, header);
    }

    private static (IReadOnlyList<string> missing, string? sheet, int? header) ParseValidationNotes(string? notes)
    {
        if (string.IsNullOrEmpty(notes)) return (Array.Empty<string>(), null, null);
        var missing = new List<string>();
        string? sheet = null;
        int? header = null;
        foreach (var part in notes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;
            var key = kv[0].Trim();
            var value = kv[1].Trim();
            if (key.Equals("Missing", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var f in value.Split(',', StringSplitOptions.TrimEntries))
                    if (!string.IsNullOrEmpty(f)) missing.Add(f);
            }
            else if (key.Equals("Sheet", StringComparison.OrdinalIgnoreCase))
                sheet = value;
            else if (key.Equals("HeaderRow", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var hr))
                header = hr;
        }
        return (missing, sheet, header);
    }

    private static (string? category, int? headerScore, int? bestSheetScore, string? requiredFoundBy, Guid? profileId, string? profileName, bool? driftDetected, string? driftSignature) ParseValidationNotesDiagnostics(string? notes)
    {
        if (string.IsNullOrEmpty(notes)) return (null, null, null, null, null, null, null, null);
        string? category = null;
        int? headerScore = null;
        int? bestSheetScore = null;
        string? requiredFoundBy = null;
        Guid? profileId = null;
        string? profileName = null;
        bool? driftDetected = null;
        string? driftSignature = null;
        var segment = notes;
        var auditIdx = notes.IndexOf("[Audit] ", StringComparison.OrdinalIgnoreCase);
        if (auditIdx >= 0) segment = notes.Substring(auditIdx + "[Audit] ".Length);
        foreach (var part in segment.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;
            var key = kv[0].Trim();
            var value = kv[1].Trim();
            if (key.Equals("Category", StringComparison.OrdinalIgnoreCase)) category = value;
            else if (key.Equals("HeaderScore", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var hs)) headerScore = hs;
            else if (key.Equals("BestSheetScore", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var bss)) bestSheetScore = bss;
            else if (key.Equals("RequiredFoundBy", StringComparison.OrdinalIgnoreCase)) requiredFoundBy = value;
            else if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(value, out var pid)) profileId = pid;
            else if (key.Equals("ProfileName", StringComparison.OrdinalIgnoreCase)) profileName = value;
            else if (key.Equals("DriftDetected", StringComparison.OrdinalIgnoreCase)) driftDetected = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("DriftSignature", StringComparison.OrdinalIgnoreCase)) driftSignature = value;
        }
        return (category, headerScore, bestSheetScore, requiredFoundBy, profileId, profileName, driftDetected, driftSignature);
    }

    private static string? SummarizeFieldDiagnostics(IReadOnlyList<FieldDiagnosticEntry>? diagnostics)
    {
        if (diagnostics == null || diagnostics.Count == 0) return null;
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ServiceId", "TicketId", "CustomerName", "ServiceAddress", "CustomerPhone" };
        var parts = diagnostics.Where(d => required.Contains(d.FieldName)).Select(d => d.Found ? $"{d.FieldName}:{d.MatchType}" : $"{d.FieldName}:n");
        return string.Join(",", parts);
    }

    private static string BuildReasonForChange(string oldStatus, string newStatus, string? oldCategory, string? newCategory,
        int? oldHeaderRow, int? newHeaderRow, int? oldHeaderScore, int? newHeaderScore, int? oldBestSheetScore, int? newBestSheetScore,
        string? newCategoryForDrift, string? newDriftSignature)
    {
        var parts = new List<string>();
        if (oldCategory != null && newCategory != null && oldCategory != newCategory)
            parts.Add($"Category {oldCategory} -> {newCategory}");
        if (oldHeaderRow.HasValue && newHeaderRow.HasValue && oldHeaderRow.Value != newHeaderRow.Value)
            parts.Add($"Header row {oldHeaderRow} -> {newHeaderRow}");
        if (oldHeaderScore.HasValue && newHeaderScore.HasValue && oldHeaderScore.Value != newHeaderScore.Value)
            parts.Add($"HeaderScore {oldHeaderScore} -> {newHeaderScore}");
        if (oldBestSheetScore.HasValue && newBestSheetScore.HasValue && oldBestSheetScore.Value != newBestSheetScore.Value)
            parts.Add($"BestSheetScore {oldBestSheetScore} -> {newBestSheetScore}");
        if (oldStatus != newStatus)
            parts.Add($"Status {oldStatus} -> {newStatus}");
        if (string.Equals(newCategoryForDrift, "LAYOUT_DRIFT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(newDriftSignature))
            parts.Add($"DriftSignature={newDriftSignature}");
        if (parts.Count == 0) return "No significant change";
        return string.Join("; ", parts);
    }

    private static (bool regression, bool improvement) DetectRegressionAndImprovement(
        string oldStatus, decimal oldConfidence, IReadOnlyList<string> oldMissing,
        string newStatus, decimal newConfidence, IReadOnlyList<string> newMissing)
    {
        var oldSuccess = IsSuccessStatus(oldStatus);
        var newSuccess = IsSuccessStatus(newStatus);

        bool regression = false;
        if (oldSuccess && !newSuccess) regression = true;
        if (newSuccess && oldSuccess && newConfidence < oldConfidence - ConfidenceRegressionThreshold) regression = true;
        if (newMissing.Count > oldMissing.Count) regression = true;

        bool improvement = false;
        if (!oldSuccess && newSuccess) improvement = true;
        if (oldSuccess && newSuccess && newConfidence > oldConfidence + ConfidenceRegressionThreshold) improvement = true;
        if (newMissing.Count < oldMissing.Count) improvement = true;

        return (regression, improvement);
    }

    private static bool IsSuccessStatus(string status)
    {
        return string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Valid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase);
    }

    private static string? SerializeMissing(IReadOnlyList<string>? list)
    {
        if (list == null || list.Count == 0) return null;
        return JsonSerializer.Serialize(list);
    }

    private static string? BuildResultSummaryJson(string oldStatus, string newStatus, decimal oldConf, decimal newConf,
        IReadOnlyList<string> oldMissing, IReadOnlyList<string> newMissing,
        string? oldCategory, string? newCategory,
        int? oldHeaderScore, int? newHeaderScore,
        int? oldBestSheetScore, int? newBestSheetScore,
        string? oldRequiredFoundBy, string? newRequiredFoundBy,
        string? reasonForChange,
        Guid? oldProfileId, string? oldProfileName, bool? oldDriftDetected, string? oldDriftSignature,
        Guid? newProfileId, string? newProfileName, bool? newDriftDetected, string? newDriftSignature,
        ProfileLifecycleContext? lifecycleContext = null)
    {
        var o = new
        {
            oldStatus,
            newStatus,
            oldConfidence = oldConf,
            newConfidence = newConf,
            oldMissing,
            newMissing,
            oldParseFailureCategory = oldCategory,
            newParseFailureCategory = newCategory,
            oldHeaderScore,
            newHeaderScore,
            oldBestSheetScore,
            newBestSheetScore,
            oldRequiredFoundBy,
            newRequiredFoundBy,
            reasonForChange,
            oldProfileId,
            oldProfileName,
            oldDriftDetected,
            oldDriftSignature,
            newProfileId,
            newProfileName,
            newDriftDetected,
            newDriftSignature,
            profileId = lifecycleContext?.ProfileId,
            profileName = lifecycleContext?.ProfileName,
            profileVersion = lifecycleContext?.ProfileVersion,
            effectiveFrom = lifecycleContext?.EffectiveFrom,
            owner = lifecycleContext?.Owner,
            packName = lifecycleContext?.PackName,
            profileChangeNotes = lifecycleContext?.ProfileChangeNotes
        };
        return JsonSerializer.Serialize(o);
    }
}
