using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Shared enrichment service for ParsedOrderDraft entities.
/// Centralizes building matching, PDF fallback, date normalization, and validation status logic.
/// Used by both Upload and Email pipelines to ensure feature parity.
/// </summary>
public class ParsedOrderDraftEnrichmentService : IParsedOrderDraftEnrichmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBuildingMatchingService _buildingMatchingService;
    private readonly IBuildingService _buildingService;
    private readonly IExcelToPdfService _excelToPdfService;
    private readonly IPdfTextExtractionService _pdfTextExtractionService;
    private readonly IPdfOrderParserService _pdfOrderParserService;
    private readonly ILogger<ParsedOrderDraftEnrichmentService> _logger;

    public ParsedOrderDraftEnrichmentService(
        ApplicationDbContext context,
        IBuildingMatchingService buildingMatchingService,
        IBuildingService buildingService,
        IExcelToPdfService excelToPdfService,
        IPdfTextExtractionService pdfTextExtractionService,
        IPdfOrderParserService pdfOrderParserService,
        ILogger<ParsedOrderDraftEnrichmentService> logger)
    {
        _context = context;
        _buildingMatchingService = buildingMatchingService;
        _buildingService = buildingService;
        _excelToPdfService = excelToPdfService;
        _pdfTextExtractionService = pdfTextExtractionService;
        _pdfOrderParserService = pdfOrderParserService;
        _logger = logger;
    }

    public async Task EnrichDraftAsync(
        ParsedOrderDraft draft,
        TimeExcelParseResult parseResult,
        IFormFile sourceFile,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Normalize dates (before other enrichment)
        NormalizeDates(draft, parseResult);

        // Step 2: PDF fallback for missing critical fields
        await TryPdfFallbackAsync(draft, sourceFile, cancellationToken);

        // Step 3: Building matching + auto-creation
        await TryBuildingMatchingAsync(draft, companyId, sourceFile.FileName, cancellationToken);
    }

    private void NormalizeDates(ParsedOrderDraft draft, TimeExcelParseResult parseResult)
    {
        _logger.LogInformation("EnrichmentStep: Step=DateNormalization, FileName={FileName}", draft.SourceFileName);
        
        // Priority 1: Use date from parseResult if available (most accurate source)
        if (parseResult.OrderData?.AppointmentDateTime.HasValue == true)
        {
            draft.AppointmentDate = NormalizeToUtc(parseResult.OrderData.AppointmentDateTime);
            _logger.LogDebug("Normalized appointment date from parseResult: {Original} → {Normalized}", 
                parseResult.OrderData.AppointmentDateTime, draft.AppointmentDate);
        }
        // Priority 2: Normalize existing draft.AppointmentDate if it exists (fixes incorrect conversions)
        else if (draft.AppointmentDate.HasValue)
        {
            var originalDate = draft.AppointmentDate.Value;
            draft.AppointmentDate = NormalizeToUtc(draft.AppointmentDate);
            if (draft.AppointmentDate.HasValue && originalDate != draft.AppointmentDate.Value)
            {
                _logger.LogInformation("Fixed appointment date timezone: {Original} (Kind: {Kind}) → {Normalized} (UTC)", 
                    originalDate, originalDate.Kind, draft.AppointmentDate);
            }
        }
    }

    private static DateTime? NormalizeToUtc(DateTime? value) =>
        value.HasValue ? EnsureUtc(value.Value) : null;

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private async Task TryPdfFallbackAsync(
        ParsedOrderDraft draft,
        IFormFile sourceFile,
        CancellationToken cancellationToken)
    {
        // PDF Fallback: If critical fields are missing, try PDF extraction
        if (string.IsNullOrWhiteSpace(draft.ServiceId) || !draft.AppointmentDate.HasValue)
        {
            _logger.LogInformation(
                "Critical fields missing (ServiceId or AppointmentDate), attempting PDF fallback for {FileName}",
                sourceFile.FileName);

            var filledFields = new List<string>();

            try
            {
                // Convert Excel to PDF
                var pdfBytes = await _excelToPdfService.ConvertToPdfAsync(sourceFile, cancellationToken);

                // Extract text from PDF
                var pdfText = await _pdfTextExtractionService.ExtractTextFromBytesAsync(pdfBytes, cancellationToken);

                // Parse from PDF text
                var pdfData = _pdfOrderParserService.ParseFromText(pdfText, sourceFile.FileName);

                // Fill missing fields from PDF
                if (string.IsNullOrWhiteSpace(draft.ServiceId) && !string.IsNullOrWhiteSpace(pdfData.ServiceId))
                {
                    draft.ServiceId = pdfData.ServiceId;
                    filledFields.Add("ServiceId");
                    _logger.LogInformation("ServiceId found in PDF: {ServiceId}", pdfData.ServiceId);
                }

                if (!draft.AppointmentDate.HasValue && pdfData.AppointmentDateTime.HasValue)
                {
                    draft.AppointmentDate = NormalizeToUtc(pdfData.AppointmentDateTime);
                    draft.AppointmentWindow = pdfData.AppointmentWindow;
                    filledFields.Add("AppointmentDate");
                    filledFields.Add("AppointmentWindow");
                    _logger.LogInformation("Appointment found in PDF: {Date}, {Window}",
                        pdfData.AppointmentDateTime, pdfData.AppointmentWindow);
                }

                // Fill other missing fields if available
                if (string.IsNullOrWhiteSpace(draft.CustomerName) && !string.IsNullOrWhiteSpace(pdfData.CustomerName))
                {
                    draft.CustomerName = pdfData.CustomerName;
                    filledFields.Add("CustomerName");
                }

                if (string.IsNullOrWhiteSpace(draft.CustomerPhone) && !string.IsNullOrWhiteSpace(pdfData.CustomerPhone))
                {
                    draft.CustomerPhone = pdfData.CustomerPhone;
                    filledFields.Add("CustomerPhone");
                }

                if (string.IsNullOrWhiteSpace(draft.AddressText) && !string.IsNullOrWhiteSpace(pdfData.ServiceAddress))
                {
                    draft.AddressText = pdfData.ServiceAddress;
                    filledFields.Add("AddressText");
                }

                if (string.IsNullOrWhiteSpace(draft.OldAddress) && !string.IsNullOrWhiteSpace(pdfData.OldAddress))
                {
                    draft.OldAddress = pdfData.OldAddress;
                    filledFields.Add("OldAddress");
                }

                // Update confidence score if PDF helped
                if (!string.IsNullOrWhiteSpace(draft.ServiceId) && draft.AppointmentDate.HasValue)
                {
                    draft.ConfidenceScore = Math.Max(draft.ConfidenceScore, pdfData.ConfidenceScore);
                    draft.ValidationNotes = (draft.ValidationNotes ?? "") + " [PDF fallback used to fill missing fields]";
                }

                _logger.LogInformation(
                    "EnrichmentStep: Step=PdfFallback, Attempted=true, FilledFields=[{FilledFields}], FileName={FileName}",
                    string.Join(", ", filledFields), sourceFile.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF fallback failed for {FileName}: {Error}", sourceFile.FileName, ex.Message);
                _logger.LogInformation(
                    "EnrichmentStep: Step=PdfFallback, Attempted=true, FilledFields=[], FileName={FileName}, Error={Error}",
                    sourceFile.FileName, ex.Message);
                // Continue with Excel-parsed data even if PDF fails
            }
        }
        else
        {
            _logger.LogInformation(
                "EnrichmentStep: Step=PdfFallback, Attempted=false, FileName={FileName}",
                sourceFile.FileName);
        }
    }

    private async Task TryBuildingMatchingAsync(
        ParsedOrderDraft draft,
        Guid companyId,
        string sourceFileName,
        CancellationToken cancellationToken)
    {
        // BUILDING MATCHING: Try to match building against existing buildings, auto-create if not found
        try
        {
            var (buildingName, city, postcode) = ExtractBuildingInfoFromAddress(draft.AddressText);
            draft.BuildingName = buildingName;

            var matchedBuilding = await _buildingMatchingService.FindMatchingBuildingAsync(
                buildingName,
                draft.AddressText,
                city,
                postcode,
                null, // buildingCode not available in TIME Excel
                companyId,
                cancellationToken);

            if (matchedBuilding != null)
            {
                draft.BuildingId = matchedBuilding.Id;
                draft.BuildingStatus = "Existing";
                _logger.LogInformation("Building matched for draft: BuildingName={BuildingName}, BuildingId={BuildingId}",
                    buildingName, matchedBuilding.Id);
                _logger.LogInformation(
                    "EnrichmentStep: Step=BuildingMatching, Matched=true, BuildingId={BuildingId}, AutoCreated=false, FileName={FileName}",
                    matchedBuilding.Id, sourceFileName);
            }
            else
            {
                // Auto-create building if not found (similar to RFB email processing)
                var createdBuildingId = await AutoCreateBuildingAsync(
                    draft.AddressText,
                    buildingName,
                    companyId,
                    sourceFileName,
                    cancellationToken);

                if (createdBuildingId.HasValue)
                {
                    draft.BuildingId = createdBuildingId.Value;
                    draft.BuildingStatus = "Existing";
                    _logger.LogInformation("✅ Building auto-created for draft: BuildingName={BuildingName}, BuildingId={BuildingId}",
                        buildingName, createdBuildingId.Value);
                    _logger.LogInformation(
                        "EnrichmentStep: Step=BuildingMatching, Matched=false, BuildingId={BuildingId}, AutoCreated=true, FileName={FileName}",
                        createdBuildingId.Value, sourceFileName);
                }
                else
                {
                    draft.BuildingStatus = "New";
                    _logger.LogInformation("No building match found and auto-creation failed. BuildingStatus=New, BuildingName={BuildingName}", buildingName);
                    _logger.LogInformation(
                        "EnrichmentStep: Step=BuildingMatching, Matched=false, BuildingId=null, AutoCreated=false, FileName={FileName}",
                        sourceFileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Building matching/creation failed for {FileName}, will mark as New", sourceFileName);
            draft.BuildingStatus = "New";
        }
    }

    private async Task<Guid?> AutoCreateBuildingAsync(
        string? addressText,
        string? buildingName,
        Guid? companyId,
        string sourceFileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(addressText) || string.IsNullOrWhiteSpace(buildingName))
        {
            _logger.LogWarning("Cannot auto-create building: missing address or building name");
            return null;
        }

        if (companyId == null || companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required for auto-create building.");

        try
        {
            // Parse address to extract components
            var addressComponents = AddressParser.ParseAddress(addressText);

            // Use parsed building name or fallback to extracted one
            var finalBuildingName = addressComponents.BuildingName ?? buildingName;

            if (string.IsNullOrWhiteSpace(finalBuildingName))
            {
                _logger.LogWarning("Cannot auto-create building: no building name detected in address");
                return null;
            }

            // Check if building already exists (duplicate check)
            var existingBuilding = await _context.Buildings
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .Where(b => EF.Functions.ILike(b.Name, finalBuildingName))
                .FirstOrDefaultAsync(cancellationToken);

            if (existingBuilding != null)
            {
                _logger.LogInformation("Building already exists, using existing: {BuildingId} ({BuildingName})",
                    existingBuilding.Id, finalBuildingName);
                return existingBuilding.Id;
            }

            // Create new building
            var createDto = new CreateBuildingDto
            {
                CompanyId = companyId,
                Name = finalBuildingName,
                Code = null, // Building code not available from parsed data
                AddressLine1 = addressComponents.AddressLine1 ?? addressText,
                AddressLine2 = addressComponents.AddressLine2,
                City = addressComponents.City ?? string.Empty,
                State = addressComponents.State ?? string.Empty,
                Postcode = addressComponents.Postcode ?? string.Empty,
                Area = null,
                Latitude = null,
                Longitude = null,
                PropertyType = null, // Will need to be filled manually if needed
                BuildingTypeId = null,
                InstallationMethodId = null,
                DepartmentId = null,
                RfbAssignedDate = null,
                FirstOrderDate = null,
                Notes = $"Auto-created from parsed order: {sourceFileName}",
                IsActive = true
            };

            var newBuilding = await _buildingService.CreateBuildingAsync(createDto, companyId, cancellationToken);

            _logger.LogInformation("✅ Auto-created building: {BuildingId} ({BuildingName}) from parsed order: {SourceFile}",
                newBuilding.Id, finalBuildingName, sourceFileName);

            return newBuilding.Id;
        }
        catch (InvalidOperationException ex)
        {
            // Duplicate building error - try to find it again
            _logger.LogWarning(ex, "Building creation failed (likely duplicate), attempting to find existing building: {BuildingName}", buildingName);

            var existingBuilding = await _context.Buildings
                .Where(b => b.CompanyId == companyId && !b.IsDeleted)
                .Where(b => EF.Functions.ILike(b.Name, buildingName))
                .FirstOrDefaultAsync(cancellationToken);

            if (existingBuilding != null)
            {
                _logger.LogInformation("Found existing building after duplicate error: {BuildingId} ({BuildingName})",
                    existingBuilding.Id, buildingName);
                return existingBuilding.Id;
            }

            _logger.LogError(ex, "Failed to auto-create building and could not find existing: {BuildingName}", buildingName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-creating building: {BuildingName} from {SourceFile}", buildingName, sourceFileName);
            return null;
        }
    }

    private (string? buildingName, string? city, string? postcode) ExtractBuildingInfoFromAddress(string? addressText)
    {
        if (string.IsNullOrWhiteSpace(addressText))
            return (null, null, null);

        var parts = addressText.Split(',').Select(p => p.Trim()).ToArray();

        // Keywords to identify building names
        var buildingKeywords = new[] { "residence", "tower", "condominium", "condo", "apartment", "apt",
                                       "plaza", "building", "court", "vista", "suites", "heights",
                                       "mansion", "park", "centre", "center", "complex" };

        // Keywords to skip (unit/level info)
        var unitKeywords = new[] { "level", "floor", "unit", "block", "lot", "no.", "no ", "#" };

        string? buildingName = null;
        string? postcode = null;
        string? city = null;

        // Find building name
        foreach (var part in parts)
        {
            var lowerPart = part.ToLowerInvariant();

            // Skip unit/level information
            if (unitKeywords.Any(kw => lowerPart.Contains(kw)))
                continue;

            // Find building name by keywords
            if (buildingKeywords.Any(kw => lowerPart.Contains(kw)))
            {
                buildingName = part;
                break;
            }
        }

        // Extract postcode (5 digits)
        postcode = parts.FirstOrDefault(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"^\d{5}$"));

        // Extract city (usually after postcode or second-to-last)
        var postcodeIndex = Array.IndexOf(parts, postcode ?? "");
        if (postcodeIndex >= 0 && postcodeIndex + 1 < parts.Length)
        {
            city = parts[postcodeIndex + 1];
        }
        else if (parts.Length >= 2)
        {
            city = parts[parts.Length - 2];
        }

        return (buildingName, city, postcode);
    }

    public void SetValidationStatus(
        ParsedOrderDraft draft,
        TimeExcelParseResult parseResult,
        string sourceFileName,
        bool autoApprove = false)
    {
        string decisionReason;
        
        // Set validation status based on validation errors (correct precedence: errors first)
        if (parseResult.ValidationErrors?.Count > 0)
        {
            draft.ValidationStatus = "NeedsReview";
            draft.ValidationNotes = $"Parsed from {sourceFileName}. Validation issues: {string.Join("; ", parseResult.ValidationErrors)}";
            decisionReason = "ValidationErrorsPresent";
        }
        else if (parseResult.Success)
        {
            // If template has AutoApprove enabled and parsing succeeded, mark as Valid (ready for auto-approval)
            // Otherwise, mark as Pending (requires manual review)
            draft.ValidationStatus = autoApprove ? "Valid" : "Pending";
            var orderTypeCode = parseResult.OrderData?.OrderTypeCode ?? "Unknown";
            draft.ValidationNotes = $"Successfully parsed from {sourceFileName}. Order Type: {orderTypeCode}";
            decisionReason = "SuccessAnd" + (autoApprove ? "AutoApproveEnabled" : "AutoApproveDisabled");
        }
        else
        {
            draft.ValidationStatus = "NeedsReview";
            draft.ValidationNotes = parseResult.ErrorMessage ?? $"Parsed with issues from {sourceFileName}";
            decisionReason = "ParseFailed";
        }

        // Append compact audit line from ParseReport for operational audit (existing columns only; max 4000)
        if (parseResult.ParseReport != null)
        {
            var auditLine = BuildParseReportAuditLine(parseResult.ParseReport);
            const string auditPrefix = " | [Audit] ";
            const int maxValidationNotes = 4000;
            var existing = draft.ValidationNotes ?? string.Empty;
            var maxExistingLength = maxValidationNotes - auditPrefix.Length - auditLine.Length;
            if (maxExistingLength < 0) maxExistingLength = 0;
            if (existing.Length > maxExistingLength)
                existing = existing.Substring(0, maxExistingLength);
            draft.ValidationNotes = existing + auditPrefix + auditLine;
        }

        _logger.LogInformation(
            "ValidationStatusInstrumentation: ValidationStatus={Status}, ValidationErrorsCount={ErrorCount}, AutoApprove={AutoApprove}, DecisionReason={Reason}, FileName={FileName}",
            draft.ValidationStatus, parseResult.ValidationErrors?.Count ?? 0, autoApprove, decisionReason, sourceFileName);
    }

    /// <summary>
    /// Build a compact, structured audit line from ParseReport for persistence in ValidationNotes (audit trail).
    /// Convention: ParseStatus=...; Missing=...; Engine=...; Sheet=...; HeaderRow=...; Converted=...; Category=...; RequiredFoundBy=...; HeaderScore=...; BestSheetScore=...
    /// Kept under 4000 chars total by caller.
    /// </summary>
    private static string BuildParseReportAuditLine(ParseReport r)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(r.ParseStatus))
            parts.Add($"ParseStatus={r.ParseStatus}");
        if (r.MissingRequiredFields != null && r.MissingRequiredFields.Count > 0)
            parts.Add($"Missing={string.Join(",", r.MissingRequiredFields)}");
        if (!string.IsNullOrEmpty(r.EngineUsed))
            parts.Add($"Engine={r.EngineUsed}");
        if (!string.IsNullOrEmpty(r.SelectedSheetName))
            parts.Add($"Sheet={r.SelectedSheetName}");
        if (r.DetectedHeaderRow.HasValue)
            parts.Add($"HeaderRow={r.DetectedHeaderRow.Value}");
        if (r.ConversionPerformed)
            parts.Add("Converted=yes");
        if (!string.IsNullOrEmpty(r.ParseFailureCategory) && r.ParseFailureCategory != "NONE")
            parts.Add($"Category={r.ParseFailureCategory}");
        var requiredFoundBy = GetRequiredFoundBySummary(r.FieldDiagnostics);
        if (!string.IsNullOrEmpty(requiredFoundBy))
            parts.Add($"RequiredFoundBy={requiredFoundBy}");
        if (r.HeaderScore.HasValue)
            parts.Add($"HeaderScore={r.HeaderScore.Value}");
        if (r.SheetScoreBest.HasValue)
            parts.Add($"BestSheetScore={r.SheetScoreBest.Value}");
        // Phase 8: profile and drift (PII-safe)
        if (r.TemplateProfileId.HasValue)
            parts.Add($"Profile={r.TemplateProfileId.Value}");
        if (!string.IsNullOrEmpty(r.TemplateProfileName))
            parts.Add($"ProfileName={r.TemplateProfileName}");
        if (r.DriftDetected.HasValue)
            parts.Add($"DriftDetected={r.DriftDetected.Value}");
        if (!string.IsNullOrEmpty(r.DriftSignature))
            parts.Add($"DriftSignature={r.DriftSignature}");
        if (string.Equals(r.ParseFailureCategory, "LAYOUT_DRIFT", StringComparison.OrdinalIgnoreCase) && r.DriftDetected == true)
            parts.Add("TemplateAction=UpdateProfile");
        return string.Join("; ", parts);
    }

    private static string? GetRequiredFoundBySummary(IReadOnlyList<FieldDiagnosticEntry>? diagnostics)
    {
        if (diagnostics == null || diagnostics.Count == 0) return null;
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ServiceId", "TicketId", "CustomerName", "ServiceAddress", "CustomerPhone" };
        var parts = diagnostics.Where(d => required.Contains(d.FieldName)).Select(d => d.Found ? $"{d.FieldName}:{d.MatchType}" : $"{d.FieldName}:n");
        var s = string.Join(",", parts);
        return string.IsNullOrEmpty(s) ? null : s;
    }
}

