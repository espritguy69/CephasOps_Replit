using System.Text.Json;
using CephasOps.Application.Buildings.DTOs;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Files.DTOs;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Events;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Parser;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MsgReader.Outlook;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Parser service implementation
/// </summary>
public class ParserService : IParserService
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderService _orderService;
    private readonly ITimeExcelParserService _timeExcelParser;
    private readonly IExcelToPdfService _excelToPdfService;
    private readonly IPdfTextExtractionService _pdfTextExtractionService;
    private readonly IPdfOrderParserService _pdfOrderParserService;
    private readonly IFileService _fileService;
    private readonly IBuildingMatchingService _buildingMatchingService;
    private readonly IBuildingService _buildingService;
    private readonly IEncryptionService _encryptionService;
    private readonly IParsedOrderDraftEnrichmentService _enrichmentService;
    /// <summary>Lazy to break circular dependency with EmailIngestionService.</summary>
    private readonly Lazy<IEmailIngestionService>? _emailIngestionServiceLazy;
    private readonly IEventBus? _eventBus;
    private readonly ILogger<ParserService> _logger;


    public ParserService(
        ApplicationDbContext context,
        IOrderService orderService,
        ITimeExcelParserService timeExcelParser,
        IExcelToPdfService excelToPdfService,
        IPdfTextExtractionService pdfTextExtractionService,
        IPdfOrderParserService pdfOrderParserService,
        IFileService fileService,
        IBuildingMatchingService buildingMatchingService,
        IBuildingService buildingService,
        IEncryptionService encryptionService,
        IParsedOrderDraftEnrichmentService enrichmentService,
        ILogger<ParserService> logger,
        Lazy<IEmailIngestionService>? emailIngestionServiceLazy = null,
        IEventBus? eventBus = null)
    {
        _context = context;
        _orderService = orderService;
        _timeExcelParser = timeExcelParser;
        _excelToPdfService = excelToPdfService;
        _pdfTextExtractionService = pdfTextExtractionService;
        _pdfOrderParserService = pdfOrderParserService;
        _fileService = fileService;
        _buildingMatchingService = buildingMatchingService;
        _buildingService = buildingService;
        _encryptionService = encryptionService;
        _enrichmentService = enrichmentService;
        _logger = logger;
        _emailIngestionServiceLazy = emailIngestionServiceLazy;
        _eventBus = eventBus;
    }

    public async Task<List<ParseSessionDto>> GetParseSessionsAsync(Guid companyId, string? status = null, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var query = _context.ParseSessions.Where(s => s.CompanyId == companyId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var sessions = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return sessions.Select(s => new ParseSessionDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            EmailMessageId = s.EmailMessageId,
            ParserTemplateId = s.ParserTemplateId,
            Status = s.Status ?? string.Empty,
            ErrorMessage = s.ErrorMessage,
            SnapshotFileId = s.SnapshotFileId,
            ParsedOrdersCount = s.ParsedOrdersCount,
            CreatedAt = s.CreatedAt,
            CompletedAt = s.CompletedAt,
            SourceType = s.SourceType,
            SourceDescription = s.SourceDescription
        }).ToList();
    }

    public async Task<ParseSessionDto?> GetParseSessionByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var session = await _context.ParseSessions
            .Where(s => s.Id == id && s.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (session == null) return null;

        return new ParseSessionDto
        {
            Id = session.Id,
            CompanyId = session.CompanyId,
            EmailMessageId = session.EmailMessageId,
            ParserTemplateId = session.ParserTemplateId,
            Status = session.Status ?? string.Empty,
            ErrorMessage = session.ErrorMessage,
            SnapshotFileId = session.SnapshotFileId,
            ParsedOrdersCount = session.ParsedOrdersCount,
            CreatedAt = session.CreatedAt,
            CompletedAt = session.CompletedAt,
            SourceType = session.SourceType,
            SourceDescription = session.SourceDescription
        };
    }

    public async Task<List<ParsedOrderDraftDto>> GetParsedOrderDraftsAsync(Guid parseSessionId, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        _logger.LogInformation("GetParsedOrderDraftsAsync called: SessionId={SessionId}, CompanyId={CompanyId}", parseSessionId, companyId);
        
        // Check session exists and belongs to company (tenant-safe: avoid FindAsync bypass)
        var session = await _context.ParseSessions
            .FirstOrDefaultAsync(s => s.Id == parseSessionId && s.CompanyId == companyId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Parse session not found: {SessionId}", parseSessionId);
            return new List<ParsedOrderDraftDto>();
        }
        
        _logger.LogInformation("Session found: SessionId={SessionId}, Status={Status}, CompanyId={SessionCompanyId}, ParsedOrdersCount={Count}", 
            parseSessionId, session.Status, session.CompanyId, session.ParsedOrdersCount);
        
        // Multi-tenant SaaS — CompanyId filter required. Drafts must belong to the same company as the session.
        var drafts = await _context.ParsedOrderDrafts
            .Where(d => d.ParseSessionId == parseSessionId && d.CompanyId == companyId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Returning {Count} drafts for session {SessionId}", drafts.Count, parseSessionId);

        var dtoList = drafts.Select(d => new ParsedOrderDraftDto
        {
            Id = d.Id,
            CompanyId = d.CompanyId,
            ParseSessionId = d.ParseSessionId,
            PartnerId = d.PartnerId,
            BuildingId = d.BuildingId,
            BuildingName = d.BuildingName,
            BuildingStatus = d.BuildingStatus,
            ServiceId = d.ServiceId,
            TicketId = d.TicketId,
            AwoNumber = d.AwoNumber,
            CustomerName = d.CustomerName,
            CustomerPhone = d.CustomerPhone,
            CustomerEmail = d.CustomerEmail,
            AdditionalContactNumber = d.AdditionalContactNumber,
            Issue = d.Issue,
            AddressText = d.AddressText,
            OldAddress = d.OldAddress,
            AppointmentDate = d.AppointmentDate,
            AppointmentWindow = d.AppointmentWindow,
            OrderTypeHint = d.OrderTypeHint,
            OrderTypeCode = d.OrderTypeCode,
            OrderCategoryId = d.OrderCategoryId,
            PackageName = d.PackageName,
            Bandwidth = d.Bandwidth,
            OnuSerialNumber = d.OnuSerialNumber,
            OnuPassword = d.OnuPassword,
            Username = d.Username,
            Password = d.Password,
            InternetWanIp = d.InternetWanIp,
            InternetLanIp = d.InternetLanIp,
            InternetGateway = d.InternetGateway,
            InternetSubnetMask = d.InternetSubnetMask,
            VoipServiceId = d.VoipServiceId,
            Remarks = d.Remarks,
            AdditionalInformation = d.AdditionalInformation,
            SourceFileName = d.SourceFileName,
            ConfidenceScore = d.ConfidenceScore,
            ValidationStatus = d.ValidationStatus ?? "Pending",
            ValidationNotes = d.ValidationNotes,
            CreatedOrderId = d.CreatedOrderId,
            CreatedByUserId = d.CreatedByUserId,
            CreatedAt = d.CreatedAt,
            Materials = ParsedMaterialsSerializer.Deserialize(d.ParsedMaterialsJson),
            UnmatchedMaterialCount = d.UnmatchedMaterialCount ?? 0,
            UnmatchedMaterialNames = DeserializeUnmatchedMaterialNames(d.UnmatchedMaterialNamesJson)
        }).ToList();

        var map = await GetExistingOrderIdsMapAsync(companyId, cancellationToken);
        foreach (var dto in dtoList)
        {
            if (!string.IsNullOrWhiteSpace(dto.ServiceId) && !dto.CreatedOrderId.HasValue && map.TryGetValue(NormalizeServiceId(dto.ServiceId), out var orderId))
                dto.ExistingOrderId = orderId;
        }
        return dtoList;
    }

    public async Task<ParsedOrderDraftDto?> GetParsedOrderDraftByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var draft = await _context.ParsedOrderDrafts
            .Where(d => d.Id == id && d.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (draft == null) return null;

        var dto = new ParsedOrderDraftDto
        {
            Id = draft.Id,
            CompanyId = draft.CompanyId,
            ParseSessionId = draft.ParseSessionId,
            PartnerId = draft.PartnerId,
            BuildingId = draft.BuildingId,
            BuildingName = draft.BuildingName,
            BuildingStatus = draft.BuildingStatus,
            ServiceId = draft.ServiceId,
            TicketId = draft.TicketId,
            AwoNumber = draft.AwoNumber, // ✅ Map AWO Number for Assurance orders
            CustomerName = draft.CustomerName,
            CustomerPhone = draft.CustomerPhone,
            CustomerEmail = draft.CustomerEmail,
            AdditionalContactNumber = draft.AdditionalContactNumber, // ✅ Map Additional Contact Number
            Issue = draft.Issue, // ✅ Map Issue for Assurance orders
            AddressText = draft.AddressText,
            OldAddress = draft.OldAddress,
            AppointmentDate = draft.AppointmentDate,
            AppointmentWindow = draft.AppointmentWindow,
            OrderTypeHint = draft.OrderTypeHint,
            OrderTypeCode = draft.OrderTypeCode,
            OrderCategoryId = draft.OrderCategoryId,
            PackageName = draft.PackageName,
            Bandwidth = draft.Bandwidth,
            OnuSerialNumber = draft.OnuSerialNumber,
            OnuPassword = draft.OnuPassword,
            Username = draft.Username,
            Password = draft.Password,
            InternetWanIp = draft.InternetWanIp,
            InternetLanIp = draft.InternetLanIp,
            InternetGateway = draft.InternetGateway,
            InternetSubnetMask = draft.InternetSubnetMask,
            VoipServiceId = draft.VoipServiceId,
            Remarks = draft.Remarks,
            AdditionalInformation = draft.AdditionalInformation,
            SourceFileName = draft.SourceFileName,
            ConfidenceScore = draft.ConfidenceScore,
            ValidationStatus = draft.ValidationStatus ?? "Pending",
            ValidationNotes = draft.ValidationNotes,
            CreatedOrderId = draft.CreatedOrderId,
            CreatedByUserId = draft.CreatedByUserId,
            CreatedAt = draft.CreatedAt,
            Materials = ParsedMaterialsSerializer.Deserialize(draft.ParsedMaterialsJson)
        };

        // Partner display code for parser review (read-only)
        if (draft.PartnerId.HasValue)
        {
            var partnerCode = await _context.Partners
                .Where(p => p.Id == draft.PartnerId.Value)
                .Select(p => p.Code)
                .FirstOrDefaultAsync(cancellationToken);
            if (partnerCode != null)
                dto.PartnerCode = partnerCode;
        }

        // When draft already has BuildingId, ensure BuildingName matches the persisted building (display consistency).
        if (draft.BuildingId.HasValue)
        {
            var buildingName = await _context.Buildings
                .Where(b => b.Id == draft.BuildingId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (buildingName != null)
                dto.BuildingName = buildingName;
        }

        // Building resolution for parser edit: only when we have building name but no ID (never overwrite existing BuildingId).
        // If single high-confidence match, bind BuildingId/BuildingName on DTO for display; else suggest list.
        if (!string.IsNullOrWhiteSpace(draft.BuildingName) && !draft.BuildingId.HasValue)
        {
            var addressComponents = AddressParser.ParseAddress(draft.AddressText);
            var candidates = await _buildingMatchingService.FindFuzzyBuildingCandidatesAsync(
                draft.BuildingName,
                addressComponents?.City,
                addressComponents?.Postcode,
                draft.CompanyId ?? companyId,
                minScore: 0.85,
                maxResults: 5,
                cancellationToken);
            if (candidates.Count == 1 && candidates[0].SimilarityScore >= 0.9)
            {
                dto.BuildingId = candidates[0].Building.Id;
                dto.BuildingName = candidates[0].Building.Name;
            }
            else if (candidates.Count > 0)
            {
                dto.SuggestedBuildings = candidates;
            }
        }
        else if (draft.BuildingStatus == "New" && !string.IsNullOrWhiteSpace(draft.BuildingName))
        {
            var addressComponents = AddressParser.ParseAddress(draft.AddressText);
            dto.SuggestedBuildings = await _buildingMatchingService.FindFuzzyBuildingCandidatesAsync(
                draft.BuildingName,
                addressComponents?.City,
                addressComponents?.Postcode,
                draft.CompanyId ?? companyId,
                minScore: 0.85,
                maxResults: 3,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(draft.ServiceId) && !draft.CreatedOrderId.HasValue)
        {
            var map = await GetExistingOrderIdsMapAsync(draft.CompanyId ?? companyId, cancellationToken);
            if (map.TryGetValue(NormalizeServiceId(draft.ServiceId), out var orderId))
                dto.ExistingOrderId = orderId;
        }

        // Unmatched materials: use persisted audit if set (after create-from-draft), else compute for parser review
        if (draft.UnmatchedMaterialCount.HasValue)
        {
            dto.UnmatchedMaterialCount = draft.UnmatchedMaterialCount.Value;
            dto.UnmatchedMaterialNames = !string.IsNullOrEmpty(draft.UnmatchedMaterialNamesJson)
                ? DeserializeUnmatchedMaterialNames(draft.UnmatchedMaterialNamesJson)
                : new List<string>();
        }
        else if (dto.Materials != null && dto.Materials.Count > 0 && (draft.CompanyId ?? companyId) != Guid.Empty)
        {
            var unmatched = await _orderService.GetUnmatchedParsedMaterialNamesAsync(draft.CompanyId ?? companyId, dto.Materials, cancellationToken);
            dto.UnmatchedMaterialCount = unmatched.Count;
            dto.UnmatchedMaterialNames = unmatched;
        }

        return dto;
    }

    public async Task<ParsedOrderDraftDto> UpdateParsedOrderDraftAsync(Guid id, UpdateParsedOrderDraftDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Get existing draft
        var draft = await _context.ParsedOrderDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId && !d.IsDeleted, cancellationToken);

        if (draft == null)
        {
            throw new KeyNotFoundException($"Parsed order draft with ID {id} not found");
        }

        // Don't allow editing after approval
        if (draft.CreatedOrderId.HasValue)
        {
            throw new InvalidOperationException("Cannot update draft that has already been approved");
        }

        // Update fields
        if (dto.ServiceId != null) draft.ServiceId = dto.ServiceId;
        if (dto.TicketId != null) draft.TicketId = dto.TicketId;
        if (dto.CustomerName != null) draft.CustomerName = dto.CustomerName;
        if (dto.CustomerPhone != null) draft.CustomerPhone = dto.CustomerPhone;
        if (dto.CustomerEmail != null) draft.CustomerEmail = dto.CustomerEmail;
        if (dto.AddressText != null) draft.AddressText = dto.AddressText;
        if (dto.OldAddress != null) draft.OldAddress = dto.OldAddress;
        if (dto.AppointmentDate.HasValue) draft.AppointmentDate = dto.AppointmentDate.Value;
        if (dto.AppointmentWindow != null) draft.AppointmentWindow = dto.AppointmentWindow;
        if (dto.OrderTypeCode != null) draft.OrderTypeCode = dto.OrderTypeCode;
        draft.OrderCategoryId = dto.OrderCategoryId;
        if (dto.PackageName != null) draft.PackageName = dto.PackageName;
        if (dto.Bandwidth != null) draft.Bandwidth = dto.Bandwidth;
        if (dto.OnuSerialNumber != null) draft.OnuSerialNumber = dto.OnuSerialNumber;
        if (dto.OnuPassword != null) draft.OnuPassword = dto.OnuPassword;
        if (dto.Username != null) draft.Username = dto.Username;
        if (dto.Password != null) draft.Password = dto.Password;
        if (dto.InternetWanIp != null) draft.InternetWanIp = dto.InternetWanIp;
        if (dto.InternetLanIp != null) draft.InternetLanIp = dto.InternetLanIp;
        if (dto.InternetGateway != null) draft.InternetGateway = dto.InternetGateway;
        if (dto.InternetSubnetMask != null) draft.InternetSubnetMask = dto.InternetSubnetMask;
        if (dto.VoipServiceId != null) draft.VoipServiceId = dto.VoipServiceId;
        if (dto.Remarks != null) draft.Remarks = dto.Remarks;
        if (dto.BuildingId.HasValue) draft.BuildingId = dto.BuildingId.Value;

        draft.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated parsed order draft: {DraftId} by user: {UserId}", id, userId);

        // Return updated draft
        return await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to retrieve updated draft");
    }

    public async Task<ParsedOrderDraftDto> ApproveParsedOrderAsync(Guid id, ApproveParsedOrderDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Parsed order draft with ID {id} not found");
        }

        // Check if already approved (idempotency)
        if (existing.CreatedOrderId.HasValue)
        {
            _logger.LogInformation("Parsed order draft already approved: {DraftId}, OrderId: {OrderId}", id, existing.CreatedOrderId);
            return existing;
        }

        var now = DateTime.UtcNow;

        // Get the parse session to retrieve SourceEmailId and PDF snapshot
        var session = await GetParseSessionByIdAsync(existing.ParseSessionId, companyId, cancellationToken);

        // Check if order already exists (by ServiceId) - Database-First Strategy for Modification Outdoor, Assurance, Modification Indoor, Value Added Service
        Guid? existingOrderId = null;
        Domain.Orders.Entities.Order? existingOrderEntity = null;
        bool isModificationOutdoor = existing.OrderTypeCode == "MODIFICATION_OUTDOOR" || 
                                     (existing.OrderTypeHint?.Contains("Modification Outdoor", StringComparison.OrdinalIgnoreCase) == true);
        bool usesDatabaseFirstStrategy = ShouldUseDatabaseFirstStrategy(existing.OrderTypeCode, existing.OrderTypeHint);
        
        if (!string.IsNullOrWhiteSpace(existing.ServiceId))
        {
            // ✅ Normalize ServiceId: Trim whitespace and convert to uppercase for consistent matching
            // This prevents duplicates from case differences or extra spaces
            var normalizedServiceId = existing.ServiceId.Trim().ToUpperInvariant();
            
            _logger.LogInformation(
                "Checking for duplicate order by ServiceId. Original: '{Original}', Normalized: '{Normalized}'",
                existing.ServiceId, normalizedServiceId);
            
            // ✅ Database-First Strategy: Match using normalized ServiceId
            // Note: We fetch all matching ServiceIds and normalize in memory for PostgreSQL compatibility
            // For modification outdoor: Use activation order (first order by CreatedAt ASC)
            // For other orders: Use most recent order (OrderByDescending)
            var candidateOrders = await _context.Orders
                .Where(o => o.CompanyId == companyId && !string.IsNullOrWhiteSpace(o.ServiceId))
                .ToListAsync(cancellationToken);
            
            var matchingOrder = candidateOrders
                .Where(o => o.ServiceId.Trim().ToUpperInvariant() == normalizedServiceId)
                .OrderBy(o => isModificationOutdoor ? o.CreatedAt : DateTime.MaxValue)
                .ThenByDescending(o => isModificationOutdoor ? DateTime.MinValue : o.CreatedAt)
                .FirstOrDefault();
            
            if (matchingOrder != null)
            {
                existingOrderEntity = matchingOrder;
            }
            
            // Fallback: If no match found with normalization, try exact match (for backward compatibility)
            if (existingOrderEntity == null)
            {
                var query = _context.Orders
                    .Where(o => o.ServiceId == existing.ServiceId && o.CompanyId == companyId);
                
                if (isModificationOutdoor)
                {
                    // Rule: Use activation order details (first order created for that Service ID)
                    existingOrderEntity = await query
                        .OrderBy(o => o.CreatedAt) // ASC - get activation order
                        .FirstOrDefaultAsync(cancellationToken);
                }
                else
                {
                    // For other order types, use most recent
                    existingOrderEntity = await query
                        .OrderByDescending(o => o.CreatedAt) // DESC - get most recent
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }
            
            if (existingOrderEntity != null)
            {
                existingOrderId = existingOrderEntity.Id;
                _logger.LogInformation(
                    "✅ DUPLICATE DETECTED: Found existing order by ServiceId. " +
                    "Original ServiceId: '{Original}', Normalized: '{Normalized}', " +
                    "Existing OrderId: {OrderId}, Status: {Status}, CreatedAt: {CreatedAt}, " +
                    "IsModificationOutdoor: {IsMod}, Strategy: {Strategy}", 
                    existing.ServiceId, normalizedServiceId, existingOrderId, 
                    existingOrderEntity.Status, existingOrderEntity.CreatedAt,
                    isModificationOutdoor, 
                    isModificationOutdoor ? "Activation Order (First)" : "Most Recent");
            }
            else
            {
                _logger.LogInformation(
                    "No existing order found for ServiceId: '{Original}' (normalized: '{Normalized}'). Will create new order.",
                    existing.ServiceId, normalizedServiceId);
            }
        }

        CreateOrderFromDraftResult result;
        Guid orderId;

        if (existingOrderId.HasValue && existingOrderEntity != null)
        {
            // ✅ Database-First Strategy: Merge existing order data with new Excel fields
            _logger.LogInformation("Updating existing order {OrderId} from parsed draft {DraftId}, IsModificationOutdoor: {IsMod}", 
                existingOrderId.Value, id, isModificationOutdoor);
            
            // Parse appointment window for update
            TimeSpan? windowFrom = null, windowTo = null;
            if (!string.IsNullOrWhiteSpace(existing.AppointmentWindow))
            {
                // Simple parsing: "09:00-17:00" format
                var parts = existing.AppointmentWindow.Split('-');
                if (parts.Length == 2)
                {
                    if (TimeSpan.TryParse(parts[0].Trim(), out var from))
                        windowFrom = from;
                    if (TimeSpan.TryParse(parts[1].Trim(), out var to))
                        windowTo = to;
                }
            }

            // ✅ Database-First Merging Logic
            string? finalCustomerName;
            string? finalCustomerPhone;
            string? finalCustomerEmail;
            string? finalOldAddress;
            string? finalServiceAddress; // New Address for modification outdoor
            string? finalOnuPassword; // New ONU Password for modification outdoor
            string? finalRemarks; // Special remarks from Excel
            Guid? finalBuildingId = null;

            if (isModificationOutdoor)
            {
                // ✅ Modification Outdoor: Database-first strategy
                // From Database (existing order):
                finalCustomerName = existingOrderEntity.CustomerName; // Use DB value
                finalCustomerPhone = existingOrderEntity.CustomerPhone; // Use DB value
                finalCustomerEmail = existingOrderEntity.CustomerEmail; // Use DB value
                finalBuildingId = existingOrderEntity.BuildingId != Guid.Empty ? existingOrderEntity.BuildingId : null;
                
                // Old Address: Database first, Excel can override if different
                finalOldAddress = existingOrderEntity.AddressLine1 + 
                    (!string.IsNullOrEmpty(existingOrderEntity.AddressLine2) ? ", " + existingOrderEntity.AddressLine2 : "") +
                    (!string.IsNullOrEmpty(existingOrderEntity.City) ? ", " + existingOrderEntity.City : "") +
                    (!string.IsNullOrEmpty(existingOrderEntity.State) ? ", " + existingOrderEntity.State : "") +
                    (!string.IsNullOrEmpty(existingOrderEntity.Postcode) ? " " + existingOrderEntity.Postcode : "");
                
                // Check if Excel Old Address exists and differs - allow override
                if (!string.IsNullOrWhiteSpace(existing.OldAddress) && 
                    !string.IsNullOrWhiteSpace(finalOldAddress) &&
                    !existing.OldAddress.Equals(finalOldAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("⚠️ Old Address differs: DB='{DbAddr}' vs Excel='{ExcelAddr}'. Using Excel value (override allowed).", 
                        finalOldAddress, existing.OldAddress);
                    finalOldAddress = existing.OldAddress; // Excel override
                }
                else if (string.IsNullOrWhiteSpace(finalOldAddress))
                {
                    finalOldAddress = existing.OldAddress; // Use Excel if DB is empty
                }
                
                // From Excel (always extracted - new/changed fields):
                finalServiceAddress = existing.AddressText; // New Address (where they're moving to)
                finalOnuPassword = existing.OnuPassword; // New ONU Password (provided by TIME)
                finalRemarks = existing.Remarks; // Special instructions from TIME
                
                _logger.LogInformation("✅ Modification Outdoor merge: Customer from DB, New Address/ONU Password/Remarks from Excel");
            }
            else if (usesDatabaseFirstStrategy)
            {
                // ✅ Assurance, Modification Indoor, Value Added Service: Database-first strategy
                // Pull customer information from existing order by ServiceId
                finalCustomerName = existingOrderEntity.CustomerName; // Use DB value
                finalCustomerPhone = existingOrderEntity.CustomerPhone; // Use DB value
                finalCustomerEmail = existingOrderEntity.CustomerEmail; // Use DB value
                finalBuildingId = existingOrderEntity.BuildingId != Guid.Empty ? existingOrderEntity.BuildingId : null;
                
                // Address: Use DB address, but allow parsed data to override if provided
                if (!string.IsNullOrWhiteSpace(existing.AddressText))
                {
                    // Parsed address provided - use it (may be more current)
                    finalServiceAddress = existing.AddressText;
                    _logger.LogInformation("Using parsed address for {OrderType}: {Address}", existing.OrderTypeCode, finalServiceAddress);
                }
                else
                {
                    // No parsed address - use DB address
                    finalServiceAddress = existingOrderEntity.AddressLine1 + 
                        (!string.IsNullOrEmpty(existingOrderEntity.AddressLine2) ? ", " + existingOrderEntity.AddressLine2 : "") +
                        (!string.IsNullOrEmpty(existingOrderEntity.City) ? ", " + existingOrderEntity.City : "") +
                        (!string.IsNullOrEmpty(existingOrderEntity.State) ? ", " + existingOrderEntity.State : "") +
                        (!string.IsNullOrEmpty(existingOrderEntity.Postcode) ? " " + existingOrderEntity.Postcode : "");
                    _logger.LogInformation("Using DB address for {OrderType}: {Address}", existing.OrderTypeCode, finalServiceAddress);
                }
                
                finalOldAddress = existing.OldAddress; // Usually not applicable for these order types
                finalOnuPassword = existing.OnuPassword; // Preserve if provided
                finalRemarks = existing.Remarks; // Use parsed remarks (includes issue description, URLs, etc.)
                
                _logger.LogInformation("✅ {OrderType} merge: Customer info from DB (ServiceId: {ServiceId}), Remarks/Issue from parsed data", 
                    existing.OrderTypeCode, existing.ServiceId);
            }
            else
            {
                // For other order types (e.g., Activation), use parsed data (existing behavior)
                finalCustomerName = existing.CustomerName ?? existingOrderEntity.CustomerName;
                finalCustomerPhone = existing.CustomerPhone ?? existingOrderEntity.CustomerPhone;
                finalCustomerEmail = existing.CustomerEmail ?? existingOrderEntity.CustomerEmail;
                finalOldAddress = existing.OldAddress;
                finalServiceAddress = existing.AddressText ?? existingOrderEntity.AddressLine1;
                finalOnuPassword = existing.OnuPassword;
                finalRemarks = existing.Remarks;
            }

            // ✅ Update order entity directly with merged data (Database-First Strategy)
            existingOrderEntity.CustomerName = finalCustomerName ?? existingOrderEntity.CustomerName;
            existingOrderEntity.CustomerPhone = finalCustomerPhone ?? existingOrderEntity.CustomerPhone;
            existingOrderEntity.CustomerEmail = finalCustomerEmail ?? existingOrderEntity.CustomerEmail;
            existingOrderEntity.OldAddress = finalOldAddress;
            
            // ✅ Update BuildingId if database-first strategy is used
            if (usesDatabaseFirstStrategy && finalBuildingId.HasValue && finalBuildingId.Value != Guid.Empty)
            {
                existingOrderEntity.BuildingId = finalBuildingId.Value;
                _logger.LogInformation("Updated BuildingId from existing order: {BuildingId}", finalBuildingId.Value);
            }
            
            // Update New Address (Service Address) if provided
            if (!string.IsNullOrWhiteSpace(finalServiceAddress))
            {
                existingOrderEntity.AddressLine1 = finalServiceAddress;
            }
            
            // Update appointment if provided
            if (existing.AppointmentDate.HasValue)
            {
                existingOrderEntity.AppointmentDate = existing.AppointmentDate.Value;
            }
            if (windowFrom.HasValue)
            {
                existingOrderEntity.AppointmentWindowFrom = windowFrom.Value;
            }
            if (windowTo.HasValue)
            {
                existingOrderEntity.AppointmentWindowTo = windowTo.Value;
            }
            
            // ✅ Update New ONU Password if provided (encrypt before storing)
            if (!string.IsNullOrWhiteSpace(finalOnuPassword))
            {
                // Encrypt ONU password using encryption service (same as OrderService does)
                existingOrderEntity.OnuPasswordEncrypted = _encryptionService.Encrypt(finalOnuPassword);
                _logger.LogInformation("✅ Updated ONU Password (encrypted) for order {OrderId}", existingOrderId.Value);
            }
            
            // ✅ Store Remarks in PartnerNotes (special instructions from TIME)
            if (!string.IsNullOrWhiteSpace(finalRemarks))
            {
                // Append to existing PartnerNotes if any, otherwise set new
                existingOrderEntity.PartnerNotes = string.IsNullOrWhiteSpace(existingOrderEntity.PartnerNotes)
                    ? finalRemarks
                    : $"{existingOrderEntity.PartnerNotes}\n\n[Modification Outdoor Remarks] {finalRemarks}";
            }
            
            // Update internal notes with merge information
            var strategyNote = usesDatabaseFirstStrategy 
                ? $" ({existing.OrderTypeCode} - Database-First Merge: Customer info from ServiceId {existing.ServiceId})" 
                : "";
            var mergeNote = $"Updated from parsed draft{strategyNote}";
            existingOrderEntity.OrderNotesInternal = string.IsNullOrWhiteSpace(existingOrderEntity.OrderNotesInternal)
                ? $"{mergeNote}. {existing.ValidationNotes}"
                : $"{existingOrderEntity.OrderNotesInternal}\n\n{mergeNote}. {existing.ValidationNotes}";
            
            existingOrderEntity.UpdatedAt = now;
            
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("✅ Updated existing order {OrderId} with merged data (Database-First Strategy: {UsesDbFirst}). " +
                "OrderType: {OrderType}, Customer info from DB (ServiceId: {ServiceId})", 
                existingOrderId.Value, usesDatabaseFirstStrategy, existing.OrderTypeCode, existing.ServiceId);
            
            orderId = existingOrderId.Value;
            result = CreateOrderFromDraftResult.Succeeded(orderId);
        }
        else
        {
            // Create new order from draft
            // ✅ For database-first order types (Assurance, Modification, VAS), enrich customer info from ServiceId if available
            string? enrichedCustomerName = existing.CustomerName;
            string? enrichedCustomerPhone = existing.CustomerPhone;
            string? enrichedCustomerEmail = existing.CustomerEmail;
            string? enrichedAddressText = existing.AddressText;
            Guid? enrichedBuildingId = existing.BuildingId;
            
            if (usesDatabaseFirstStrategy && !string.IsNullOrWhiteSpace(existing.ServiceId))
            {
                // Look up most recent order with this ServiceId to get customer info
                var normalizedServiceId = existing.ServiceId.Trim().ToUpperInvariant();
                var candidateOrders = await _context.Orders
                    .Where(o => o.CompanyId == companyId && !string.IsNullOrWhiteSpace(o.ServiceId))
                    .ToListAsync(cancellationToken);
                
                var matchingOrder = candidateOrders
                    .Where(o => o.ServiceId.Trim().ToUpperInvariant() == normalizedServiceId)
                    .OrderByDescending(o => o.CreatedAt) // Get most recent
                    .FirstOrDefault();
                
                if (matchingOrder != null)
                {
                    // Enrich customer info from existing order
                    enrichedCustomerName = string.IsNullOrWhiteSpace(existing.CustomerName) ? matchingOrder.CustomerName : existing.CustomerName;
                    enrichedCustomerPhone = string.IsNullOrWhiteSpace(existing.CustomerPhone) ? matchingOrder.CustomerPhone : existing.CustomerPhone;
                    enrichedCustomerEmail = string.IsNullOrWhiteSpace(existing.CustomerEmail) ? matchingOrder.CustomerEmail : existing.CustomerEmail;
                    
                    // Enrich address if not provided in parsed data
                    if (string.IsNullOrWhiteSpace(existing.AddressText))
                    {
                        enrichedAddressText = matchingOrder.AddressLine1 + 
                            (!string.IsNullOrEmpty(matchingOrder.AddressLine2) ? ", " + matchingOrder.AddressLine2 : "") +
                            (!string.IsNullOrEmpty(matchingOrder.City) ? ", " + matchingOrder.City : "") +
                            (!string.IsNullOrEmpty(matchingOrder.State) ? ", " + matchingOrder.State : "") +
                            (!string.IsNullOrEmpty(matchingOrder.Postcode) ? " " + matchingOrder.Postcode : "");
                    }
                    
                    // Enrich building ID if not provided
                    if (!existing.BuildingId.HasValue && matchingOrder.BuildingId != Guid.Empty)
                    {
                        enrichedBuildingId = matchingOrder.BuildingId;
                    }
                    
                    _logger.LogInformation("✅ Enriched customer info from existing order (ServiceId: {ServiceId}) for new {OrderType} order. " +
                        "Customer: {CustomerName}, Building: {BuildingId}", 
                        existing.ServiceId, existing.OrderTypeCode, enrichedCustomerName, enrichedBuildingId);
                }
                else
                {
                    _logger.LogInformation("⚠️ No existing order found for ServiceId {ServiceId} to enrich customer info for new {OrderType} order", 
                        existing.ServiceId, existing.OrderTypeCode);
                }
            }
            
            var createDto = new CreateOrderFromDraftDto
            {
                ParsedOrderDraftId = id,
                CompanyId = existing.CompanyId,
                PartnerId = existing.PartnerId,
                BuildingId = enrichedBuildingId,
                SourceEmailId = session?.EmailMessageId,
                OrderTypeHint = existing.OrderTypeHint,
                OrderCategoryId = existing.OrderCategoryId,
                ServiceId = existing.ServiceId,
                TicketId = existing.TicketId,
                CustomerName = enrichedCustomerName,
                CustomerPhone = enrichedCustomerPhone,
                CustomerEmail = enrichedCustomerEmail,
                AdditionalContactNumber = existing.AdditionalContactNumber, // ✅ Map Additional Contact Number
                Issue = existing.Issue, // ✅ Map Issue for Assurance orders
                AddressText = enrichedAddressText,
                OldAddress = existing.OldAddress,
                AppointmentDate = existing.AppointmentDate,
                AppointmentWindow = existing.AppointmentWindow,
                PackageName = existing.PackageName,
                Bandwidth = existing.Bandwidth,
                OnuSerialNumber = existing.OnuSerialNumber,
                OnuPassword = existing.OnuPassword, // Will be encrypted in OrderService
                VoipServiceId = existing.VoipServiceId,
                Remarks = existing.Remarks,
                ValidationNotes = dto.ValidationNotes ?? existing.ValidationNotes,
                Materials = existing.Materials != null && existing.Materials.Count > 0 ? existing.Materials.ToList() : null
            };

            result = await _orderService.CreateFromParsedDraftAsync(createDto, userId, cancellationToken);
            orderId = result.OrderId ?? Guid.Empty;
        }

        if (!result.Success)
        {
            // Update draft with validation errors
            var errorNotes = string.Join("; ", result.ValidationErrors);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                errorNotes = string.IsNullOrEmpty(errorNotes) 
                    ? result.ErrorMessage 
                    : $"{result.ErrorMessage}: {errorNotes}";
            }

            // Update draft with validation errors - use entity update instead of raw SQL
            var failedDraft = await _context.ParsedOrderDrafts
                .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);
            
            if (failedDraft != null)
            {
                failedDraft.ValidationStatus = "Invalid";
                failedDraft.ValidationNotes = errorNotes;
                failedDraft.UpdatedAt = now;
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogWarning("Parsed order draft approval failed: {DraftId}, Errors: {Errors}", id, errorNotes);

            if (result.BuildingDetection != null)
            {
                throw new BuildingRequiredException(result.ErrorMessage ?? errorNotes, result.BuildingDetection);
            }

            throw new InvalidOperationException($"Order creation failed: {errorNotes}");
        }

        // Emit OrderCreatedEvent for new orders (parser path) so integrations and handlers can react
        if (!existingOrderId.HasValue && _eventBus != null && orderId != Guid.Empty && companyId != Guid.Empty)
        {
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = orderId,
                CompanyId = companyId,
                PartnerId = existing.PartnerId,
                BuildingId = existing.BuildingId,
                SourceSystem = "Parser",
                TriggeredByUserId = userId
            };
            await _eventBus.PublishAsync(orderCreatedEvent, cancellationToken);
        }

        // Update draft with created/updated order ID - use entity update instead of raw SQL
        var draftToUpdate = await _context.ParsedOrderDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);
        
        if (draftToUpdate != null)
        {
            draftToUpdate.ValidationStatus = "Valid";
            draftToUpdate.ValidationNotes = dto.ValidationNotes;
            draftToUpdate.CreatedOrderId = orderId;
            draftToUpdate.CreatedByUserId = userId;
            draftToUpdate.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Parsed order draft approved: {DraftId}, OrderId: {OrderId}, User: {UserId}, Action: {Action}", 
            id, orderId, userId, existingOrderId.HasValue ? "Updated" : "Created");

        return await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken) ?? existing;
    }

    public async Task<BulkApproveParsedOrdersResultDto> BulkApproveParsedOrdersAsync(IReadOnlyList<Guid> draftIds, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = new BulkApproveParsedOrdersResultDto();
        if (draftIds == null || draftIds.Count == 0)
            return result;

        var approveDto = new ApproveParsedOrderDto { ValidationNotes = null };
        foreach (var id in draftIds)
        {
            try
            {
                await ApproveParsedOrderAsync(id, approveDto, companyId, userId, cancellationToken);
                result.SucceededCount++;
                result.SucceededDraftIds.Add(id);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new BulkApproveErrorDto { DraftId = id, Message = ex.Message });
                _logger.LogWarning(ex, "Bulk approve failed for draft {DraftId}", id);
            }
        }

        _logger.LogInformation("Bulk approve completed: {Succeeded} succeeded, {Failed} failed", result.SucceededCount, result.FailedCount);
        return result;
    }

    public async Task<ParsedOrderDraftDto> RejectParsedOrderAsync(Guid id, RejectParsedOrderDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Parsed order draft with ID {id} not found");
        }

        var now = DateTime.UtcNow;

        // Use entity update instead of raw SQL
        var draftToReject = await _context.ParsedOrderDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);
        
        if (draftToReject != null)
        {
            draftToReject.ValidationStatus = "Rejected";
            draftToReject.ValidationNotes = dto.ValidationNotes ?? string.Empty;
            draftToReject.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Parsed order draft rejected: {DraftId}, User: {UserId}", id, userId);

        return await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken) ?? existing;
    }

    /// <summary>
    /// Mark a parsed order draft as approved after order was created through the full order form.
    /// This is called when the user reviews a draft in the CreateOrderPage and successfully creates an order.
    /// </summary>
    public async Task<ParsedOrderDraftDto> MarkDraftAsApprovedAsync(Guid id, Guid createdOrderId, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Parsed order draft with ID {id} not found");
        }

        var now = DateTime.UtcNow;

        // Update the draft to mark it as approved with the created order ID
        var draftToApprove = await _context.ParsedOrderDrafts
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);
        
        if (draftToApprove != null)
        {
            draftToApprove.ValidationStatus = "Valid";
            draftToApprove.CreatedOrderId = createdOrderId;
            draftToApprove.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Parsed order draft marked as approved: {DraftId}, CreatedOrderId: {OrderId}, User: {UserId}", id, createdOrderId, userId);

        return await GetParsedOrderDraftByIdAsync(id, companyId, cancellationToken) ?? existing;
    }

    public async Task<ParseSessionDto> CreateParseSessionFromFilesAsync(List<IFormFile> files, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();
        var now = EnsureUtc(DateTime.UtcNow); // Ensure UTC kind is explicitly set for PostgreSQL

        // Build file names list for source tracking
        var fileNames = string.Join(", ", files.Select(f => f.FileName));

        // Create parse session
        var session = new ParseSession
        {
            Id = sessionId,
            CompanyId = companyId,
            Status = "Processing",
            SourceType = "FileUpload",
            SourceDescription = $"Uploaded files: {fileNames}",
            ParsedOrdersCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
            EmailMessageId = null // File uploads don't have an email message
        };

        _context.ParseSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Parse session created: {SessionId}, Status: {Status}", sessionId, session.Status);

        var draftsCreated = 0;
        Guid? snapshotFileId = null;
        var sessionUpdated = false;

        try
        {
            _logger.LogInformation("Starting file processing for session {SessionId}", sessionId);
            // Save the first file as a PDF snapshot for audit purposes (convert Excel to PDF if needed)
            if (files.Count > 0)
            {
                try
                {
                    var firstFile = files[0];
                    var extension = Path.GetExtension(firstFile.FileName).ToLowerInvariant();
                    
                    // Convert Excel to PDF for consistent reference format
                    byte[] pdfBytes;
                    string pdfFileName;
                    
                    if (extension == ".xlsx" || extension == ".xls")
                    {
                        pdfBytes = await _excelToPdfService.ConvertToPdfAsync(firstFile, cancellationToken);
                        pdfFileName = Path.ChangeExtension(firstFile.FileName, ".pdf");
                        _logger.LogInformation("Converted Excel to PDF for snapshot: {FileName} -> {PdfFileName}", 
                            firstFile.FileName, pdfFileName);
                    }
                    else if (extension == ".pdf")
                    {
                        // Already PDF, just read it
                        using var stream = new MemoryStream();
                        await firstFile.CopyToAsync(stream, cancellationToken);
                        pdfBytes = stream.ToArray();
                        pdfFileName = firstFile.FileName;
                    }
                    else
                    {
                        // For other file types, save as-is
                        using var stream = new MemoryStream();
                        await firstFile.CopyToAsync(stream, cancellationToken);
                        pdfBytes = stream.ToArray();
                        pdfFileName = firstFile.FileName;
                    }
                    
                    // Create an IFormFile wrapper for PDF bytes
                    var pdfFile = new InMemoryFormFile(
                        pdfBytes,
                        pdfFileName,
                        "application/pdf");
                    
                    var uploadDto = new FileUploadDto
                    {
                        File = pdfFile,
                        Module = "Parser",
                        EntityId = sessionId,
                        EntityType = "ParseSession"
                    };
                    var savedFile = await _fileService.UploadFileAsync(uploadDto, companyId, userId, cancellationToken);
                    snapshotFileId = savedFile.Id;
                    
                    // Update the tracked session entity
                    session.SnapshotFileId = snapshotFileId;
                    await _context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("PDF snapshot saved for parse session: {SessionId}, FileId: {FileId}, FileName: {FileName}",
                        sessionId, savedFile.Id, pdfFileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save PDF snapshot for parse session: {SessionId}. Error: {ErrorMessage}", 
                        sessionId, ex.Message);
                    if (ex.InnerException != null)
                    {
                        _logger.LogWarning("Inner exception: {InnerException}", ex.InnerException.Message);
                    }
                    // Continue processing even if snapshot save fails - this is not critical
                    _logger.LogInformation("Continuing with file parsing despite snapshot save failure");
                }
            }

            // Process each file and create draft orders
            _logger.LogInformation("Starting to process {FileCount} file(s) for session {SessionId}", files.Count, sessionId);
            
            foreach (var file in files)
            {
                try
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    _logger.LogInformation("Processing file: {FileName}, Extension: {Extension}", file.FileName, extension);
                    
                    ParsedOrderDraft? draft = null;

                    // Parse based on file type
                    if (extension == ".xlsx" || extension == ".xls")
                    {
                        // Use TIME Excel parser for Excel files
                        _logger.LogInformation("Parsing Excel file: {FileName}", file.FileName);
                        draft = await ParseExcelFileAsync(file, sessionId, companyId, now, cancellationToken);
                        _logger.LogInformation("Excel file parsed: {FileName}, Draft created: {HasDraft}", file.FileName, draft != null);
                    }
                    else if (extension == ".pdf")
                    {
                        // PDF files - parse using PDF parser service
                        _logger.LogInformation("Parsing PDF file: {FileName}", file.FileName);
                        draft = await ParsePdfFileAsync(file, sessionId, companyId, now, cancellationToken);
                        _logger.LogInformation("PDF file parsed: {FileName}, Draft created: {HasDraft}", file.FileName, draft != null);
                    }
                    else if (extension == ".msg")
                    {
                        // Outlook MSG files - extract attachments and parse Excel files
                        var msgDrafts = await ParseMsgFileAsync(file, sessionId, companyId, now, cancellationToken);
                        if (msgDrafts != null && msgDrafts.Any())
                        {
                            foreach (var msgDraft in msgDrafts)
                            {
                                _context.ParsedOrderDrafts.Add(msgDraft);
                                draftsCreated++;
                            }
                            continue; // Skip adding draft below since we already added them
                        }
                        else
                        {
                            // No Excel attachments found in MSG
                            draft = CreatePlaceholderDraft(sessionId, companyId, now, file.FileName,
                                $"Outlook message uploaded: {file.FileName}. No Excel attachments found - please upload the Excel file directly.", 0.4m);
                        }
                    }
                    else
                    {
                        // Unknown file type
                        draft = CreatePlaceholderDraft(sessionId, companyId, now, file.FileName,
                            $"Unknown file type: {file.FileName}. Manual review required.", 0.3m);
                    }

                    if (draft != null)
                    {
                        _logger.LogInformation("Adding draft to context: DraftId={DraftId}, ServiceId={ServiceId}", draft.Id, draft.ServiceId);
                        _context.ParsedOrderDrafts.Add(draft);
                        draftsCreated++;
                        _logger.LogInformation("Draft added. Total drafts created so far: {Count}", draftsCreated);
                    }
                    else
                    {
                        _logger.LogWarning("No draft created for file: {FileName}", file.FileName);
                    }
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "Error processing file {FileName} in session {SessionId}: {Error}", 
                        file.FileName, sessionId, fileEx.Message);
                    // Create a placeholder draft so we know the file was processed but failed
                    try
                    {
                        var errorDraft = CreatePlaceholderDraft(sessionId, companyId, now, file.FileName,
                            $"Error processing file: {fileEx.Message}", 0.1m);
                        if (errorDraft != null)
                        {
                            _context.ParsedOrderDrafts.Add(errorDraft);
                            draftsCreated++;
                            _logger.LogInformation("Created error placeholder draft for failed file: {FileName}", file.FileName);
                        }
                    }
                    catch (Exception draftEx)
                    {
                        _logger.LogError(draftEx, "Failed to create error placeholder draft for {FileName}", file.FileName);
                    }
                    // Continue with next file
                }
            }
            
            _logger.LogInformation("Finished processing all files. Total drafts created: {Count}", draftsCreated);

            // Update session with results
            // If no drafts were created, mark as Completed with a warning message
            if (draftsCreated == 0)
            {
                session.Status = "Completed";
                session.ErrorMessage = "No order drafts were created from the uploaded files. Please check the file format and try again.";
                _logger.LogWarning("Parse session {SessionId} completed with 0 drafts. Files: {FileCount}", sessionId, files.Count);
            }
            else
            {
                session.Status = "Completed";
                session.ErrorMessage = null; // Clear any previous error message
            }
            
            session.ParsedOrdersCount = draftsCreated;
            session.CompletedAt = NormalizeToUtc(DateTime.UtcNow);
            session.UpdatedAt = EnsureUtc(DateTime.UtcNow);

            await _context.SaveChangesAsync(cancellationToken);
            sessionUpdated = true;

            _logger.LogInformation(
                "Parse session created from files: Session {SessionId}, Files: {FileCount}, Drafts: {DraftCount}, Status: {Status}",
                sessionId, files.Count, draftsCreated, session.Status);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Log database-specific errors with full details
            _logger.LogError(dbEx, "Database error creating parse session from files. SessionId: {SessionId}", sessionId);
            
            var innerMessage = dbEx.InnerException?.Message ?? "No inner exception";
            _logger.LogError("Database error details: {Details}", innerMessage);
            
            // Log entity validation errors if available
            if (dbEx.Entries != null && dbEx.Entries.Any())
            {
                foreach (var entry in dbEx.Entries)
                {
                    _logger.LogError("Entity: {EntityType}, State: {State}", entry.Entity.GetType().Name, entry.State);
                    foreach (var prop in entry.Properties)
                    {
                        _logger.LogError("  Property: {Property} = {Value} (IsModified: {IsModified})", 
                            prop.Metadata.Name, prop.CurrentValue, prop.IsModified);
                    }
                }
            }
            
            // Update session with error - reload from database (tenant-safe: scope by current tenant)
            try
            {
                var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
                var sessionToUpdate = (tenantId.HasValue && tenantId.Value != Guid.Empty)
                    ? await _context.ParseSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.CompanyId == tenantId.Value, cancellationToken)
                    : await _context.ParseSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
                if (sessionToUpdate != null)
                {
                    sessionToUpdate.Status = "Failed";
                    sessionToUpdate.ErrorMessage = $"Database error: {dbEx.Message} | {innerMessage}";
                    sessionToUpdate.UpdatedAt = EnsureUtc(DateTime.UtcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Updated parse session {SessionId} status to Failed", sessionId);
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save error status to parse session: {SessionId}", sessionId);
            }
            
            throw; // Re-throw to propagate the error
        }
        catch (Exception ex)
        {
            // Log full exception details
            _logger.LogError(ex, "Error creating parse session from files. SessionId: {SessionId}, Error: {ErrorMessage}", 
                sessionId, ex.Message);
            
            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerException}", ex.InnerException.ToString());
            }
            
            // Update session with error - reload from database (tenant-safe: scope by current tenant)
            try
            {
                var tenantId = CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
                var sessionToUpdate = (tenantId.HasValue && tenantId.Value != Guid.Empty)
                    ? await _context.ParseSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.CompanyId == tenantId.Value, cancellationToken)
                    : await _context.ParseSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
                if (sessionToUpdate != null)
                {
                    sessionToUpdate.Status = "Failed";
                    sessionToUpdate.ErrorMessage = ex.Message + (ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "");
                    sessionToUpdate.UpdatedAt = EnsureUtc(DateTime.UtcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Updated parse session {SessionId} status to Failed", sessionId);
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save error status to parse session: {SessionId}", sessionId);
                throw; // Re-throw original exception
            }
            
            throw; // Re-throw to propagate the error
        }
        finally
        {
            // Ensure session status is updated even if exception occurred
            // Note: We can't use await in finally, so we handle status updates in catch blocks above
            // This finally block is mainly for logging
            if (!sessionUpdated)
            {
                _logger.LogWarning("Parse session {SessionId} completed without setting sessionUpdated flag - check catch blocks for status update", sessionId);
            }
        }

        return new ParseSessionDto
        {
            Id = session.Id,
            CompanyId = session.CompanyId,
            Status = session.Status,
            ErrorMessage = session.ErrorMessage,
            SnapshotFileId = session.SnapshotFileId,
            ParsedOrdersCount = session.ParsedOrdersCount,
            CreatedAt = session.CreatedAt,
            CompletedAt = session.CompletedAt,
            SourceType = session.SourceType,
            SourceDescription = session.SourceDescription
        };
    }

    /// <summary>
    /// Parse Excel file using TIME Excel parser
    /// </summary>
    private async Task<ParsedOrderDraft> ParseExcelFileAsync(IFormFile file, Guid sessionId, Guid companyId, DateTime now, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parsing Excel file: {FileName}", file.FileName);
        
        // ✅ FIX: Read file into bytes ONCE to avoid stream consumption issues
        // This allows both the Excel parser and PDF fallback to read the file
        byte[] fileBytes;
        using (var tempStream = new MemoryStream())
        {
            await file.CopyToAsync(tempStream, cancellationToken);
            fileBytes = tempStream.ToArray();
        }
        
        // Create a reusable IFormFile from bytes for the Excel parser
        var reusableFile = new InMemoryFormFile(fileBytes, file.FileName, file.ContentType);
        
        var result = await _timeExcelParser.ParseAsync(reusableFile, null, cancellationToken);
        
        _logger.LogInformation("Parse result for {FileName}: Success={Success}, HasOrderData={HasOrderData}, ErrorMessage={ErrorMessage}, ValidationErrors={ErrorCount}",
            file.FileName, result.Success, result.OrderData != null, result.ErrorMessage, result.ValidationErrors?.Count ?? 0);

        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ParseSessionId = sessionId,
            SourceFileName = file.FileName,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Check if we have parsed order data (even with validation errors)
        if (result.OrderData != null)
        {
            var data = result.OrderData;

            // Map parsed data to draft
            draft.ServiceId = data.ServiceId;
            draft.TicketId = data.TicketId;
            draft.AwoNumber = data.AwoNumber; // ✅ Map AWO Number for Assurance orders
            draft.CustomerName = data.CustomerName ?? data.ContactPerson;
            draft.CustomerPhone = data.CustomerPhone;
            draft.CustomerEmail = data.CustomerEmail;
            draft.AddressText = data.ServiceAddress;
            draft.OldAddress = data.OldAddress;
            draft.AppointmentWindow = data.AppointmentWindow;
            draft.OrderTypeHint = data.OrderTypeHint;
            draft.OrderTypeCode = data.OrderTypeCode;
            draft.PackageName = data.PackageName;
            draft.Bandwidth = data.Bandwidth;
            draft.OnuSerialNumber = data.OnuSerialNumber;
            draft.OnuPassword = data.OnuPassword;
            draft.Username = data.Username;
            draft.Password = data.Password;
            draft.InternetWanIp = data.InternetWanIp;
            draft.InternetLanIp = data.InternetLanIp;
            draft.InternetGateway = data.InternetGateway;
            draft.InternetSubnetMask = data.InternetSubnetMask;
            draft.VoipServiceId = data.VoipServiceId;
            draft.Remarks = data.Remarks;
            draft.AdditionalInformation = data.AdditionalInformation;
            draft.ConfidenceScore = data.ConfidenceScore;
            if (data.Materials?.Any() == true)
            {
                var materialDtos = data.Materials
                    .Select(m => new ParsedDraftMaterialDto
                    {
                        Id = Guid.NewGuid(),
                        Name = m.Name,
                        ActionTag = m.ActionTag,
                        Quantity = m.Quantity,
                        UnitOfMeasure = m.UnitOfMeasure,
                        Notes = m.Notes
                    })
                    .ToList();

                draft.ParsedMaterialsJson = ParsedMaterialsSerializer.Serialize(materialDtos);
            }

            // Enrich draft with building matching, PDF fallback, date normalization, and validation status
            await _enrichmentService.EnrichDraftAsync(draft, result, reusableFile, companyId, cancellationToken);
            _enrichmentService.SetValidationStatus(draft, result, file.FileName, false);

            _logger.LogInformation("Excel file parsed: {FileName}, ServiceId: {ServiceId}, OrderType: {OrderType}, Confidence: {Confidence}, BuildingStatus: {BuildingStatus}, ValidationErrors: {ErrorCount}",
                file.FileName, data.ServiceId, data.OrderTypeCode, data.ConfidenceScore, draft.BuildingStatus, result.ValidationErrors?.Count ?? 0);
        }
        else
        {
            // Parsing completely failed - create draft with error info
            draft.ValidationStatus = "NeedsReview";
            draft.ValidationNotes = result.ErrorMessage ?? $"Failed to parse Excel file: {file.FileName}";
            draft.ConfidenceScore = 0.3m;

            _logger.LogWarning("Excel file parse failed: {FileName}, Error: {Error}",
                file.FileName, result.ErrorMessage);
        }

        return draft;
    }

    /// <summary>
    /// Parse PDF file using PDF parser service
    /// </summary>
    private async Task<ParsedOrderDraft> ParsePdfFileAsync(IFormFile file, Guid sessionId, Guid companyId, DateTime now, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parsing PDF file: {FileName}", file.FileName);
        
        try
        {
            // Extract text from PDF
            var pdfText = await _pdfTextExtractionService.ExtractTextAsync(file, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(pdfText))
            {
                _logger.LogWarning("PDF text extraction returned empty text for {FileName}", file.FileName);
                return CreatePlaceholderDraft(
                    sessionId, companyId, now, file.FileName,
                    "PDF text extraction returned empty content. Please review manually.", 0.3m);
            }
            
            // Parse order data from PDF text
            var parsedData = _pdfOrderParserService.ParseFromText(pdfText, file.FileName);
            
            _logger.LogInformation("PDF parse result for {FileName}: ServiceId={ServiceId}, OrderType={OrderType}, Confidence={Confidence}",
                file.FileName, parsedData.ServiceId, parsedData.OrderTypeCode, parsedData.ConfidenceScore);
            
            var draft = new ParsedOrderDraft
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ParseSessionId = sessionId,
                SourceFileName = file.FileName,
                CreatedAt = now,
                UpdatedAt = now
            };
            
            // Map parsed data to draft (same mapping as Excel parser)
            draft.ServiceId = parsedData.ServiceId;
            draft.AwoNumber = parsedData.AwoNumber; // ✅ Map AWO Number for Assurance orders
            draft.TicketId = parsedData.TicketId;
            draft.CustomerName = parsedData.CustomerName ?? parsedData.ContactPerson;
            draft.CustomerPhone = parsedData.CustomerPhone;
            draft.CustomerEmail = parsedData.CustomerEmail;
            draft.AddressText = parsedData.ServiceAddress;
            draft.OldAddress = parsedData.OldAddress;
            draft.AppointmentDate = NormalizeToUtc(parsedData.AppointmentDateTime);
            draft.AppointmentWindow = parsedData.AppointmentWindow;
            draft.OrderTypeHint = parsedData.OrderTypeHint;
            draft.OrderTypeCode = parsedData.OrderTypeCode;
            draft.PackageName = parsedData.PackageName;
            draft.Bandwidth = parsedData.Bandwidth;
            draft.OnuSerialNumber = parsedData.OnuSerialNumber;
            draft.OnuPassword = parsedData.OnuPassword;
            draft.Username = parsedData.Username;
            draft.Password = parsedData.Password;
            draft.InternetWanIp = parsedData.InternetWanIp;
            draft.InternetLanIp = parsedData.InternetLanIp;
            draft.InternetGateway = parsedData.InternetGateway;
            draft.InternetSubnetMask = parsedData.InternetSubnetMask;
            draft.VoipServiceId = parsedData.VoipServiceId;
            draft.ConfidenceScore = parsedData.ConfidenceScore;
            
            // Store PartnerCode in Remarks if available
            var remarksParts = new List<string>();
            if (!string.IsNullOrEmpty(parsedData.PartnerCode) && parsedData.PartnerCode != "TIME")
            {
                remarksParts.Add($"[PartnerCode: {parsedData.PartnerCode}]");
            }
            if (!string.IsNullOrEmpty(parsedData.Remarks))
            {
                remarksParts.Add(parsedData.Remarks);
            }
            draft.Remarks = remarksParts.Any() ? string.Join(" | ", remarksParts) : null;
            
            // BUILDING MATCHING: Try to match building against existing buildings
            try
            {
                var (buildingName, city, postcode) = ExtractBuildingInfoFromAddress(draft.AddressText);
                draft.BuildingName = buildingName;
                
                var matchedBuilding = await _buildingMatchingService.FindMatchingBuildingAsync(
                    buildingName,
                    draft.AddressText,
                    city,
                    postcode,
                    null, // buildingCode not available in PDF
                    companyId,
                    cancellationToken);
                
                if (matchedBuilding != null)
                {
                    draft.BuildingId = matchedBuilding.Id;
                    draft.BuildingStatus = "Existing";
                    _logger.LogInformation("Building matched for PDF draft: BuildingName={BuildingName}, BuildingId={BuildingId}", 
                        buildingName, matchedBuilding.Id);
                }
                else
                {
                    // Auto-create building if not found (similar to RFB email processing)
                    var createdBuildingId = await AutoCreateBuildingAsync(
                        draft.AddressText,
                        buildingName,
                        companyId,
                        file.FileName,
                        cancellationToken);
                    
                    if (createdBuildingId.HasValue)
                    {
                        draft.BuildingId = createdBuildingId.Value;
                        draft.BuildingStatus = "Existing";
                        _logger.LogInformation("✅ Building auto-created for PDF draft: BuildingName={BuildingName}, BuildingId={BuildingId}", 
                            buildingName, createdBuildingId.Value);
                    }
                    else
                    {
                        draft.BuildingStatus = "New";
                        _logger.LogInformation("No building match found for PDF and auto-creation failed. BuildingStatus=New, BuildingName={BuildingName}", buildingName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Building matching/creation failed for PDF {FileName}, will mark as New", file.FileName);
                draft.BuildingStatus = "New";
            }
            
            // Set validation status based on extracted fields
            if (parsedData.ConfidenceScore >= 0.7m && !string.IsNullOrEmpty(parsedData.ServiceId))
            {
                draft.ValidationStatus = "Pending";
                draft.ValidationNotes = $"Successfully parsed from {file.FileName}. Order Type: {parsedData.OrderTypeCode}";
            }
            else
            {
                draft.ValidationStatus = "NeedsReview";
                var notes = new List<string>();
                if (string.IsNullOrEmpty(parsedData.ServiceId))
                {
                    notes.Add("Service ID not found in PDF");
                }
                if (parsedData.ConfidenceScore < 0.7m)
                {
                    notes.Add($"Low confidence score: {parsedData.ConfidenceScore:P0}");
                }
                draft.ValidationNotes = notes.Any() 
                    ? $"Parsed from {file.FileName}. {string.Join("; ", notes)}" 
                    : $"PDF parsed with some fields missing from {file.FileName}";
            }
            
            _logger.LogInformation("PDF file parsed: {FileName}, ServiceId: {ServiceId}, OrderType: {OrderType}, Confidence: {Confidence}, BuildingStatus: {BuildingStatus}",
                file.FileName, parsedData.ServiceId, parsedData.OrderTypeCode, parsedData.ConfidenceScore, draft.BuildingStatus);
            
            return draft;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PDF file {FileName}: {Error}", file.FileName, ex.Message);
            return CreatePlaceholderDraft(
                sessionId, companyId, now, file.FileName,
                $"PDF parsing error: {ex.Message}. Please review manually.", 0.3m);
        }
    }

    /// <summary>
    /// Create a placeholder draft for unsupported file types
    /// </summary>
    private static ParsedOrderDraft CreatePlaceholderDraft(Guid sessionId, Guid companyId, DateTime now, string fileName, string validationNotes, decimal confidenceScore)
    {
        return new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ParseSessionId = sessionId,
            SourceFileName = fileName,
            ValidationStatus = "NeedsReview",
            ValidationNotes = validationNotes,
            ConfidenceScore = confidenceScore,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Parse Outlook MSG file and extract Excel attachments for processing
    /// </summary>
    private async Task<List<ParsedOrderDraft>?> ParseMsgFileAsync(
        IFormFile file, 
        Guid sessionId, 
        Guid companyId, 
        DateTime now, 
        CancellationToken cancellationToken)
    {
        var drafts = new List<ParsedOrderDraft>();
        
        try
        {
            _logger.LogInformation("Parsing MSG file: {FileName}", file.FileName);
            
            // Read MSG file into memory
            using var msgStream = new MemoryStream();
            await file.CopyToAsync(msgStream, cancellationToken);
            msgStream.Position = 0;
            
            // Parse MSG using MsgReader
            using var message = new Storage.Message(msgStream);
            
            _logger.LogInformation("MSG parsed: Subject='{Subject}', From='{From}', Attachments={Count}",
                message.Subject, message.Sender?.Email ?? "Unknown", message.Attachments?.Count ?? 0);
            
            if (message.Attachments == null || message.Attachments.Count == 0)
            {
                _logger.LogWarning("No attachments found in MSG file: {FileName}", file.FileName);
                return null;
            }
            
            // Process each attachment
            foreach (var attachment in message.Attachments)
            {
                try
                {
                    // Check if it's a file attachment (not embedded message)
                    if (attachment is Storage.Attachment fileAttachment)
                    {
                        var attachmentName = fileAttachment.FileName ?? "unknown";
                        var attachmentExt = Path.GetExtension(attachmentName).ToLowerInvariant();
                        
                        _logger.LogInformation("Processing attachment: {Name}, Extension: {Ext}", attachmentName, attachmentExt);
                        
                        // Only process Excel files
                        if (attachmentExt == ".xls" || attachmentExt == ".xlsx" || attachmentExt == ".xlsm")
                        {
                            var attachmentData = fileAttachment.Data;
                            
                            if (attachmentData == null || attachmentData.Length == 0)
                            {
                                _logger.LogWarning("Attachment {Name} has no data, skipping", attachmentName);
                                continue;
                            }
                            
                            // Create IFormFile from attachment data
                            var attachmentStream = new MemoryStream(attachmentData);
                            var contentType = attachmentExt == ".xlsx" 
                                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                                : "application/vnd.ms-excel";
                            
                            var formFile = new InMemoryFormFile(attachmentData, attachmentName, contentType);
                            
                            // Parse the Excel attachment
                            var draft = await ParseExcelFileAsync(formFile, sessionId, companyId, now, cancellationToken);
                            
                            if (draft != null)
                            {
                                // Add note about source MSG file
                                draft.ValidationNotes = $"Extracted from email: {file.FileName}. " + (draft.ValidationNotes ?? "");
                                drafts.Add(draft);
                                
                                _logger.LogInformation("Successfully parsed Excel attachment: {AttachmentName} from MSG: {MsgFile}",
                                    attachmentName, file.FileName);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Skipping non-Excel attachment: {Name}", attachmentName);
                        }
                    }
                }
                catch (Exception attachEx)
                {
                    _logger.LogWarning(attachEx, "Error processing attachment in MSG file: {FileName}", file.FileName);
                }
            }
            
            if (drafts.Count == 0)
            {
                _logger.LogWarning("No Excel attachments found in MSG file: {FileName}", file.FileName);
                return null;
            }
            
            _logger.LogInformation("MSG file processed: {FileName}, Excel attachments parsed: {Count}", 
                file.FileName, drafts.Count);
            
            return drafts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing MSG file: {FileName}", file.FileName);
            return null;
        }
    }

    /// <summary>
    /// Auto-create building when not found during parsing (similar to RFB email processing)
    /// Note: This method is still used by ParsePdfFileAsync which has different enrichment logic
    /// </summary>
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

        // Split on comma and newline, trim whitespace, remove empty parts
        var parts = System.Text.RegularExpressions.Regex
            .Split(addressText, @"[\r\n,]+")
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        // Priority order: MENARA > WISMA > PANGSAPURI > BANGUNAN > KOMPLEKS > MEDAN (lower number = higher priority)
        var priorityKeywords = new (string keyword, int priority)[]
        {
            ("menara", 1), ("wisma", 2), ("pangsapuri", 3), ("bangunan", 4), ("kompleks", 5), ("medan", 6)
        };
        // English keywords as fallback (lowest priority)
        var englishKeywords = new[] { "residence", "tower", "condominium", "condo", "apartment", "apt", "plaza", "building", "court", "vista", "suites", "heights", "mansion", "park", "centre", "center", "complex" };
        const int englishPriority = 99;

        var unitKeywords = new[] { "level", "floor", "unit", "block", "lot", "no.", "no ", "#" };
        var streetPrefixes = new[] { "jalan", "jln", "lorong", "lg", "persiaran", "jalan besar", "jl " };

        string? buildingName = null;
        string? postcode = null;
        string? city = null;

        var candidates = new List<(string segment, int priority)>();

        foreach (var part in parts)
        {
            var lowerPart = part.ToLowerInvariant();

            if (unitKeywords.Any(kw => lowerPart.Contains(kw)))
                continue;

            if (streetPrefixes.Any(prefix => lowerPart.StartsWith(prefix.Trim(), StringComparison.OrdinalIgnoreCase) && part.Length > prefix.Trim().Length + 2))
                continue;

            var words = part.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (words.Length == 0) continue;

            int? bestPriority = null;
            foreach (var (keyword, priority) in priorityKeywords)
            {
                if (words.Any(w => w.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    bestPriority = priority;
                    break;
                }
            }
            if (!bestPriority.HasValue)
            {
                if (englishKeywords.Any(kw => words.Any(w => w.Equals(kw, StringComparison.OrdinalIgnoreCase))))
                    bestPriority = englishPriority;
            }
            if (bestPriority.HasValue)
                candidates.Add((part.Trim(), bestPriority.Value));
        }

        if (candidates.Count > 0)
            buildingName = candidates.OrderBy(c => c.priority).ThenBy(c => candidates.IndexOf(c)).First().segment;

        postcode = parts.FirstOrDefault(p => System.Text.RegularExpressions.Regex.IsMatch(p, @"^\d{5}$"));
        var postcodeIndex = Array.IndexOf(parts, postcode ?? "");
        if (postcodeIndex >= 0 && postcodeIndex + 1 < parts.Length)
            city = parts[postcodeIndex + 1];
        else if (parts.Length >= 2)
            city = parts[parts.Length - 2];

        return (buildingName, city, postcode);
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


/// <summary>
/// In-memory implementation of IFormFile for creating file uploads from byte arrays
/// </summary>
internal class InMemoryFormFile : IFormFile
{
    private readonly byte[] _bytes;
    private readonly string _fileName;
    private readonly string _contentType;

    public InMemoryFormFile(byte[] bytes, string fileName, string contentType)
    {
        _bytes = bytes;
        _fileName = fileName;
        _contentType = contentType;
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
    public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get; } = new Microsoft.AspNetCore.Http.HeaderDictionary();
    public long Length => _bytes.Length;
    public string Name => "file";
    public string FileName => _fileName;

    public Stream OpenReadStream() => new MemoryStream(_bytes, false);

    public void CopyTo(Stream target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        target.Write(_bytes, 0, _bytes.Length);
    }

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        return target.WriteAsync(_bytes, 0, _bytes.Length, cancellationToken);
    }
}

    public async Task<List<ParseSessionDto>> GetFailedParseSessionsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var failedSessions = await _context.ParseSessions
            .Where(s => s.CompanyId == companyId && (s.Status == "Failed" || (!string.IsNullOrEmpty(s.ErrorMessage))))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return failedSessions.Select(s => new ParseSessionDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            EmailMessageId = s.EmailMessageId,
            ParserTemplateId = s.ParserTemplateId,
            Status = s.Status ?? string.Empty,
            ErrorMessage = s.ErrorMessage,
            SnapshotFileId = s.SnapshotFileId,
            ParsedOrdersCount = s.ParsedOrdersCount,
            CreatedAt = s.CreatedAt,
            CompletedAt = s.CompletedAt,
            SourceType = s.SourceType,
            SourceDescription = s.SourceDescription
        }).ToList();
    }

    public async Task<PagedResultDto<ParsedOrderDraftDto>> GetParsedOrderDraftsWithFiltersAsync(
        Guid companyId,
        string? validationStatus = null,
        string? sourceType = null,
        string? status = null,
        string? serviceId = null,
        string? customerName = null,
        Guid? partnerId = null,
        string? buildingStatus = null,
        decimal? confidenceMin = null,
        bool? buildingMatched = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortOrder = "desc",
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        // Multi-tenant SaaS — CompanyId filter required.
        var query = from draft in _context.ParsedOrderDrafts
                    join session in _context.ParseSessions on draft.ParseSessionId equals session.Id
                    where draft.CompanyId == companyId && session.CompanyId == companyId
                    select new { Draft = draft, Session = session };

        // Apply filters
        if (!string.IsNullOrEmpty(validationStatus))
        {
            query = query.Where(x => x.Draft.ValidationStatus == validationStatus);
        }

        if (!string.IsNullOrEmpty(sourceType))
        {
            query = query.Where(x => x.Session.SourceType == sourceType);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(x => x.Session.Status == status);
        }

        if (!string.IsNullOrEmpty(serviceId))
        {
            query = query.Where(x => x.Draft.ServiceId != null && x.Draft.ServiceId.Contains(serviceId));
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            query = query.Where(x => x.Draft.CustomerName != null && x.Draft.CustomerName.Contains(customerName));
        }

        if (partnerId.HasValue)
        {
            query = query.Where(x => x.Draft.PartnerId == partnerId.Value);
        }

        if (!string.IsNullOrEmpty(buildingStatus))
        {
            query = query.Where(x => x.Draft.BuildingStatus == buildingStatus);
        }

        if (confidenceMin.HasValue)
        {
            query = query.Where(x => x.Draft.ConfidenceScore >= confidenceMin.Value);
        }

        if (buildingMatched.HasValue)
        {
            if (buildingMatched.Value)
                query = query.Where(x => x.Draft.BuildingId != null);
            else
                query = query.Where(x => x.Draft.BuildingId == null);
        }

        if (fromDate.HasValue)
        {
            var fromDateUtc = fromDate.Value.ToUniversalTime();
            query = query.Where(x => x.Draft.CreatedAt >= fromDateUtc);
        }

        if (toDate.HasValue)
        {
            var toDateUtc = toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(x => x.Draft.CreatedAt <= toDateUtc);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            var isDescending = sortOrder?.ToLowerInvariant() == "desc";
            switch (sortBy.ToLowerInvariant())
            {
                case "serviceid":
                    query = isDescending ? query.OrderByDescending(x => x.Draft.ServiceId) : query.OrderBy(x => x.Draft.ServiceId);
                    break;
                case "customername":
                    query = isDescending ? query.OrderByDescending(x => x.Draft.CustomerName) : query.OrderBy(x => x.Draft.CustomerName);
                    break;
                case "createdat":
                    query = isDescending ? query.OrderByDescending(x => x.Draft.CreatedAt) : query.OrderBy(x => x.Draft.CreatedAt);
                    break;
                case "validationstatus":
                    query = isDescending ? query.OrderByDescending(x => x.Draft.ValidationStatus) : query.OrderBy(x => x.Draft.ValidationStatus);
                    break;
                case "confidencescore":
                    query = isDescending ? query.OrderByDescending(x => x.Draft.ConfidenceScore) : query.OrderBy(x => x.Draft.ConfidenceScore);
                    break;
                default:
                    query = query.OrderByDescending(x => x.Draft.CreatedAt);
                    break;
            }
        }
        else
        {
            query = query.OrderByDescending(x => x.Draft.CreatedAt);
        }

        // Apply pagination
        var skip = (page - 1) * pageSize;
        var results = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var draftDtos = results.Select(x => new ParsedOrderDraftDto
        {
            Id = x.Draft.Id,
            CompanyId = x.Draft.CompanyId,
            ParseSessionId = x.Draft.ParseSessionId,
            PartnerId = x.Draft.PartnerId,
            BuildingId = x.Draft.BuildingId,
            BuildingName = x.Draft.BuildingName,
            BuildingStatus = x.Draft.BuildingStatus,
            ServiceId = x.Draft.ServiceId,
            TicketId = x.Draft.TicketId,
            AwoNumber = x.Draft.AwoNumber, // ✅ Map AWO Number for Assurance orders
            CustomerName = x.Draft.CustomerName,
            CustomerPhone = x.Draft.CustomerPhone,
            CustomerEmail = x.Draft.CustomerEmail,
            AddressText = x.Draft.AddressText,
            OldAddress = x.Draft.OldAddress,
            AppointmentDate = x.Draft.AppointmentDate,
            AppointmentWindow = x.Draft.AppointmentWindow,
            OrderTypeHint = x.Draft.OrderTypeHint,
            OrderTypeCode = x.Draft.OrderTypeCode,
            OrderCategoryId = x.Draft.OrderCategoryId,
            PackageName = x.Draft.PackageName,
            Bandwidth = x.Draft.Bandwidth,
            OnuSerialNumber = x.Draft.OnuSerialNumber,
            VoipServiceId = x.Draft.VoipServiceId,
            Remarks = x.Draft.Remarks,
            AdditionalInformation = x.Draft.AdditionalInformation,
            SourceFileName = x.Draft.SourceFileName,
            ConfidenceScore = x.Draft.ConfidenceScore,
            ValidationStatus = x.Draft.ValidationStatus ?? "Pending",
            ValidationNotes = x.Draft.ValidationNotes,
            CreatedOrderId = x.Draft.CreatedOrderId,
            CreatedByUserId = x.Draft.CreatedByUserId,
            CreatedAt = x.Draft.CreatedAt,
            Materials = ParsedMaterialsSerializer.Deserialize(x.Draft.ParsedMaterialsJson),
            UnmatchedMaterialCount = x.Draft.UnmatchedMaterialCount ?? 0,
            UnmatchedMaterialNames = DeserializeUnmatchedMaterialNames(x.Draft.UnmatchedMaterialNamesJson)
        }).ToList();

        var map = await GetExistingOrderIdsMapAsync(companyId, cancellationToken);
        foreach (var dto in draftDtos)
        {
            if (!string.IsNullOrWhiteSpace(dto.ServiceId) && !dto.CreatedOrderId.HasValue && map.TryGetValue(NormalizeServiceId(dto.ServiceId), out var orderId))
                dto.ExistingOrderId = orderId;
        }

        return new PagedResultDto<ParsedOrderDraftDto>
        {
            Items = draftDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ParserStatisticsDto> GetParserStatisticsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Multi-tenant SaaS — CompanyId filter required.
        var sessionsToday = await _context.ParseSessions
            .Where(s => s.CompanyId == companyId && s.CreatedAt >= today && s.CreatedAt < tomorrow)
            .ToListAsync(cancellationToken);

        var totalSessionsToday = sessionsToday.Count;
        var successfulSessionsToday = sessionsToday.Count(s => s.Status == "Completed");
        var failedSessionsToday = sessionsToday.Count(s => s.Status == "Failed" || s.Status == "Error");

        var totalSessionsAllTime = await _context.ParseSessions.Where(s => s.CompanyId == companyId).CountAsync(cancellationToken);

        var allDrafts = await _context.ParsedOrderDrafts.Where(d => d.CompanyId == companyId).ToListAsync(cancellationToken);
        
        var totalDrafts = allDrafts.Count;
        var pendingDrafts = allDrafts.Count(d => d.ValidationStatus == "Pending");
        var validDrafts = allDrafts.Count(d => d.ValidationStatus == "Valid");
        var needsReviewDrafts = allDrafts.Count(d => d.ValidationStatus == "NeedsReview");
        var rejectedDrafts = allDrafts.Count(d => d.ValidationStatus == "Rejected");
        var approvedDrafts = allDrafts.Count(d => d.CreatedOrderId != null);

        // Average confidence score
        var averageConfidenceScore = allDrafts.Any() 
            ? allDrafts.Average(d => (double)d.ConfidenceScore) 
            : 0;

        return new ParserStatisticsDto
        {
            TotalSessionsToday = totalSessionsToday,
            SuccessfulSessionsToday = successfulSessionsToday,
            FailedSessionsToday = failedSessionsToday,
            TotalDrafts = totalDrafts,
            PendingDrafts = pendingDrafts,
            ValidDrafts = validDrafts,
            NeedsReviewDrafts = needsReviewDrafts,
            RejectedDrafts = rejectedDrafts,
            ApprovedDrafts = approvedDrafts,
            AverageConfidenceScore = (decimal)averageConfidenceScore,
            TotalSessionsAllTime = totalSessionsAllTime,
            TotalDraftsAllTime = totalDrafts
        };
    }

    public async Task<ParserAnalyticsDto> GetParserAnalyticsAsync(Guid companyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            throw new InvalidOperationException("Tenant context missing: CompanyId required.");
        TenantSafetyGuard.AssertTenantContext();
        var to = toDate ?? DateTime.UtcNow.Date.AddDays(1);
        var from = fromDate ?? to.AddDays(-30);
        if (from >= to) from = to.AddDays(-30);

        // Multi-tenant SaaS — CompanyId filter required.
        var sessionsInPeriod = await _context.ParseSessions
            .Where(s => s.CompanyId == companyId && s.CreatedAt >= from && s.CreatedAt < to)
            .ToListAsync(cancellationToken);

        var totalSessions = sessionsInPeriod.Count;
        var completedSessions = sessionsInPeriod.Count(s => s.Status == "Completed");
        var failedSessions = sessionsInPeriod.Count(s => s.Status == "Failed" || s.Status == "Error");
        var parseSuccessRate = totalSessions > 0 ? (decimal)(100.0 * completedSessions / totalSessions) : 0;

        // Drafts in period (drafts whose parse session is in period), scoped to tenant
        var sessionIdsInPeriod = sessionsInPeriod.Select(s => s.Id).ToHashSet();
        var draftsInPeriod = await _context.ParsedOrderDrafts
            .Where(d => d.CompanyId == companyId && sessionIdsInPeriod.Contains(d.ParseSessionId))
            .ToListAsync(cancellationToken);

        var totalDrafts = draftsInPeriod.Count;
        var buildingMatchedDrafts = draftsInPeriod.Count(d => d.BuildingStatus == "Existing" || d.BuildingId != null);
        var autoMatchRate = totalDrafts > 0 ? (decimal)(100.0 * buildingMatchedDrafts / totalDrafts) : 0;

        // Confidence distribution (0-50, 50-80, 80-100)
        var confidenceDistribution = new List<ConfidenceBucketDto>
        {
            new() { Label = "0-50%", Count = draftsInPeriod.Count(d => d.ConfidenceScore < 50) },
            new() { Label = "50-80%", Count = draftsInPeriod.Count(d => d.ConfidenceScore >= 50 && d.ConfidenceScore < 80) },
            new() { Label = "80-100%", Count = draftsInPeriod.Count(d => d.ConfidenceScore >= 80) }
        };

        // Common errors: session error messages (top 10 by count)
        const int maxErrorLength = 200;
        var sessionErrors = sessionsInPeriod
            .Where(s => !string.IsNullOrWhiteSpace(s.ErrorMessage))
            .Select(s => s.ErrorMessage!.Length > maxErrorLength ? s.ErrorMessage[..maxErrorLength] + "…" : s.ErrorMessage)
            .GroupBy(m => m)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new ErrorCountDto { Message = g.Key, Count = g.Count() })
            .ToList();

        // Add top validation notes from rejected/invalid drafts
        var validationErrors = draftsInPeriod
            .Where(d => (d.ValidationStatus == "Rejected" || d.ValidationStatus == "Invalid") && !string.IsNullOrWhiteSpace(d.ValidationNotes))
            .Select(d => d.ValidationNotes!.Length > maxErrorLength ? d.ValidationNotes[..maxErrorLength] + "…" : d.ValidationNotes)
            .GroupBy(m => m!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new ErrorCountDto { Message = g.Key, Count = g.Count() })
            .ToList();

        var commonErrors = sessionErrors
            .Concat(validationErrors)
            .GroupBy(e => e.Message)
            .Select(g => new ErrorCountDto { Message = g.Key, Count = g.Sum(x => x.Count) })
            .OrderByDescending(e => e.Count)
            .Take(10)
            .ToList();

        // Orders created from parser per day (draft.CreatedOrderId -> Order.CreatedAt in period)
        var orderIdsFromDrafts = draftsInPeriod
            .Where(d => d.CreatedOrderId.HasValue)
            .Select(d => d.CreatedOrderId!.Value)
            .Distinct()
            .ToList();

        var ordersCreatedPerDay = new List<OrdersPerDayDto>();
        if (orderIdsFromDrafts.Count > 0)
        {
            var ordersInPeriod = await _context.Orders
                .Where(o => orderIdsFromDrafts.Contains(o.Id) && o.CreatedAt >= from && o.CreatedAt < to)
                .Select(o => new { o.CreatedAt })
                .ToListAsync(cancellationToken);

            var byDay = ordersInPeriod
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new OrdersPerDayDto { Date = g.Key.ToString("yyyy-MM-dd"), Count = g.Count() })
                .ToList();

            // Fill in missing days with 0 so frontend can show a continuous chart
            var allDays = new List<OrdersPerDayDto>();
            for (var d = from.Date; d < to.Date; d = d.AddDays(1))
            {
                var existing = byDay.FirstOrDefault(x => x.Date == d.ToString("yyyy-MM-dd"));
                allDays.Add(existing ?? new OrdersPerDayDto { Date = d.ToString("yyyy-MM-dd"), Count = 0 });
            }
            ordersCreatedPerDay = allDays;
        }
        else
        {
            for (var d = from.Date; d < to.Date; d = d.AddDays(1))
                ordersCreatedPerDay.Add(new OrdersPerDayDto { Date = d.ToString("yyyy-MM-dd"), Count = 0 });
        }

        return new ParserAnalyticsDto
        {
            ParseSuccessRate = parseSuccessRate,
            AutoMatchRate = autoMatchRate,
            TotalSessions = totalSessions,
            CompletedSessions = completedSessions,
            FailedSessions = failedSessions,
            TotalDrafts = totalDrafts,
            BuildingMatchedDrafts = buildingMatchedDrafts,
            ConfidenceDistribution = confidenceDistribution,
            CommonErrors = commonErrors,
            OrdersCreatedPerDay = ordersCreatedPerDay,
            FromDate = from,
            ToDate = to
        };
    }

    public async Task<ParseSessionDto> RetryParseSessionAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        // Get the original parse session
        var originalSession = await _context.ParseSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (originalSession == null)
        {
            throw new KeyNotFoundException($"Parse session with ID {id} not found");
        }

        // For email-based sessions, trigger re-processing of the email account
        if (originalSession.EmailMessageId.HasValue)
        {
            var emailMessage = await _context.Set<CephasOps.Domain.Parser.Entities.EmailMessage>()
                .FirstOrDefaultAsync(e => e.Id == originalSession.EmailMessageId.Value, cancellationToken);

            if (emailMessage != null && _emailIngestionServiceLazy != null)
            {
                _logger.LogInformation("Retrying parse session {SessionId} by re-processing email account {EmailAccountId}", 
                    id, emailMessage.EmailAccountId);
                
                // Trigger re-poll of the email account - this will re-process the email
                var result = await _emailIngestionServiceLazy.Value.TriggerPollAsync(emailMessage.EmailAccountId, companyId, cancellationToken);
                
                if (result.Success && result.ParseSessionsCreated > 0)
                {
                    // Get the newly created session (should be the most recent one for this email)
                    var newSession = await _context.ParseSessions
                        .Where(s => s.EmailMessageId == originalSession.EmailMessageId && s.Id != id)
                        .OrderByDescending(s => s.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (newSession != null)
                    {
                        return new ParseSessionDto
                        {
                            Id = newSession.Id,
                            CompanyId = newSession.CompanyId,
                            EmailMessageId = newSession.EmailMessageId,
                            ParserTemplateId = newSession.ParserTemplateId,
                            Status = newSession.Status ?? string.Empty,
                            ErrorMessage = newSession.ErrorMessage,
                            SnapshotFileId = newSession.SnapshotFileId,
                            ParsedOrdersCount = newSession.ParsedOrdersCount,
                            CreatedAt = newSession.CreatedAt,
                            CompletedAt = newSession.CompletedAt,
                            SourceType = newSession.SourceType,
                            SourceDescription = newSession.SourceDescription
                        };
                    }
                }
            }
        }

        // For file upload sessions or if email retry failed, create a new session with same source info
        // but mark it as a retry
        var retrySession = new ParseSession
        {
            Id = Guid.NewGuid(),
            CompanyId = originalSession.CompanyId,
            EmailMessageId = originalSession.EmailMessageId,
            ParserTemplateId = originalSession.ParserTemplateId,
            Status = "Pending",
            SourceType = originalSession.SourceType,
            SourceDescription = $"Retry: {originalSession.SourceDescription}",
            CreatedAt = DateTime.UtcNow
        };

        _context.ParseSessions.Add(retrySession);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created retry parse session {RetrySessionId} for original session {OriginalSessionId}", 
            retrySession.Id, id);

        return new ParseSessionDto
        {
            Id = retrySession.Id,
            CompanyId = retrySession.CompanyId,
            EmailMessageId = retrySession.EmailMessageId,
            ParserTemplateId = retrySession.ParserTemplateId,
            Status = retrySession.Status ?? string.Empty,
            ErrorMessage = retrySession.ErrorMessage,
            SnapshotFileId = retrySession.SnapshotFileId,
            ParsedOrdersCount = retrySession.ParsedOrdersCount,
            CreatedAt = retrySession.CreatedAt,
            CompletedAt = retrySession.CompletedAt,
            SourceType = retrySession.SourceType,
            SourceDescription = retrySession.SourceDescription
        };
    }

    /// <inheritdoc />
    public async Task<OrderExistsByServiceIdDto> CheckOrderExistsByServiceIdAsync(Guid companyId, string serviceId, CancellationToken cancellationToken = default)
    {
        var result = new OrderExistsByServiceIdDto { Exists = false, ServiceId = serviceId?.Trim() };
        if (string.IsNullOrWhiteSpace(serviceId))
            return result;

        var normalized = serviceId.Trim().ToUpperInvariant();
        var candidateOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.CompanyId == companyId && !string.IsNullOrWhiteSpace(o.ServiceId))
            .Select(o => new { o.Id, o.ServiceId, o.TicketId })
            .ToListAsync(cancellationToken);

        var matching = candidateOrders
            .FirstOrDefault(o => string.Equals(o.ServiceId?.Trim().ToUpperInvariant(), normalized, StringComparison.Ordinal));

        if (matching != null)
        {
            result.Exists = true;
            result.OrderId = matching.Id;
            result.ServiceId = matching.ServiceId;
            result.TicketId = matching.TicketId;
        }

        return result;
    }

    /// <summary>
    /// Determines if an order type should use database-first strategy (pull customer info from existing ServiceId)
    /// </summary>
    /// <param name="orderTypeCode">Order type code (e.g., ASSURANCE, MODIFICATION_INDOOR, etc.)</param>
    /// <param name="orderTypeHint">Order type hint (optional fallback)</param>
    /// <returns>True if order type should use database-first strategy</returns>
    private static bool ShouldUseDatabaseFirstStrategy(string? orderTypeCode, string? orderTypeHint)
    {
        // Check by code first (preferred)
        if (!string.IsNullOrWhiteSpace(orderTypeCode))
        {
            var codeUpper = orderTypeCode.Trim().ToUpperInvariant();
            if (codeUpper == "ASSURANCE" || 
                codeUpper == "MODIFICATION_INDOOR" || 
                codeUpper == "MODIFICATION_OUTDOOR" ||
                codeUpper == "VALUE_ADDED_SERVICE" ||
                codeUpper == "VAS")
            {
                return true;
            }
        }
        
        // Fallback to hint if code not available
        if (!string.IsNullOrWhiteSpace(orderTypeHint))
        {
            var hintUpper = orderTypeHint.Trim().ToUpperInvariant();
            if (hintUpper.Contains("ASSURANCE", StringComparison.OrdinalIgnoreCase) ||
                hintUpper.Contains("MODIFICATION", StringComparison.OrdinalIgnoreCase) ||
                hintUpper.Contains("VALUE ADDED", StringComparison.OrdinalIgnoreCase) ||
                hintUpper.Contains("VAS", StringComparison.OrdinalIgnoreCase) ||
                hintUpper.Contains("TTKT", StringComparison.OrdinalIgnoreCase)) // TTKT is used for Assurance
            {
                return true;
            }
        }
        
        return false;
    }

    private static string NormalizeServiceId(string? s) =>
        string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim().ToUpperInvariant();

    private static List<string> DeserializeUnmatchedMaterialNames(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <inheritdoc />
    public async Task<List<UnmatchedMaterialReviewItemDto>> GetRecentUnmatchedMaterialNamesAsync(Guid companyId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-90);
        var drafts = await _context.ParsedOrderDrafts
            .Where(d => d.CompanyId == companyId && d.UnmatchedMaterialNamesJson != null && d.UpdatedAt >= since)
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new { d.UnmatchedMaterialNamesJson, d.UpdatedAt })
            .Take(500)
            .ToListAsync(cancellationToken);

        var agg = new Dictionary<string, (int count, string example, DateTime lastSeen)>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in drafts)
        {
            var names = DeserializeUnmatchedMaterialNames(d.UnmatchedMaterialNamesJson);
            var at = d.UpdatedAt;
            foreach (var name in names)
            {
                var normalized = MaterialNameNormalizer.Normalize(name);
                if (string.IsNullOrEmpty(normalized)) continue;
                if (agg.TryGetValue(normalized, out var existing))
                    agg[normalized] = (existing.count + 1, existing.example, existing.lastSeen > at ? existing.lastSeen : at);
                else
                    agg[normalized] = (1, name.Trim(), at);
            }
        }

        return agg
            .OrderByDescending(x => x.Value.count)
            .Take(Math.Min(limit, 100))
            .Select(x => new UnmatchedMaterialReviewItemDto
            {
                NormalizedName = x.Key,
                ExampleOriginal = x.Value.example,
                Frequency = x.Value.count,
                LastSeenAt = x.Value.lastSeen
            })
            .ToList();
    }

    /// <summary>
    /// Builds a map of normalized ServiceId -> existing OrderId for duplicate warning on drafts.
    /// </summary>
    private async Task<Dictionary<string, Guid>> GetExistingOrderIdsMapAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Where(o => o.CompanyId == companyId && o.ServiceId != null)
            .Select(o => new { o.Id, o.ServiceId })
            .Take(50_000)
            .ToListAsync(cancellationToken);
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in orders)
        {
            if (string.IsNullOrWhiteSpace(o.ServiceId)) continue;
            var key = NormalizeServiceId(o.ServiceId);
            if (!map.ContainsKey(key))
                map[key] = o.Id;
        }
        return map;
    }
}

