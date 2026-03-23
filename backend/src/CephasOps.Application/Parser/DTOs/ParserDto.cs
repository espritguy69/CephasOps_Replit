using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Parser.DTOs;

/// <summary>
/// Parse session DTO
/// </summary>
public class ParseSessionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid? EmailMessageId { get; set; }
    public Guid? ParserTemplateId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Guid? SnapshotFileId { get; set; }
    public int ParsedOrdersCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Source type: Email, FileUpload
    /// </summary>
    public string? SourceType { get; set; }
    
    /// <summary>
    /// Description of the source (e.g., file names, email subject)
    /// </summary>
    public string? SourceDescription { get; set; }
}

/// <summary>
/// Parsed order draft DTO
/// </summary>
public class ParsedOrderDraftDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public Guid ParseSessionId { get; set; }
    public Guid? PartnerId { get; set; }
    /// <summary>Partner display code (e.g. TIME, TIME-FTTH) for parser review UI.</summary>
    public string? PartnerCode { get; set; }
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? BuildingStatus { get; set; } // "Existing" (matched) or "New" (needs creation)
    public string? ServiceId { get; set; }
    public string? TicketId { get; set; }
    
    /// <summary>
    /// AWO Number - required for Assurance orders
    /// </summary>
    public string? AwoNumber { get; set; }
    
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Additional contact number for Assurance orders
    /// </summary>
    public string? AdditionalContactNumber { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders (e.g., "Link Down", "LOSi", "LOBi")
    /// </summary>
    public string? Issue { get; set; }
    
    public string? AddressText { get; set; }
    public string? OldAddress { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? AppointmentWindow { get; set; }
    public string? OrderTypeHint { get; set; }
    public string? OrderTypeCode { get; set; }
    /// <summary>Order category ID (FTTH, FTTO, etc.) for parser review/edit.</summary>
    public Guid? OrderCategoryId { get; set; }
    public string? PackageName { get; set; }
    public string? Bandwidth { get; set; }
    public string? OnuSerialNumber { get; set; }
    public string? OnuPassword { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? InternetWanIp { get; set; }
    public string? InternetLanIp { get; set; }
    public string? InternetGateway { get; set; }
    public string? InternetSubnetMask { get; set; }
    public string? VoipServiceId { get; set; }
    public string? Remarks { get; set; }
    /// <summary>Extra/unmapped parser info (e.g. AnyUnhandledSections) - read-only.</summary>
    public string? AdditionalInformation { get; set; }
    public string? SourceFileName { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string? ValidationNotes { get; set; }
    public Guid? CreatedOrderId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ParsedDraftMaterialDto> Materials { get; set; } = new();

    /// <summary>
    /// Number of parsed materials that could not be matched to Material master. For parser review/order detail warning.
    /// </summary>
    public int UnmatchedMaterialCount { get; set; }

    /// <summary>
    /// Names of parsed materials that could not be matched (for display in warning).
    /// </summary>
    public List<string> UnmatchedMaterialNames { get; set; } = new();

    /// <summary>
    /// Fuzzy-matched building candidates when BuildingStatus is "New" (e.g. "ROYCE RESIDENCE" → "Royce Residences").
    /// Frontend can show a modal for user to pick one or create new.
    /// </summary>
    public List<BuildingMatchCandidateDto> SuggestedBuildings { get; set; } = new();

    /// <summary>
    /// When set, an order with the same ServiceId already exists. Approving this draft will update that order (merge).
    /// Frontend can show a duplicate warning and link to the existing order.
    /// </summary>
    public Guid? ExistingOrderId { get; set; }
}

/// <summary>
/// Update parsed order draft request DTO
/// </summary>
public class UpdateParsedOrderDraftDto
{
    public string? ServiceId { get; set; }
    public string? TicketId { get; set; }
    
    /// <summary>
    /// AWO Number - required for Assurance orders
    /// </summary>
    public string? AwoNumber { get; set; }
    
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    
    /// <summary>
    /// Additional contact number for Assurance orders
    /// </summary>
    public string? AdditionalContactNumber { get; set; }
    
    /// <summary>
    /// Issue description for Assurance orders
    /// </summary>
    public string? Issue { get; set; }
    
    public string? AddressText { get; set; }
    public string? OldAddress { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? AppointmentWindow { get; set; }
    public string? OrderTypeCode { get; set; }
    public Guid? OrderCategoryId { get; set; }
    public string? PackageName { get; set; }
    public string? Bandwidth { get; set; }
    public string? OnuSerialNumber { get; set; }
    public string? OnuPassword { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? InternetWanIp { get; set; }
    public string? InternetLanIp { get; set; }
    public string? InternetGateway { get; set; }
    public string? InternetSubnetMask { get; set; }
    public string? VoipServiceId { get; set; }
    public string? Remarks { get; set; }
    public Guid? BuildingId { get; set; }
}

/// <summary>
/// Approve parsed order request DTO
/// </summary>
public class ApproveParsedOrderDto
{
    public string? ValidationNotes { get; set; }
}

/// <summary>
/// Reject parsed order request DTO
/// </summary>
public class RejectParsedOrderDto
{
    public string ValidationNotes { get; set; } = string.Empty;
}

/// <summary>
/// Request to bulk-approve parsed order drafts
/// </summary>
public class BulkApproveParsedOrdersRequestDto
{
    public List<Guid> DraftIds { get; set; } = new();
}

/// <summary>
/// Result of bulk-approving parsed order drafts
/// </summary>
public class BulkApproveParsedOrdersResultDto
{
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public List<Guid> SucceededDraftIds { get; set; } = new();
    /// <summary>Draft ID and error message for each failure</summary>
    public List<BulkApproveErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// Per-draft error when bulk approving
/// </summary>
public class BulkApproveErrorDto
{
    public Guid DraftId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ParsedDraftMaterialDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ActionTag { get; set; }
    public decimal? Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Email parsing diagnostic DTO
/// </summary>
public class EmailParsingDiagnosticDto
{
    public Guid EmailAccountId { get; set; }
    public string EmailAccountName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int EmailsFetched { get; set; }
    public int ParseSessionsCreated { get; set; }
    public int DraftsCreated { get; set; }
    public int Errors { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ProcessedEmails { get; set; } = new();
    public List<FailedSessionInfo> FailedSessions { get; set; } = new();
    public DateTime TestStartedAt { get; set; }
    public DateTime TestCompletedAt { get; set; }
}

/// <summary>
/// Failed session information
/// </summary>
public class FailedSessionInfo
{
    public Guid SessionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SourceDescription { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ParsedOrdersCount { get; set; }
}

/// <summary>
/// Paginated result DTO
/// </summary>
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// Parser statistics DTO
/// </summary>
public class ParserStatisticsDto
{
    /// <summary>
    /// Total parse sessions today
    /// </summary>
    public int TotalSessionsToday { get; set; }
    
    /// <summary>
    /// Successful parse sessions today (Status = "Completed")
    /// </summary>
    public int SuccessfulSessionsToday { get; set; }
    
    /// <summary>
    /// Failed parse sessions today (Status = "Failed" or "Error")
    /// </summary>
    public int FailedSessionsToday { get; set; }
    
    /// <summary>
    /// Total parsed order drafts
    /// </summary>
    public int TotalDrafts { get; set; }
    
    /// <summary>
    /// Pending drafts (ValidationStatus = "Pending")
    /// </summary>
    public int PendingDrafts { get; set; }
    
    /// <summary>
    /// Valid drafts (ValidationStatus = "Valid")
    /// </summary>
    public int ValidDrafts { get; set; }
    
    /// <summary>
    /// Drafts needing review (ValidationStatus = "NeedsReview")
    /// </summary>
    public int NeedsReviewDrafts { get; set; }
    
    /// <summary>
    /// Rejected drafts (ValidationStatus = "Rejected")
    /// </summary>
    public int RejectedDrafts { get; set; }
    
    /// <summary>
    /// Drafts that have been approved and created orders
    /// </summary>
    public int ApprovedDrafts { get; set; }
    
    /// <summary>
    /// Average confidence score across all drafts
    /// </summary>
    public decimal AverageConfidenceScore { get; set; }
    
    /// <summary>
    /// Total parse sessions (all time)
    /// </summary>
    public int TotalSessionsAllTime { get; set; }
    
    /// <summary>
    /// Total parsed order drafts (all time)
    /// </summary>
    public int TotalDraftsAllTime { get; set; }
}

/// <summary>
/// Parser analytics for dashboard: success rate, auto-match rate, confidence distribution, common errors, orders per day.
/// </summary>
public class ParserAnalyticsDto
{
    /// <summary>Parse success rate (0-100): completed sessions / total sessions in period.</summary>
    public decimal ParseSuccessRate { get; set; }

    /// <summary>Auto-match rate (0-100): drafts with building matched / total drafts in period.</summary>
    public decimal AutoMatchRate { get; set; }

    /// <summary>Total sessions in period.</summary>
    public int TotalSessions { get; set; }

    /// <summary>Completed sessions in period.</summary>
    public int CompletedSessions { get; set; }

    /// <summary>Failed sessions in period.</summary>
    public int FailedSessions { get; set; }

    /// <summary>Total drafts in period.</summary>
    public int TotalDrafts { get; set; }

    /// <summary>Drafts with building matched (BuildingStatus = "Existing" or BuildingId set).</summary>
    public int BuildingMatchedDrafts { get; set; }

    /// <summary>Confidence score buckets: label and count (e.g. "0-50%", "50-80%", "80-100%").</summary>
    public List<ConfidenceBucketDto> ConfidenceDistribution { get; set; } = new();

    /// <summary>Common session/draft errors: message (or truncated) and count.</summary>
    public List<ErrorCountDto> CommonErrors { get; set; } = new();

    /// <summary>Orders created from parser per day in period (date string YYYY-MM-DD, count).</summary>
    public List<OrdersPerDayDto> OrdersCreatedPerDay { get; set; } = new();

    /// <summary>Period start (UTC).</summary>
    public DateTime FromDate { get; set; }

    /// <summary>Period end (UTC).</summary>
    public DateTime ToDate { get; set; }
}

/// <summary>Confidence score bucket for analytics.</summary>
public class ConfidenceBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Error message and occurrence count for analytics.</summary>
public class ErrorCountDto
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Orders created from parser on a given day.</summary>
public class OrdersPerDayDto
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
    public int Count { get; set; }
}

/// <summary>
/// DTO for testing parser templates with sample data
/// </summary>
public class ParserTemplateTestDataDto
{
    /// <summary>
    /// Sample FROM email address
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Sample email subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Sample email body text
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Whether the sample email has attachments
    /// </summary>
    public bool HasAttachments { get; set; }

    /// <summary>
    /// Attachment file names (for testing attachment-based templates)
    /// </summary>
    public List<string> AttachmentFileNames { get; set; } = new();
}

/// <summary>
/// Result of testing a parser template
/// </summary>
public class ParserTemplateTestResultDto
{
    /// <summary>
    /// Whether the template matched the test data
    /// </summary>
    public bool Matched { get; set; }

    /// <summary>
    /// Template that matched (if any)
    /// </summary>
    public ParserTemplateDto? MatchedTemplate { get; set; }

    /// <summary>
    /// Matching details (which patterns matched)
    /// </summary>
    public TemplateMatchDetailsDto? MatchDetails { get; set; }

    /// <summary>
    /// Error message if matching failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Sample extracted data (if template matched and parsing was attempted)
    /// </summary>
    public Dictionary<string, object>? ExtractedData { get; set; }
}

/// <summary>
/// Result of checking if an order already exists for a given Service ID (duplicate warning).
/// </summary>
public class OrderExistsByServiceIdDto
{
    /// <summary>True if an order with this Service ID already exists.</summary>
    public bool Exists { get; set; }

    /// <summary>Existing order ID, if found.</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Service ID that was checked (normalized).</summary>
    public string? ServiceId { get; set; }

    /// <summary>Ticket ID of the existing order, if any.</summary>
    public string? TicketId { get; set; }
}

/// <summary>
/// Details about how a template matched
/// </summary>
public class TemplateMatchDetailsDto
{
    /// <summary>
    /// Whether FROM address pattern matched
    /// </summary>
    public bool FromAddressMatched { get; set; }

    /// <summary>
    /// Whether subject pattern matched
    /// </summary>
    public bool SubjectMatched { get; set; }

    /// <summary>
    /// FROM address pattern that was checked
    /// </summary>
    public string? FromAddressPattern { get; set; }

    /// <summary>
    /// Subject pattern that was checked
    /// </summary>
    public string? SubjectPattern { get; set; }

    /// <summary>
    /// Template priority
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Request to create a parsed material alias (manual resolve: parsed name → Material).
/// </summary>
public class CreateParsedMaterialAliasRequest
{
    /// <summary>Parsed name as seen in parser (e.g. "ONU Adaptor", "Legacy ONU Plug").</summary>
    public string AliasText { get; set; } = string.Empty;
    /// <summary>Material master ID to resolve to.</summary>
    public Guid MaterialId { get; set; }
}

/// <summary>
/// Parsed material alias DTO for list/display.
/// </summary>
public class ParsedMaterialAliasDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string AliasText { get; set; } = string.Empty;
    public string NormalizedAliasText { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string? MaterialItemCode { get; set; }
    public string? MaterialDescription { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? Source { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Aggregated unmatched parsed material name for review queue (visibility aid).
/// </summary>
public class UnmatchedMaterialReviewItemDto
{
    public string NormalizedName { get; set; } = string.Empty;
    public string ExampleOriginal { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

