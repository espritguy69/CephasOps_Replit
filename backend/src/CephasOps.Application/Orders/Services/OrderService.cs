using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.Workflow.Services;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Events;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.Common.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Order service implementation
/// </summary>
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IBuildingService _buildingService;
    private readonly IBlockerValidationService _blockerValidationService;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly IWorkflowDefinitionsService _workflowDefinitionsService;
    private readonly ISlaProfileService _slaProfileService;
    private readonly IAutomationRuleService _automationRuleService;
    private readonly IBusinessHoursService _businessHoursService;
    private readonly IEscalationRuleService _escalationRuleService;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IOrderTypeService _orderTypeService;
    private readonly INotificationService _notificationService;
    private readonly IEncryptionService _encryptionService;
    private readonly IMaterialTemplateService _materialTemplateService;
    private readonly IInventoryService _inventoryService;
    private readonly IEffectiveScopeResolver _effectiveScopeResolver;
    private readonly IOrderPayoutSnapshotService _orderPayoutSnapshotService;
    private readonly IEventBus? _eventBus;
    private readonly ITenantUsageService? _tenantUsageService;

    public OrderService(
        ApplicationDbContext context,
        ILogger<OrderService> logger,
        IBuildingService buildingService,
        IBlockerValidationService blockerValidationService,
        IWorkflowEngineService workflowEngineService,
        IWorkflowDefinitionsService workflowDefinitionsService,
        ISlaProfileService slaProfileService,
        IAutomationRuleService automationRuleService,
        IBusinessHoursService businessHoursService,
        IEscalationRuleService escalationRuleService,
        IApprovalWorkflowService approvalWorkflowService,
        IOrderTypeService orderTypeService,
        INotificationService notificationService,
        IEncryptionService encryptionService,
        IMaterialTemplateService materialTemplateService,
        IInventoryService inventoryService,
        IEffectiveScopeResolver effectiveScopeResolver,
        IOrderPayoutSnapshotService orderPayoutSnapshotService,
        IEventBus? eventBus = null,
        ITenantUsageService? tenantUsageService = null)
    {
        _context = context;
        _logger = logger;
        _buildingService = buildingService;
        _orderPayoutSnapshotService = orderPayoutSnapshotService;
        _tenantUsageService = tenantUsageService;
        _blockerValidationService = blockerValidationService;
        _workflowEngineService = workflowEngineService;
        _workflowDefinitionsService = workflowDefinitionsService;
        _slaProfileService = slaProfileService;
        _automationRuleService = automationRuleService;
        _businessHoursService = businessHoursService;
        _escalationRuleService = escalationRuleService;
        _approvalWorkflowService = approvalWorkflowService;
        _orderTypeService = orderTypeService;
        _notificationService = notificationService;
        _encryptionService = encryptionService;
        _materialTemplateService = materialTemplateService;
        _inventoryService = inventoryService;
        _effectiveScopeResolver = effectiveScopeResolver;
        _eventBus = eventBus;
    }

    public async Task<List<OrderDto>> GetOrdersAsync(
        Guid? companyId,
        Guid? departmentId = null,
        string? status = null,
        Guid? partnerId = null,
        Guid? assignedSiId = null,
        Guid? buildingId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = companyId.HasValue 
            ? _context.Orders.Where(o => o.CompanyId == companyId.Value)
            : _context.Orders.AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(o => o.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(o => o.PartnerId == partnerId.Value);
        }

        if (assignedSiId.HasValue)
        {
            query = query.Where(o => o.AssignedSiId == assignedSiId.Value);
        }

        if (buildingId.HasValue)
        {
            query = query.Where(o => o.BuildingId == buildingId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.AppointmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.AppointmentDate <= toDate.Value);
        }

        query = query
            .Include(o => o.Partner)
            .Include(o => o.OrderCategory)
            .Include(o => o.InstallationMethod);
        var orders = await query.ToListAsync(cancellationToken);

        return orders.Select(o => MapToOrderDto(o, null, o.Partner, o.OrderCategory, o.InstallationMethod)).ToList();
    }

    public async Task<OrderListResultDto> GetOrdersPagedAsync(
        Guid? companyId,
        Guid? departmentId = null,
        string? status = null,
        Guid? partnerId = null,
        Guid? assignedSiId = null,
        Guid? buildingId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? keyword = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        const int maxPageSize = 500;
        if (pageSize <= 0) pageSize = 50;
        if (pageSize > maxPageSize) pageSize = maxPageSize;
        if (page <= 0) page = 1;

        var query = companyId.HasValue
            ? _context.Orders.Where(o => o.CompanyId == companyId.Value)
            : _context.Orders.AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(o => o.DepartmentId == departmentId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
        if (partnerId.HasValue)
            query = query.Where(o => o.PartnerId == partnerId.Value);
        if (assignedSiId.HasValue)
            query = query.Where(o => o.AssignedSiId == assignedSiId.Value);
        if (buildingId.HasValue)
            query = query.Where(o => o.BuildingId == buildingId.Value);
        if (fromDate.HasValue)
            query = query.Where(o => o.AppointmentDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(o => o.AppointmentDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            var termLower = term.ToLower();
            query = query.Where(o =>
                (o.ServiceId != null && o.ServiceId.ToLower().Contains(termLower)) ||
                (o.TicketId != null && o.TicketId.ToLower().Contains(termLower)) ||
                (o.AwoNumber != null && o.AwoNumber.ToLower().Contains(termLower)) ||
                (o.ExternalRef != null && o.ExternalRef.ToLower().Contains(termLower)) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(termLower)) ||
                (o.BuildingName != null && o.BuildingName.ToLower().Contains(termLower)) ||
                (o.AddressLine1 != null && o.AddressLine1.ToLower().Contains(termLower)) ||
                (o.City != null && o.City.ToLower().Contains(termLower)) ||
                (o.Postcode != null && o.Postcode.ToLower().Contains(termLower)) ||
                (o.OrderNotesInternal != null && o.OrderNotesInternal.ToLower().Contains(termLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .Include(o => o.Partner)
            .Include(o => o.OrderCategory)
            .Include(o => o.InstallationMethod)
            .OrderByDescending(o => o.AppointmentDate)
            .ThenBy(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = orders.Select(o => MapToOrderDto(o, null, o.Partner, o.OrderCategory, o.InstallationMethod)).ToList();
        return new OrderListResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid id, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        // When companyId is provided, filter explicitly. When null (e.g. SuperAdmin), global query filter still applies (TenantScope.CurrentTenantId from middleware/X-Company-Id or JWT).
        var query = _context.Orders.Where(o => o.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(o => o.DepartmentId == departmentId.Value);
        }

        var order = await query
            .Include(o => o.Partner)
            .Include(o => o.OrderCategory)
            .Include(o => o.InstallationMethod)
            .FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            return null;
        }

        var draftProjection = await _context.ParsedOrderDrafts
            .Where(d => d.CreatedOrderId == order.Id)
            .Select(d => new { d.ParsedMaterialsJson, d.UnmatchedMaterialCount, d.UnmatchedMaterialNamesJson })
            .FirstOrDefaultAsync(cancellationToken);

        var parsedMaterials = ParsedMaterialsSerializer.Deserialize(draftProjection?.ParsedMaterialsJson);
        int? unmatchedCount = draftProjection?.UnmatchedMaterialCount;
        List<string>? unmatchedNames = null;
        if (!string.IsNullOrEmpty(draftProjection?.UnmatchedMaterialNamesJson))
        {
            try
            {
                unmatchedNames = JsonSerializer.Deserialize<List<string>>(draftProjection.UnmatchedMaterialNamesJson);
            }
            catch
            {
                // ignore malformed json
            }
        }

        return MapToOrderDto(order, parsedMaterials, order.Partner, order.OrderCategory, order.InstallationMethod, unmatchedParsedMaterialCount: unmatchedCount, unmatchedParsedMaterialNames: unmatchedNames);
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, Guid companyId, Guid userId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var orderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Get the OrderType entity to access its DepartmentId (from settings)
        var orderType = await _context.OrderTypes
            .Where(ot => ot.Id == dto.OrderTypeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderType == null)
        {
            throw new InvalidOperationException($"OrderType with ID {dto.OrderTypeId} not found");
        }
        if (!orderType.IsActive)
        {
            throw new InvalidOperationException("Order type cannot be used because it is inactive. Please select an active order type.");
        }

        // Priority: Use OrderType's DepartmentId first (from settings), then explicit departmentId parameter, then Building's department
        Guid? resolvedDepartmentId = null;
        if (orderType.DepartmentId.HasValue)
        {
            resolvedDepartmentId = orderType.DepartmentId;
            _logger.LogInformation(
                "Using DepartmentId from OrderType: {OrderTypeName} ({OrderTypeCode}), DepartmentId: {DepartmentId}", 
                orderType.Name, orderType.Code, resolvedDepartmentId);
        }
        else if (departmentId.HasValue || dto.DepartmentId.HasValue)
        {
            // Use explicit departmentId if provided (from UI or API parameter)
            resolvedDepartmentId = departmentId ?? dto.DepartmentId;
            _logger.LogInformation(
                "Using explicit DepartmentId: {DepartmentId}", 
                resolvedDepartmentId);
        }
        else
        {
            // Fallback to Building's department
            resolvedDepartmentId = await ResolveDepartmentIdAsync(dto.BuildingId, null, requireDepartment: true, cancellationToken);
            if (resolvedDepartmentId.HasValue)
            {
                _logger.LogInformation(
                    "Using DepartmentId from Building: {BuildingId}, DepartmentId: {DepartmentId}", 
                    dto.BuildingId, resolvedDepartmentId);
            }
        }

        if (!resolvedDepartmentId.HasValue)
        {
            _logger.LogWarning(
                "No DepartmentId resolved for manual order creation. OrderType: {OrderTypeName}, BuildingId: {BuildingId}. " +
                "Order will be created without department assignment.", 
                orderType.Name, dto.BuildingId);
        }

        // Create order using EF Core entity (cleaner, type-safe, handles nulls properly for PostgreSQL)
        try
        {
            var order = new Order
            {
                Id = orderId,
                CompanyId = companyId,
                PartnerId = dto.PartnerId,
                SourceSystem = "Manual",
                SourceEmailId = null,
                OrderTypeId = dto.OrderTypeId,
                OrderCategoryId = dto.OrderCategoryId, // Service/technology category (FTTH, FTTO, etc.)
                InstallationMethodId = dto.InstallationMethodId, // Site condition/installation method (Prelaid, Non-Prelaid, etc.)
                ServiceIdType = dto.ServiceIdType,
                ServiceId = dto.ServiceId ?? string.Empty,
                TicketId = dto.TicketId,
                ExternalRef = dto.ExternalRef,
                Status = "Pending",
                StatusReason = null,
                Priority = dto.Priority ?? "Normal",
                BuildingId = dto.BuildingId,
                BuildingName = null, // Can be populated from building lookup if needed
                UnitNo = dto.UnitNo,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                State = dto.State,
                Postcode = dto.Postcode,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                // Relocation fields per ORDERS_MODULE.md section 7
                RelocationType = dto.RelocationType,
                OldAddress = dto.OldAddress,
                OldLocationNote = dto.OldLocationNote,
                NewLocationNote = dto.NewLocationNote,
                PackageName = null,
                Bandwidth = null,
                OnuSerialNumber = null,
                VoipServiceId = dto.VoipServiceId,
                // Network Info fields
                NetworkPackage = dto.NetworkPackage,
                NetworkBandwidth = dto.NetworkBandwidth,
                NetworkLoginId = dto.NetworkLoginId,
                NetworkPassword = dto.NetworkPassword,
                NetworkWanIp = dto.NetworkWanIp,
                NetworkLanIp = dto.NetworkLanIp,
                NetworkGateway = dto.NetworkGateway,
                NetworkSubnetMask = dto.NetworkSubnetMask,
                // VOIP fields
                VoipPassword = dto.VoipPassword,
                VoipIpAddressOnu = dto.VoipIpAddressOnu,
                VoipGatewayOnu = dto.VoipGatewayOnu,
                VoipSubnetMaskOnu = dto.VoipSubnetMaskOnu,
                VoipIpAddressSrp = dto.VoipIpAddressSrp,
                VoipRemarks = dto.VoipRemarks,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerPhone2 = dto.CustomerPhone2, // ✅ Map CustomerPhone2
                CustomerEmail = dto.CustomerEmail,
                Issue = dto.Issue, // ✅ Map Issue for Assurance orders
                Solution = dto.Solution, // ✅ Map Solution for Assurance orders
                OrderNotesInternal = dto.OrderNotesInternal,
                PartnerNotes = dto.PartnerNotes,
                RequestedAppointmentAt = dto.RequestedAppointmentAt,
                AppointmentDate = dto.AppointmentDate,
                AppointmentWindowFrom = dto.AppointmentWindowFrom,
                AppointmentWindowTo = dto.AppointmentWindowTo,
                AssignedSiId = null,
                AssignedTeamId = null,
                KpiCategory = null,
                KpiDueAt = null,
                KpiBreachedAt = null,
                HasReschedules = false,
                RescheduleCount = 0,
                DocketUploaded = false,
                PhotosUploaded = false,
                SerialsValidated = false,
                InvoiceEligible = false,
                InvoiceId = null,
                PayrollPeriodId = null,
                PnlPeriod = null,
                CreatedByUserId = userId,
                CreatedAt = now,
                UpdatedAt = now,
                CancelledAt = null,
                CancelledByUserId = null,
                DepartmentId = resolvedDepartmentId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order created: {OrderId}, Company: {CompanyId}", orderId, companyId);

            if (_tenantUsageService != null)
                await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.OrdersCreated, 1, cancellationToken);

            if (_eventBus != null)
            {
                var evt = new OrderCreatedEvent
                {
                    EventId = Guid.NewGuid(),
                    OccurredAtUtc = now,
                    CompanyId = companyId,
                    TriggeredByUserId = userId,
                    OrderId = orderId,
                    PartnerId = dto.PartnerId,
                    BuildingId = dto.BuildingId,
                    SourceSystem = "Manual"
                };
                await _eventBus.PublishAsync(evt, cancellationToken);
            }

            // Auto-apply default materials from building + job type, then material templates
            await ApplyDefaultMaterialsAsync(orderId, dto.BuildingId, dto.OrderTypeId, dto.PartnerId, companyId, userId, cancellationToken);

            return await GetOrderByIdAsync(orderId, companyId, resolvedDepartmentId, cancellationToken) 
                ?? throw new InvalidOperationException("Failed to retrieve created order");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order: {OrderId}", orderId);
            throw new InvalidOperationException($"Failed to create order: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Auto-apply default materials following priority:
    /// 1. Building default materials (by BuildingId + OrderTypeId)
    /// 2. Material templates (by CompanyId + PartnerId + OrderType + BuildingTypeId)
    /// 3. Manual selection (always available)
    /// 
    /// Note: "Order Type" refers to the type of order (Activation, Modification, etc.), not background job types.
    /// </summary>
    private async Task ApplyDefaultMaterialsAsync(
        Guid orderId, 
        Guid buildingId, 
        Guid orderTypeId,
        Guid? partnerId,
        Guid companyId, 
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var materialsApplied = 0;

            // Priority 1: Get default materials for this building + order type
            var defaultMaterials = await _context.BuildingDefaultMaterials
                .Where(m => m.BuildingId == buildingId && m.OrderTypeId == orderTypeId && m.IsActive)
                .ToListAsync(cancellationToken);

            if (defaultMaterials.Count > 0)
            {
                // Get material details for cost lookup
                var materialIds = defaultMaterials.Select(m => m.MaterialId).ToList();
                var materials = await _context.Materials
                    .Where(m => materialIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, cancellationToken);

                // Create OrderMaterialUsage records
                foreach (var defaultMaterial in defaultMaterials)
                {
                    var material = materials.GetValueOrDefault(defaultMaterial.MaterialId);
                    var unitCost = material?.DefaultCost ?? 0;

                    var usage = new OrderMaterialUsage
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        OrderId = orderId,
                        MaterialId = defaultMaterial.MaterialId,
                        Quantity = defaultMaterial.DefaultQuantity,
                        UnitCost = unitCost,
                        TotalCost = unitCost * defaultMaterial.DefaultQuantity,
                        Notes = $"Auto-applied from building defaults ({defaultMaterial.Notes ?? ""})",
                        RecordedByUserId = userId,
                        RecordedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.OrderMaterialUsage.Add(usage);
                    materialsApplied++;
                }

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Applied {Count} building default materials to order {OrderId}", 
                    defaultMaterials.Count, orderId);
            }
            else
            {
                _logger.LogInformation("No building default materials found for building {BuildingId} and order type {OrderTypeId}. Checking material templates.", 
                    buildingId, orderTypeId);

                // Priority 2: Try material templates if no building defaults
                var orderType = await _context.OrderTypes
                    .Where(ot => ot.Id == orderTypeId)
                    .Select(ot => ot.Code)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!string.IsNullOrEmpty(orderType))
                {
                    // Get building type ID from building
                    var buildingTypeId = await _context.Buildings
                        .Where(b => b.Id == buildingId)
                        .Select(b => (Guid?)null) // BuildingTypeId is obsolete - use PropertyType enum instead
                        .FirstOrDefaultAsync(cancellationToken);

                    var installationMethodId = await _context.Orders
                        .Where(o => o.Id == orderId)
                        .Select(o => o.InstallationMethodId)
                        .FirstOrDefaultAsync(cancellationToken);

                    var template = await _materialTemplateService.GetEffectiveTemplateAsync(
                        companyId, partnerId, orderType, installationMethodId, buildingTypeId, cancellationToken);

                    if (template != null && template.Items != null && template.Items.Count > 0)
                    {
                        // Get material details for cost lookup
                        var templateMaterialIds = template.Items.Select(i => i.MaterialId).ToList();
                        var materials = await _context.Materials
                            .Where(m => templateMaterialIds.Contains(m.Id))
                            .ToDictionaryAsync(m => m.Id, cancellationToken);

                        // Create OrderMaterialUsage records from template
                        foreach (var templateItem in template.Items)
                        {
                            var material = materials.GetValueOrDefault(templateItem.MaterialId);
                            var unitCost = material?.DefaultCost ?? 0;

                            var usage = new OrderMaterialUsage
                            {
                                Id = Guid.NewGuid(),
                                CompanyId = companyId,
                                OrderId = orderId,
                                MaterialId = templateItem.MaterialId,
                                Quantity = templateItem.Quantity,
                                UnitCost = unitCost,
                                TotalCost = unitCost * templateItem.Quantity,
                                Notes = $"Auto-applied from material template '{template.Name}' ({templateItem.Notes ?? ""})",
                                RecordedByUserId = userId,
                                RecordedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.OrderMaterialUsage.Add(usage);
                            materialsApplied++;
                        }

                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Applied {Count} materials from template '{TemplateName}' to order {OrderId}", 
                            template.Items.Count, template.Name, orderId);
                    }
                    else
                    {
                        _logger.LogInformation("No material template found for order type {OrderType}, partner {PartnerId}, building type {BuildingTypeId}", 
                            orderType, partnerId, buildingTypeId);
                    }
                }
            }

            if (materialsApplied == 0)
            {
                _logger.LogInformation("No default materials or templates found for order {OrderId}. Materials must be selected manually.", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply default materials to order {OrderId}", orderId);
            // Don't throw - allow order creation to succeed even if materials fail
        }
    }

    /// <summary>
    /// Resolve a parsed material name to an existing Material (company-scoped).
    /// Order: (1) ItemCode exact normalized match, (2) Description exact normalized match, (3) ParsedMaterialAlias match.
    /// Uses shared MaterialNameNormalizer for consistency. Returns null if no match.
    /// </summary>
    private async Task<Material?> ResolveParsedMaterialToMaterialAsync(string normalizedName, Guid companyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(normalizedName)) return null;
        var materials = await _context.Materials
            .Where(m => m.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        // Layer 1: ItemCode exact normalized match
        var byItemCode = materials.FirstOrDefault(m =>
            string.Equals(MaterialNameNormalizer.Normalize(m.ItemCode), normalizedName, StringComparison.OrdinalIgnoreCase));
        if (byItemCode != null) return byItemCode;
        // Layer 2: Description exact normalized match
        var byDescription = materials.FirstOrDefault(m =>
            string.Equals(MaterialNameNormalizer.Normalize(m.Description), normalizedName, StringComparison.OrdinalIgnoreCase));
        if (byDescription != null) return byDescription;
        // Layer 3: Alias (learn-once reuse; company-scoped, active only)
        var alias = await _context.ParsedMaterialAliases
            .Where(a => a.CompanyId == companyId && a.IsActive && !a.IsDeleted
                && string.Equals(a.NormalizedAliasText, normalizedName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefaultAsync(cancellationToken);
        if (alias != null)
        {
            var material = materials.FirstOrDefault(m => m.Id == alias.MaterialId);
            if (material != null) return material;
            var loaded = await _context.Materials.FirstOrDefaultAsync(m => m.Id == alias.MaterialId && m.CompanyId == companyId, cancellationToken);
            return loaded;
        }
        return null;
    }

    /// <summary>
    /// Apply parsed draft materials to the order as OrderMaterialUsage.
    /// Only creates usage for materials that resolve to an existing Material (ItemCode or Description match).
    /// Skips materials that would duplicate an existing OrderMaterialUsage for the same MaterialId (e.g. from defaults).
    /// Unmatched parsed materials are logged and not created. Returns the list of unmatched names for audit.
    /// </summary>
    private async Task<List<string>> ApplyParsedMaterialsAsync(
        Guid orderId,
        Guid companyId,
        Guid userId,
        List<ParsedDraftMaterialDto> parsedMaterials,
        CancellationToken cancellationToken)
    {
        if (parsedMaterials == null || parsedMaterials.Count == 0) return new List<string>();
        var now = DateTime.UtcNow;
        var existingUsageMaterialIds = await _context.OrderMaterialUsage
            .Where(u => u.OrderId == orderId)
            .Select(u => u.MaterialId)
            .ToListAsync(cancellationToken);
        var existingSet = existingUsageMaterialIds.ToHashSet();
        var applied = 0;
        var unmatched = new List<string>();

        foreach (var pm in parsedMaterials)
        {
            var normalizedName = MaterialNameNormalizer.Normalize(pm.Name);
            if (string.IsNullOrEmpty(normalizedName))
            {
                unmatched.Add("(empty or whitespace)");
                continue;
            }

            var material = await ResolveParsedMaterialToMaterialAsync(normalizedName, companyId, cancellationToken);
            if (material == null)
            {
                unmatched.Add(pm.Name ?? "(no name)");
                continue;
            }
            if (existingSet.Contains(material.Id))
            {
                // Already have this material (e.g. from building defaults); skip to avoid duplicate row
                continue;
            }

            var quantity = pm.Quantity ?? 1;
            if (quantity <= 0) quantity = 1;
            var unitCost = material.DefaultCost ?? 0;
            var notesParts = new List<string> { "From parser" };
            if (!string.IsNullOrWhiteSpace(pm.UnitOfMeasure)) notesParts.Add($"Unit: {pm.UnitOfMeasure}");
            if (!string.IsNullOrWhiteSpace(pm.Notes)) notesParts.Add(pm.Notes);
            var notes = string.Join("; ", notesParts);

            var usage = new OrderMaterialUsage
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                OrderId = orderId,
                MaterialId = material.Id,
                Quantity = quantity,
                UnitCost = unitCost,
                TotalCost = unitCost * quantity,
                Notes = notes,
                RecordedByUserId = userId,
                RecordedAt = now,
                CreatedAt = now
            };
            _context.OrderMaterialUsage.Add(usage);
            existingSet.Add(material.Id);
            applied++;
        }

        if (applied > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Applied {Count} parsed materials to order {OrderId} (resolved from draft).",
                applied, orderId);
        }
        if (unmatched.Count > 0)
        {
            _logger.LogWarning(
                "Parsed materials not resolved to Material (ItemCode/Description/alias match for company {CompanyId}); order creation succeeded. Unmatched: {Unmatched}",
                companyId, string.Join(", ", unmatched));
        }
        return unmatched;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUnmatchedParsedMaterialNamesAsync(Guid companyId, List<ParsedDraftMaterialDto> materials, CancellationToken cancellationToken = default)
    {
        if (materials == null || materials.Count == 0) return new List<string>();
        var unmatched = new List<string>();
        foreach (var pm in materials)
        {
            var normalizedName = MaterialNameNormalizer.Normalize(pm.Name);
            if (string.IsNullOrEmpty(normalizedName))
            {
                unmatched.Add("(empty or whitespace)");
                continue;
            }
            var material = await ResolveParsedMaterialToMaterialAsync(normalizedName, companyId, cancellationToken);
            if (material == null)
                unmatched.Add(pm.Name ?? "(no name)");
        }
        return unmatched;
    }

    private async Task<Guid?> ResolveDepartmentIdAsync(
        Guid buildingId,
        Guid? requestedDepartmentId,
        bool requireDepartment,
        CancellationToken cancellationToken)
    {
        var buildingDepartmentId = await _context.Buildings
            .Where(b => b.Id == buildingId)
            .Select(b => b.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (requestedDepartmentId.HasValue && buildingDepartmentId.HasValue &&
            requestedDepartmentId.Value != buildingDepartmentId.Value)
        {
            throw new InvalidOperationException("Building belongs to a different department.");
        }

        var finalDepartmentId = requestedDepartmentId ?? buildingDepartmentId;

        if (requireDepartment && !finalDepartmentId.HasValue)
        {
            throw new InvalidOperationException("Department assignment is required for this operation.");
        }

        return finalDepartmentId;
    }

    public async Task<OrderDto> UpdateOrderAsync(Guid id, UpdateOrderDto dto, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        // Load entity directly to avoid raw SQL string building issues
        var query = _context.Orders.Where(o => o.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        var orderEntity = await query.FirstOrDefaultAsync(cancellationToken);
        if (orderEntity == null)
        {
            throw new KeyNotFoundException($"Order with ID {id} not found");
        }

        if (departmentId.HasValue && orderEntity.DepartmentId != departmentId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this order");
        }

        if (dto.TicketId != null) orderEntity.TicketId = dto.TicketId;
        if (dto.ExternalRef != null) orderEntity.ExternalRef = dto.ExternalRef;
        if (dto.Priority != null) orderEntity.Priority = dto.Priority;
        if (dto.UnitNo != null) orderEntity.UnitNo = dto.UnitNo;
        if (dto.AddressLine1 != null) orderEntity.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 != null) orderEntity.AddressLine2 = dto.AddressLine2;
        if (dto.City != null) orderEntity.City = dto.City;
        if (dto.State != null) orderEntity.State = dto.State;
        if (dto.Postcode != null) orderEntity.Postcode = dto.Postcode;
        if (dto.Latitude.HasValue) orderEntity.Latitude = dto.Latitude.Value;
        if (dto.Longitude.HasValue) orderEntity.Longitude = dto.Longitude.Value;
        if (dto.CustomerName != null) orderEntity.CustomerName = dto.CustomerName;
        if (dto.CustomerPhone != null) orderEntity.CustomerPhone = dto.CustomerPhone;
        if (dto.CustomerEmail != null) orderEntity.CustomerEmail = dto.CustomerEmail;
        if (dto.OrderNotesInternal != null) orderEntity.OrderNotesInternal = dto.OrderNotesInternal;
        if (dto.PartnerNotes != null) orderEntity.PartnerNotes = dto.PartnerNotes;
        if (dto.RequestedAppointmentAt.HasValue) orderEntity.RequestedAppointmentAt = dto.RequestedAppointmentAt.Value;
        if (dto.AppointmentDate.HasValue) orderEntity.AppointmentDate = dto.AppointmentDate.Value;
        if (dto.AppointmentWindowFrom.HasValue) orderEntity.AppointmentWindowFrom = dto.AppointmentWindowFrom.Value;
        if (dto.AppointmentWindowTo.HasValue) orderEntity.AppointmentWindowTo = dto.AppointmentWindowTo.Value;

        orderEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order updated: {OrderId}", id);

        return MapToOrderDto(orderEntity);
    }

    public async Task DeleteOrderAsync(Guid id, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        TenantSafetyGuard.AssertTenantContext();
        // Use IgnoreQueryFilters to find even soft-deleted orders; always constrain by company (explicit or current tenant).
        var query = _context.Orders.IgnoreQueryFilters().Where(o => o.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }
        else
        {
            query = query.Where(o => o.CompanyId == TenantScope.CurrentTenantId!.Value);
        }

        var orderEntity = await query.FirstOrDefaultAsync(cancellationToken);
        if (orderEntity == null)
        {
            throw new KeyNotFoundException($"Order with ID {id} not found");
        }

        if (departmentId.HasValue && orderEntity.DepartmentId != departmentId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this order");
        }

        // Soft delete - set IsDeleted flag instead of hard delete
        orderEntity.IsDeleted = true;
        orderEntity.DeletedAt = DateTime.UtcNow;
        orderEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order soft deleted: {OrderId}", id);
    }

    public async Task<OrderDto> ChangeOrderStatusAsync(Guid id, ChangeOrderStatusDto dto, Guid? companyId, Guid? departmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetOrderByIdAsync(id, companyId, departmentId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Order with ID {id} not found");
        }

        var orderEntity = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        
        if (orderEntity == null)
        {
            throw new KeyNotFoundException($"Order with ID {id} not found");
        }

        var oldStatus = existing.Status;
        var orderCompanyId = orderEntity.CompanyId ?? throw new InvalidOperationException("Order must have a CompanyId");

        // Validate status transition
        if (!OrderStatus.IsValid(dto.Status))
        {
            throw new InvalidOperationException($"Invalid status '{dto.Status}'. Valid statuses: {string.Join(", ", OrderStatus.AllStatuses)}");
        }

        // Special validation for Blocker status (uses BlockerValidationService - separate from workflow)
        // This is business logic that should remain in OrderService
        if (dto.Status == OrderStatus.Blocker)
        {
            var blockerReason = dto.Reason;
            var blockerCategory = dto.Metadata?.GetValueOrDefault("blockerCategory")?.ToString();

            if (string.IsNullOrEmpty(blockerReason))
            {
                throw new InvalidOperationException("Blocker reason is required when setting status to Blocker");
            }

            var validationResult = _blockerValidationService.ValidateBlockerTransition(
                oldStatus, blockerReason, blockerCategory);

            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors);
                _logger.LogWarning(
                    "Blocker validation failed for Order {OrderId}: {Errors}. Context: {Context}, Allowed reasons: {AllowedReasons}",
                    id, errorMessage, validationResult.BlockerContext, 
                    string.Join(", ", validationResult.AllowedReasons));
                
                throw new InvalidOperationException(
                    $"Blocker validation failed: {errorMessage}");
            }

            _logger.LogInformation(
                "Blocker validation passed for Order {OrderId}. Context: {Context}, Reason: {Reason}",
                id, validationResult.BlockerContext, blockerReason);
        }

        // Reschedule: require explicit reason for operational accountability
        if (dto.Status == OrderStatus.ReschedulePendingApproval && string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException(
                "Reschedule reason is required when setting status to ReschedulePendingApproval. Provide a reason (e.g. customer request, building issue) for auditability.");
        }

        // Resolve order type code for workflow via shared resolver (parent code when subtype, else own code)
        var orderTypeCode = await _effectiveScopeResolver.GetOrderTypeCodeForScopeAsync(orderEntity.OrderTypeId, cancellationToken);

        // Get effective workflow definition (priority: Partner → Department → OrderType → General)
        var workflowDefinition = await _workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(
            orderCompanyId,
            "Order",
            orderEntity.PartnerId,
            orderEntity.DepartmentId,
            orderTypeCode,
            cancellationToken);

        if (workflowDefinition == null)
        {
            throw new InvalidOperationException(
                "No active workflow definition found for Order entity. Please configure workflow definitions first.");
        }

        // Prepare workflow execution payload (pass full context so engine uses same workflow as lookup)
        var executeDto = new ExecuteTransitionDto
        {
            EntityType = "Order",
            EntityId = id,
            TargetStatus = dto.Status,
            PartnerId = orderEntity.PartnerId,
            DepartmentId = orderEntity.DepartmentId,
            OrderTypeCode = orderTypeCode,
            Payload = new Dictionary<string, object>
            {
                ["reason"] = dto.Reason ?? string.Empty,
                ["userId"] = userId.ToString(),
                ["source"] = "AdminPortal",
                ["metadata"] = dto.Metadata ?? new Dictionary<string, object>()
            }
        };

        // Execute transition through workflow engine
        // This will:
        // 1. Validate guard conditions (from settings - splitter, docket, photos, etc.)
        // 2. Execute side effects (from settings - notifications, stock movements, status logs)
        // 3. Update entity status
        // 4. Create WorkflowJob record for audit
        var workflowJob = await _workflowEngineService.ExecuteTransitionAsync(
            orderCompanyId,
            executeDto,
            userId,
            cancellationToken);

        if (workflowJob.State != "Succeeded")
        {
            throw new InvalidOperationException(
                $"Workflow transition failed: {workflowJob.LastError ?? "Unknown error"}");
        }

        _logger.LogInformation(
            "Order status changed via workflow engine: {OrderId}, From: {OldStatus}, To: {NewStatus}, JobId: {JobId}",
            id, oldStatus, dto.Status, workflowJob.Id);

        // After successful status change, apply integrations
        try
        {
            // 1. Calculate and track SLA
            await CalculateAndTrackSlaAsync(orderEntity, dto.Status, cancellationToken);

            // 2. Execute automation rules (may set AssignedSiId via auto-assign rules)
            await ExecuteAutomationRulesAsync(orderEntity, oldStatus, dto.Status, cancellationToken);

            // Reload order entity to get updated AssignedSiId if it was set by automation rules
            await _context.Entry(orderEntity).ReloadAsync(cancellationToken);

            // 3. Create delivery order if order is assigned to SI (only when status changes to Assigned)
            if (dto.Status == "Assigned" && orderEntity.AssignedSiId.HasValue && oldStatus != "Assigned")
            {
                await CreateDeliveryOrderForAssignedOrderAsync(orderEntity, userId, cancellationToken);
            }

            // 4. Check escalation rules
            await CheckEscalationRulesAsync(orderEntity, cancellationToken);

            // 5. Create immutable payout snapshot when order/job reaches completed state
            if (dto.Status == OrderStatus.OrderCompleted || dto.Status == OrderStatus.Completed)
            {
                try
                {
                    await _orderPayoutSnapshotService.CreateSnapshotForOrderIfEligibleAsync(id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create payout snapshot for order {OrderId}", id);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the status change if integrations fail
            _logger.LogWarning(ex, "Error executing integrations for order {OrderId} status change", id);
        }

        // Return updated order
        return await GetOrderByIdAsync(id, companyId, departmentId, cancellationToken) 
            ?? existing;
    }

    public async Task<CreateOrderFromDraftResult> CreateFromParsedDraftAsync(CreateOrderFromDraftDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        // Validate mandatory fields
        var validationErrors = ValidateDraftForOrderCreation(dto);
        if (validationErrors.Count > 0)
        {
            return CreateOrderFromDraftResult.Failed("Validation failed", validationErrors);
        }

        // Check for duplicates
        var isAssurance = IsAssuranceOrder(dto.OrderTypeHint);
        if (isAssurance && !string.IsNullOrEmpty(dto.ServiceId) && !string.IsNullOrEmpty(dto.TicketId))
        {
            var existsByTicket = await ExistsByServiceIdAndTicketIdAsync(dto.ServiceId, dto.TicketId, dto.CompanyId, cancellationToken);
            if (existsByTicket)
            {
                return CreateOrderFromDraftResult.Failed($"Order with ServiceId '{dto.ServiceId}' and TicketId '{dto.TicketId}' already exists");
            }
        }
        else if (!string.IsNullOrEmpty(dto.ServiceId))
        {
            var existsByService = await ExistsByServiceIdAsync(dto.ServiceId, dto.CompanyId, cancellationToken);
            if (existsByService)
            {
                return CreateOrderFromDraftResult.Failed($"Order with ServiceId '{dto.ServiceId}' already exists");
            }
        }

        // Resolve PartnerId (required FK). Use draft value or first active partner for company.
        var resolvedPartnerId = (dto.PartnerId.HasValue && dto.PartnerId.Value != Guid.Empty)
            ? dto.PartnerId.Value
            : await GetFirstActivePartnerIdForCompanyAsync(dto.CompanyId, cancellationToken);
        if (resolvedPartnerId == Guid.Empty)
        {
            return CreateOrderFromDraftResult.Failed(
                "Partner is required for order creation. Set Partner on the parsed order draft or ensure at least one active Partner exists for the company.");
        }

        // Parse address components
        var addressComponents = AddressParser.ParseAddress(dto.AddressText);

        // If BuildingId is not provided but building name is detected, try to find the building
        Guid? resolvedBuildingId = dto.BuildingId;
        if (!resolvedBuildingId.HasValue && !string.IsNullOrWhiteSpace(addressComponents.BuildingName))
        {
            _logger.LogInformation("Building name detected but BuildingId not provided. Searching for building: {BuildingName}", 
                addressComponents.BuildingName);
            
            var buildingLookup = await _buildingService.FindBuildingByAddressAsync(
                addressComponents.BuildingName,
                addressComponents.AddressLine1,
                addressComponents.City,
                addressComponents.State,
                addressComponents.Postcode,
                dto.CompanyId,
                cancellationToken);

            if (buildingLookup.Found && buildingLookup.Building != null)
            {
                resolvedBuildingId = buildingLookup.Building.Id;
                _logger.LogInformation("Building found: {BuildingId}, Name: {BuildingName}", 
                    resolvedBuildingId, buildingLookup.Building.Name);
            }
            else
            {
                // Building not found - return result indicating building detection is needed
                _logger.LogWarning("Building not found for detected name: {BuildingName}. Returning building detection result.", 
                    addressComponents.BuildingName);
                
                var buildingDetection = new BuildingDetectionResult
                {
                    DetectedBuildingName = addressComponents.BuildingName,
                    DetectedAddress = addressComponents.AddressLine1,
                    DetectedCity = addressComponents.City,
                    DetectedState = addressComponents.State,
                    DetectedPostcode = addressComponents.Postcode,
                    MatchedBuilding = buildingLookup.Building,
                    SimilarBuildings = buildingLookup.SimilarBuildings
                };
                
                return CreateOrderFromDraftResult.RequiresBuilding(buildingDetection);
            }
        }

        // Normalize phone number
        var normalizedPhone = PhoneNumberUtility.NormalizePhoneNumber(dto.CustomerPhone);

        // Parse appointment window - provide default if missing
        TimeSpan windowFrom, windowTo;
        var appointmentWindow = dto.AppointmentWindow;
        if (string.IsNullOrWhiteSpace(appointmentWindow))
        {
            // Default to 9 AM - 5 PM if not provided
            appointmentWindow = "09:00-17:00";
            _logger.LogWarning("AppointmentWindow missing for draft {DraftId}, using default: {DefaultWindow}", 
                dto.ParsedOrderDraftId, appointmentWindow);
        }
        
        if (!AppointmentWindowParser.TryParseAppointmentWindow(appointmentWindow, out windowFrom, out windowTo))
        {
            return CreateOrderFromDraftResult.Failed($"Invalid appointment window format: '{appointmentWindow}'");
        }

        // Use default appointment date if missing (tomorrow)
        var appointmentDate = dto.AppointmentDate ?? DateTime.UtcNow.Date.AddDays(1);
        if (!dto.AppointmentDate.HasValue)
        {
            _logger.LogWarning("AppointmentDate missing for draft {DraftId}, using default: {DefaultDate}", 
                dto.ParsedOrderDraftId, appointmentDate);
        }

        // Resolve OrderTypeId from hint if not provided
        var orderTypeId = dto.OrderTypeId ?? await ResolveOrderTypeIdAsync(dto.OrderTypeHint, cancellationToken);
        if (orderTypeId == Guid.Empty)
        {
            return CreateOrderFromDraftResult.Failed($"Unable to resolve OrderType from hint: '{dto.OrderTypeHint}'");
        }

        // Get the OrderType entity to access its DepartmentId (from settings)
        var orderType = await _context.OrderTypes
            .Where(ot => ot.Id == orderTypeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderType == null)
        {
            return CreateOrderFromDraftResult.Failed($"OrderType with ID {orderTypeId} not found");
        }
        if (!orderType.IsActive)
        {
            return CreateOrderFromDraftResult.Failed("Order type cannot be used because it is inactive.");
        }

        // Priority: Use OrderType's DepartmentId first (from settings), then fallback to Building's department
        Guid? resolvedDepartmentId = null;
        if (orderType.DepartmentId.HasValue)
        {
            resolvedDepartmentId = orderType.DepartmentId;
            _logger.LogInformation(
                "Using DepartmentId from OrderType: {OrderTypeName} ({OrderTypeCode}), DepartmentId: {DepartmentId}", 
                orderType.Name, orderType.Code, resolvedDepartmentId);
        }
        else if (resolvedBuildingId.HasValue)
        {
            resolvedDepartmentId = await ResolveDepartmentIdAsync(resolvedBuildingId.Value, null, requireDepartment: false, cancellationToken);
            if (resolvedDepartmentId.HasValue)
            {
                _logger.LogInformation(
                    "Using DepartmentId from Building: {BuildingId}, DepartmentId: {DepartmentId}", 
                    resolvedBuildingId.Value, resolvedDepartmentId);
            }
        }

        // Note: If both OrderType and Building don't have departments, resolvedDepartmentId will be null
        // This is acceptable per cursor rules (single company, multi-department mode - department is optional)
        if (!resolvedDepartmentId.HasValue)
        {
            _logger.LogWarning(
                "No DepartmentId resolved for order from draft. OrderType: {OrderTypeName}, BuildingId: {BuildingId}. " +
                "Order will be created without department assignment.", 
                orderType.Name, dto.BuildingId);
        }

        // Resolve OrderCategoryId (required for payroll and rate resolution; must never be Guid.Empty)
        Guid resolvedOrderCategoryId;
        if (dto.OrderCategoryId.HasValue && dto.OrderCategoryId.Value != Guid.Empty)
        {
            var categoryExists = await _context.OrderCategories
                .AnyAsync(oc => oc.Id == dto.OrderCategoryId.Value && oc.IsActive && (oc.CompanyId == (dto.CompanyId ?? Guid.Empty) || dto.CompanyId == null), cancellationToken);
            if (categoryExists)
            {
                resolvedOrderCategoryId = dto.OrderCategoryId.Value;
                _logger.LogDebug("Using OrderCategoryId from draft: {OrderCategoryId}", resolvedOrderCategoryId);
            }
            else
            {
                _logger.LogWarning("Draft OrderCategoryId {OrderCategoryId} not found or inactive; resolving default.", dto.OrderCategoryId.Value);
                resolvedOrderCategoryId = Guid.Empty;
            }
        }
        else
        {
            resolvedOrderCategoryId = Guid.Empty;
        }

        if (resolvedOrderCategoryId == Guid.Empty)
        {
            var defaultCategory = await _context.OrderCategories
                .Where(oc => oc.CompanyId == (dto.CompanyId ?? Guid.Empty) && oc.IsActive)
                .OrderBy(oc => oc.DisplayOrder)
                .ThenBy(oc => oc.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (defaultCategory != null)
            {
                resolvedOrderCategoryId = defaultCategory.Id;
                _logger.LogInformation("Using default OrderCategory for company: {OrderCategoryCode} ({OrderCategoryId})", defaultCategory.Code, resolvedOrderCategoryId);
            }
        }

        if (resolvedOrderCategoryId == Guid.Empty)
        {
            _logger.LogWarning("Order category could not be resolved for draft {DraftId}. Order category is required for payroll and rate resolution.", dto.ParsedOrderDraftId);
            return CreateOrderFromDraftResult.Failed("Order category must be set before order creation. Please set OrderCategoryId on the draft or ensure at least one active Order Category exists for the company.");
        }

        // Resolve InstallationMethodId (optional; from dto or building)
        Guid? resolvedInstallationMethodId = null;
        if (dto.InstallationMethodId.HasValue && dto.InstallationMethodId.Value != Guid.Empty)
        {
            var methodExists = await _context.InstallationMethods
                .AnyAsync(im => im.Id == dto.InstallationMethodId.Value && im.IsActive, cancellationToken);
            if (methodExists)
            {
                resolvedInstallationMethodId = dto.InstallationMethodId.Value;
                _logger.LogDebug("Using InstallationMethodId from draft: {InstallationMethodId}", resolvedInstallationMethodId);
            }
        }

        if (!resolvedInstallationMethodId.HasValue && resolvedBuildingId.HasValue)
        {
            var building = await _context.Buildings
                .AsNoTracking()
                .Where(b => b.Id == resolvedBuildingId.Value)
                .Select(b => new { b.InstallationMethodId })
                .FirstOrDefaultAsync(cancellationToken);
            if (building?.InstallationMethodId != null && building.InstallationMethodId != Guid.Empty)
            {
                resolvedInstallationMethodId = building.InstallationMethodId;
                _logger.LogInformation("Using InstallationMethodId from building: {InstallationMethodId}", resolvedInstallationMethodId);
            }
        }

        if (!resolvedInstallationMethodId.HasValue)
        {
            _logger.LogWarning("Installation method could not be resolved for draft {DraftId}. Payroll and rate resolution may use fallback rates.", dto.ParsedOrderDraftId);
        }

        // Create the order using EF Core entity (handles nulls properly for PostgreSQL)
        var orderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        try
        {
            var order = new Order
            {
                Id = orderId,
                CompanyId = dto.CompanyId,
                PartnerId = resolvedPartnerId,
                SourceSystem = "EmailParser",
                SourceEmailId = dto.SourceEmailId,
                OrderTypeId = orderTypeId,
                ServiceId = dto.ServiceId ?? string.Empty,
                TicketId = dto.TicketId,
                ExternalRef = dto.ExternalRef,
                Status = "Pending",
                StatusReason = null,
                Priority = "Normal",
                BuildingId = resolvedBuildingId ?? Guid.Empty,
                BuildingName = addressComponents.BuildingName,
                UnitNo = addressComponents.UnitNo,
                AddressLine1 = addressComponents.AddressLine1,
                AddressLine2 = addressComponents.AddressLine2,
                City = addressComponents.City ?? string.Empty,
                State = addressComponents.State ?? string.Empty,
                Postcode = addressComponents.Postcode ?? string.Empty,
                Latitude = null,
                Longitude = null,
                OldAddress = dto.OldAddress,
                ServiceIdType = dto.ServiceIdType,
                PackageName = dto.PackageName,
                Bandwidth = dto.Bandwidth,
                OnuSerialNumber = dto.OnuSerialNumber,
                OnuPasswordEncrypted = !string.IsNullOrWhiteSpace(dto.OnuPassword) 
                    ? _encryptionService.Encrypt(dto.OnuPassword) 
                    : null,
                VoipServiceId = dto.VoipServiceId,
                // Network Info fields
                NetworkPackage = dto.NetworkPackage,
                NetworkBandwidth = dto.NetworkBandwidth,
                NetworkLoginId = dto.NetworkLoginId,
                NetworkPassword = dto.NetworkPassword,
                NetworkWanIp = dto.NetworkWanIp,
                NetworkLanIp = dto.NetworkLanIp,
                NetworkGateway = dto.NetworkGateway,
                NetworkSubnetMask = dto.NetworkSubnetMask,
                // VOIP fields
                VoipPassword = dto.VoipPassword,
                VoipIpAddressOnu = dto.VoipIpAddressOnu,
                VoipGatewayOnu = dto.VoipGatewayOnu,
                VoipSubnetMaskOnu = dto.VoipSubnetMaskOnu,
                VoipIpAddressSrp = dto.VoipIpAddressSrp,
                VoipRemarks = dto.VoipRemarks,
                CustomerName = dto.CustomerName ?? string.Empty,
                CustomerPhone = normalizedPhone,
                CustomerPhone2 = dto.AdditionalContactNumber, // ✅ Map Additional Contact Number to CustomerPhone2
                CustomerEmail = dto.CustomerEmail,
                Issue = dto.Issue, // ✅ Map Issue for Assurance orders
                Solution = null, // Solution will be entered later by SI/Admin
                OrderNotesInternal = dto.ValidationNotes,
                PartnerNotes = dto.Remarks,
                RequestedAppointmentAt = dto.AppointmentDate,
                AppointmentDate = appointmentDate,
                AppointmentWindowFrom = windowFrom,
                AppointmentWindowTo = windowTo,
                AssignedSiId = null,
                AssignedTeamId = null,
                KpiCategory = null,
                KpiDueAt = null,
                KpiBreachedAt = null,
                HasReschedules = false,
                RescheduleCount = 0,
                DocketUploaded = false,
                PhotosUploaded = false,
                SerialsValidated = false,
                InvoiceEligible = false,
                InvoiceId = null,
                PayrollPeriodId = null,
                PnlPeriod = null,
                CreatedByUserId = userId,
                CreatedAt = now,
                UpdatedAt = now,
                CancelledAt = null,
                CancelledByUserId = null,
                DepartmentId = resolvedDepartmentId,
                OrderCategoryId = resolvedOrderCategoryId,
                InstallationMethodId = resolvedInstallationMethodId
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order created from parsed draft: OrderId={OrderId}, DraftId={DraftId}, ServiceId={ServiceId}, OrderCategoryId={OrderCategoryId}, InstallationMethodId={InstallationMethodId}, User={UserId}",
                orderId, dto.ParsedOrderDraftId, dto.ServiceId, resolvedOrderCategoryId, resolvedInstallationMethodId, userId);

            if (_tenantUsageService != null && dto.CompanyId.HasValue)
                await _tenantUsageService.RecordUsageAsync(dto.CompanyId, TenantUsageService.MetricKeys.OrdersCreated, 1, cancellationToken);

            if (_eventBus != null)
            {
                var evt = new OrderCreatedEvent
                {
                    EventId = Guid.NewGuid(),
                    OccurredAtUtc = now,
                    CompanyId = dto.CompanyId,
                    TriggeredByUserId = userId,
                    OrderId = orderId,
                    PartnerId = resolvedPartnerId,
                    BuildingId = resolvedBuildingId,
                    SourceSystem = "EmailParser"
                };
                await _eventBus.PublishAsync(evt, cancellationToken);
            }

            // Auto-apply default materials from building + job type, then material templates
            if (resolvedBuildingId.HasValue && dto.CompanyId.HasValue)
            {
                await ApplyDefaultMaterialsAsync(orderId, resolvedBuildingId.Value, orderTypeId, resolvedPartnerId, dto.CompanyId.Value, userId, cancellationToken);
            }

            // Apply parsed materials from draft: resolve to Material and add OrderMaterialUsage (additive; skips if already present from defaults)
            var unmatchedNames = new List<string>();
            if (dto.Materials != null && dto.Materials.Count > 0 && dto.CompanyId.HasValue)
            {
                unmatchedNames = await ApplyParsedMaterialsAsync(orderId, dto.CompanyId.Value, userId, dto.Materials, cancellationToken);
            }

            // Persist unmatched material audit on draft so parser-origin order detail can show it later
            var draft = await _context.ParsedOrderDrafts.FirstOrDefaultAsync(d => d.Id == dto.ParsedOrderDraftId, cancellationToken);
            if (draft != null)
            {
                draft.UnmatchedMaterialCount = unmatchedNames.Count;
                draft.UnmatchedMaterialNamesJson = unmatchedNames.Count > 0 ? JsonSerializer.Serialize(unmatchedNames) : null;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return CreateOrderFromDraftResult.Succeeded(orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order from parsed draft: DraftId={DraftId}", dto.ParsedOrderDraftId);
            var message = GetInnermostMessage(ex);
            return CreateOrderFromDraftResult.Failed($"Database error: {message}");
        }
    }

    public async Task<bool> ExistsByServiceIdAsync(string serviceId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceId))
            return false;

        var query = _context.Orders.Where(o => o.ServiceId == serviceId);
        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByServiceIdAndTicketIdAsync(string serviceId, string ticketId, Guid? companyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(ticketId))
            return false;

        var query = _context.Orders.Where(o => o.ServiceId == serviceId && o.TicketId == ticketId);
        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<OrderDto> AddOrderNoteAsync(Guid id, string note, string userName, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.Where(o => o.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        var orderEntity = await query.FirstOrDefaultAsync(cancellationToken);
        if (orderEntity == null)
        {
            throw new KeyNotFoundException($"Order with ID {id} not found");
        }

        if (departmentId.HasValue && orderEntity.DepartmentId != departmentId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this order");
        }

        // Append note with timestamp and user
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var noteEntry = $"[{timestamp}] {userName}: {note}";
        
        if (string.IsNullOrEmpty(orderEntity.OrderNotesInternal))
        {
            orderEntity.OrderNotesInternal = noteEntry;
        }
        else
        {
            orderEntity.OrderNotesInternal = $"{orderEntity.OrderNotesInternal}\n{noteEntry}";
        }

        orderEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Note added to order: {OrderId} by {UserName}", id, userName);

        return MapToOrderDto(orderEntity);
    }

    public async Task<List<OrderStatusLogDto>> GetOrderStatusLogsAsync(Guid orderId, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status logs for order: {OrderId}", orderId);

        // Verify order exists and user has access
        var orderQuery = _context.Orders.Where(o => o.Id == orderId);
        if (companyId.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.CompanyId == companyId.Value);
        }
        var order = await orderQuery.FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found");
        }
        if (departmentId.HasValue && order.DepartmentId != departmentId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this order");
        }

        var logs = await _context.OrderStatusLogs
            .Where(l => l.OrderId == orderId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        // Load user and SI names
        var userIds = logs.Where(l => l.TriggeredByUserId.HasValue).Select(l => l.TriggeredByUserId!.Value).Distinct().ToList();
        var siIds = logs.Where(l => l.TriggeredBySiId.HasValue).Select(l => l.TriggeredBySiId!.Value).Distinct().ToList();

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);
        var sis = await _context.ServiceInstallers.Where(s => siIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return logs.Select(l => new OrderStatusLogDto
        {
            Id = l.Id,
            OrderId = l.OrderId,
            FromStatus = l.FromStatus,
            ToStatus = l.ToStatus,
            TransitionReason = l.TransitionReason,
            TriggeredByUserId = l.TriggeredByUserId,
            TriggeredByUserName = l.TriggeredByUserId.HasValue && users.TryGetValue(l.TriggeredByUserId.Value, out var userName) ? userName : null,
            TriggeredBySiId = l.TriggeredBySiId,
            TriggeredBySiName = l.TriggeredBySiId.HasValue && sis.TryGetValue(l.TriggeredBySiId.Value, out var siName) ? siName : null,
            Source = l.Source,
            MetadataJson = l.MetadataJson,
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    public async Task<List<OrderRescheduleDto>> GetOrderReschedulesAsync(Guid orderId, Guid? companyId, Guid? departmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting reschedules for order: {OrderId}", orderId);

        // Verify order exists and user has access
        var orderQuery = _context.Orders.Where(o => o.Id == orderId);
        if (companyId.HasValue)
        {
            orderQuery = orderQuery.Where(o => o.CompanyId == companyId.Value);
        }
        var order = await orderQuery.FirstOrDefaultAsync(cancellationToken);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found");
        }
        if (departmentId.HasValue && order.DepartmentId != departmentId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this order");
        }

        var reschedules = await _context.OrderReschedules
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

        // Load user and SI names
        var userIds = reschedules
            .SelectMany(r => new[] { r.RequestedByUserId, r.StatusChangedByUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var siIds = reschedules.Where(r => r.RequestedBySiId.HasValue).Select(r => r.RequestedBySiId!.Value).Distinct().ToList();

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);
        var sis = await _context.ServiceInstallers.Where(s => siIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        return reschedules.Select(r => new OrderRescheduleDto
        {
            Id = r.Id,
            OrderId = r.OrderId,
            RequestedByUserId = r.RequestedByUserId,
            RequestedByUserName = r.RequestedByUserId.HasValue && users.TryGetValue(r.RequestedByUserId.Value, out var reqUserName) ? reqUserName : null,
            RequestedBySiId = r.RequestedBySiId,
            RequestedBySiName = r.RequestedBySiId.HasValue && sis.TryGetValue(r.RequestedBySiId.Value, out var siName) ? siName : null,
            RequestedBySource = r.RequestedBySource,
            RequestedAt = r.RequestedAt,
            OriginalDate = r.OriginalDate,
            OriginalWindowFrom = r.OriginalWindowFrom,
            OriginalWindowTo = r.OriginalWindowTo,
            NewDate = r.NewDate,
            NewWindowFrom = r.NewWindowFrom,
            NewWindowTo = r.NewWindowTo,
            Reason = r.Reason,
            ApprovalSource = r.ApprovalSource,
            ApprovalEmailId = r.ApprovalEmailId,
            Status = r.Status,
            StatusChangedByUserId = r.StatusChangedByUserId,
            StatusChangedByUserName = r.StatusChangedByUserId.HasValue && users.TryGetValue(r.StatusChangedByUserId.Value, out var statusUserName) ? statusUserName : null,
            StatusChangedAt = r.StatusChangedAt,
            IsSameDayReschedule = r.IsSameDayReschedule,
            SameDayEvidenceAttachmentId = r.SameDayEvidenceAttachmentId,
            SameDayEvidenceNotes = r.SameDayEvidenceNotes,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    private static string GetInnermostMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current.Message;
    }

    private async Task<Guid> GetFirstActivePartnerIdForCompanyAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        var query = _context.Partners.Where(p => p.IsActive);
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            query = query.Where(p => p.CompanyId == companyId.Value);
        var partner = await query.OrderBy(p => p.Name).Select(p => p.Id).FirstOrDefaultAsync(cancellationToken);
        return partner;
    }

    private static List<string> ValidateDraftForOrderCreation(CreateOrderFromDraftDto dto)
    {
        var errors = new List<string>();

        // Mandatory fields
        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            errors.Add("CustomerName is required");

        if (string.IsNullOrWhiteSpace(dto.CustomerPhone))
            errors.Add("CustomerPhone is required");

        if (string.IsNullOrWhiteSpace(dto.AddressText))
            errors.Add("AddressText is required");

        // AppointmentDate and AppointmentWindow are optional - defaults will be provided
        // Removed strict validation to allow orders to be created with defaults

        // Conditional: ServiceId required for activation orders
        var isAssurance = IsAssuranceOrder(dto.OrderTypeHint);
        if (!isAssurance && string.IsNullOrWhiteSpace(dto.ServiceId))
            errors.Add("ServiceId is required for activation orders");

        // Conditional: TicketId required for assurance orders
        if (isAssurance && string.IsNullOrWhiteSpace(dto.TicketId))
            errors.Add("TicketId is required for assurance orders");

        return errors;
    }

    private static bool IsAssuranceOrder(string? orderTypeHint)
    {
        if (string.IsNullOrEmpty(orderTypeHint))
            return false;

        return orderTypeHint.Equals("Assurance", StringComparison.OrdinalIgnoreCase) ||
               orderTypeHint.Contains("TTKT", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid> ResolveOrderTypeIdAsync(string? orderTypeHint, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(orderTypeHint))
            return Guid.Empty;

        // Normalize hint: convert spaces to underscores and uppercase (e.g., "Modification Outdoor" -> "MODIFICATION_OUTDOOR")
        var normalizedHint = orderTypeHint.ToUpper().Replace(" ", "_").Trim();

        // Try exact code match first (e.g., "MODIFICATION_OUTDOOR")
        var orderType = await _context.OrderTypes
            .Where(ot => ot.Code.ToUpper() == normalizedHint)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderType != null)
        {
            _logger.LogInformation("Resolved OrderType by exact code match: {Hint} -> {Code} ({Name})", 
                orderTypeHint, orderType.Code, orderType.Name);
            return orderType.Id;
        }

        // Try name or code contains match (fallback for variations)
        orderType = await _context.OrderTypes
            .Where(ot => ot.Name.ToLower().Contains(orderTypeHint.ToLower()) ||
                         ot.Code.ToLower().Contains(orderTypeHint.ToLower()))
            .FirstOrDefaultAsync(cancellationToken);

        if (orderType != null)
        {
            _logger.LogInformation("Resolved OrderType by contains match: {Hint} -> {Code} ({Name})", 
                orderTypeHint, orderType.Code, orderType.Name);
            return orderType.Id;
        }

        // Fallback: try to find a default order type
        var defaultOrderType = await _context.OrderTypes
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultOrderType != null)
        {
            _logger.LogWarning("Using default OrderType for hint '{Hint}': {Code} ({Name})", 
                orderTypeHint, defaultOrderType.Code, defaultOrderType.Name);
            return defaultOrderType.Id;
        }

        return Guid.Empty;
    }

    private static OrderDto MapToOrderDto(
        Order o,
        List<ParsedDraftMaterialDto>? parsedMaterials = null,
        Partner? partner = null,
        OrderCategory? orderCategory = null,
        InstallationMethod? installationMethod = null,
        int? unmatchedParsedMaterialCount = null,
        List<string>? unmatchedParsedMaterialNames = null)
    {
        var partnerCode = partner != null ? (partner.Code ?? partner.Name) : null;
        var orderCategoryCode = orderCategory?.Code;
        var installationMethodCode = installationMethod?.Code;
        var installationMethodName = installationMethod?.Name;
        var derivedPartnerCategoryLabel = (partner != null && orderCategory != null && !string.IsNullOrEmpty(orderCategory.Code))
            ? $"{partnerCode}-{orderCategory.Code}"
            : null;

        return new OrderDto
        {
            Id = o.Id,
            CompanyId = o.CompanyId,
            PartnerId = o.PartnerId,
            PartnerCode = partnerCode,
            OrderCategoryCode = orderCategoryCode,
            InstallationMethodCode = installationMethodCode,
            InstallationMethodName = installationMethodName,
            DerivedPartnerCategoryLabel = derivedPartnerCategoryLabel,
            SourceSystem = o.SourceSystem,
            SourceEmailId = o.SourceEmailId,
            OrderTypeId = o.OrderTypeId,
            ServiceIdType = o.ServiceIdType,
            ServiceId = o.ServiceId,
            TicketId = o.TicketId,
            ExternalRef = o.ExternalRef,
            Status = o.Status,
            StatusReason = o.StatusReason,
            Priority = o.Priority,
            BuildingId = o.BuildingId,
            BuildingName = o.BuildingName,
            UnitNo = o.UnitNo,
            AddressLine1 = o.AddressLine1,
            AddressLine2 = o.AddressLine2,
            City = o.City,
            State = o.State,
            Postcode = o.Postcode,
            Latitude = o.Latitude,
            Longitude = o.Longitude,
            CustomerName = o.CustomerName,
            CustomerPhone = o.CustomerPhone,
            CustomerPhone2 = o.CustomerPhone2,
            CustomerEmail = o.CustomerEmail,
            Issue = o.Issue, // ✅ Map Issue for Assurance orders
            Solution = o.Solution, // ✅ Map Solution for Assurance orders
            OrderNotesInternal = o.OrderNotesInternal,
            PartnerNotes = o.PartnerNotes,
            RequestedAppointmentAt = o.RequestedAppointmentAt,
            AppointmentDate = o.AppointmentDate,
            AppointmentWindowFrom = o.AppointmentWindowFrom,
            AppointmentWindowTo = o.AppointmentWindowTo,
            AssignedSiId = o.AssignedSiId,
            AssignedTeamId = o.AssignedTeamId,
            KpiCategory = o.KpiCategory,
            KpiDueAt = o.KpiDueAt,
            KpiBreachedAt = o.KpiBreachedAt,
            HasReschedules = o.HasReschedules,
            RescheduleCount = o.RescheduleCount,
            DocketUploaded = o.DocketUploaded,
            PhotosUploaded = o.PhotosUploaded,
            SerialsValidated = o.SerialsValidated,
            InvoiceEligible = o.InvoiceEligible,
            InvoiceId = o.InvoiceId,
            PayrollPeriodId = o.PayrollPeriodId,
            PnlPeriod = o.PnlPeriod,
            CreatedByUserId = o.CreatedByUserId,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            CancelledAt = o.CancelledAt,
            CancelledByUserId = o.CancelledByUserId,
            DepartmentId = o.DepartmentId,
            OrderCategoryId = o.OrderCategoryId,
            InstallationMethodId = o.InstallationMethodId,
            ParsedMaterials = parsedMaterials ?? new List<ParsedDraftMaterialDto>(),
            UnmatchedParsedMaterialCount = unmatchedParsedMaterialCount,
            UnmatchedParsedMaterialNames = unmatchedParsedMaterialNames,
            // Network Info fields
            PackageName = o.PackageName,
            Bandwidth = o.Bandwidth,
            NetworkPackage = o.NetworkPackage,
            NetworkBandwidth = o.NetworkBandwidth,
            NetworkLoginId = o.NetworkLoginId,
            NetworkPassword = o.NetworkPassword,
            NetworkWanIp = o.NetworkWanIp,
            NetworkLanIp = o.NetworkLanIp,
            NetworkGateway = o.NetworkGateway,
            NetworkSubnetMask = o.NetworkSubnetMask,
            // VOIP fields
            VoipServiceId = o.VoipServiceId,
            VoipPassword = o.VoipPassword,
            VoipIpAddressOnu = o.VoipIpAddressOnu,
            VoipGatewayOnu = o.VoipGatewayOnu,
            VoipSubnetMaskOnu = o.VoipSubnetMaskOnu,
            VoipIpAddressSrp = o.VoipIpAddressSrp,
            VoipRemarks = o.VoipRemarks
        };
    }

    #region Integration Methods

    /// <summary>
    /// Get order type name from OrderTypeId
    /// </summary>
    private async Task<string> GetOrderTypeNameAsync(Guid orderTypeId, CancellationToken cancellationToken)
    {
        var orderType = await _orderTypeService.GetOrderTypeByIdAsync(orderTypeId, null, cancellationToken);
        return orderType?.Name ?? orderTypeId.ToString();
    }

    /// <summary>
    /// Determine if order is VIP by checking source email
    /// </summary>
    private async Task<bool> IsOrderVipAsync(Order order, CancellationToken cancellationToken)
    {
        if (!order.SourceEmailId.HasValue) return false;

        var emailMessage = await _context.Set<CephasOps.Domain.Parser.Entities.EmailMessage>()
            .FirstOrDefaultAsync(e => e.Id == order.SourceEmailId.Value, cancellationToken);

        return emailMessage?.IsVip ?? false;
    }

    /// <summary>
    /// Calculate and track SLA for an order after status change
    /// </summary>
    private async Task CalculateAndTrackSlaAsync(Order order, string newStatus, CancellationToken cancellationToken)
    {
        if (order.CompanyId == null) return;

        try
        {
            // Get effective SLA profile for this order
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var isVip = await IsOrderVipAsync(order, cancellationToken);
            var slaProfile = await _slaProfileService.GetEffectiveProfileAsync(
                order.CompanyId.Value,
                order.PartnerId,
                orderTypeName,
                order.DepartmentId,
                isVip: isVip,
                effectiveDate: DateTime.UtcNow,
                cancellationToken);

            if (slaProfile == null)
            {
                _logger.LogDebug("No SLA profile found for order {OrderId}", order.Id);
                return;
            }

            // Calculate response SLA if configured
            if (slaProfile.ResponseSlaMinutes.HasValue && 
                !string.IsNullOrEmpty(slaProfile.ResponseSlaFromStatus) &&
                !string.IsNullOrEmpty(slaProfile.ResponseSlaToStatus))
            {
                if (newStatus == slaProfile.ResponseSlaToStatus)
                {
                    var fromStatusLog = await _context.OrderStatusLogs
                        .Where(sl => sl.OrderId == order.Id && sl.ToStatus == slaProfile.ResponseSlaFromStatus)
                        .OrderByDescending(sl => sl.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (fromStatusLog != null)
                    {
                        var elapsedMinutes = await CalculateBusinessMinutesAsync(
                            fromStatusLog.CreatedAt,
                            DateTime.UtcNow,
                            order.CompanyId.Value,
                            order.DepartmentId,
                            slaProfile,
                            cancellationToken);

                        if (elapsedMinutes > slaProfile.ResponseSlaMinutes.Value)
                        {
                            _logger.LogWarning(
                                "Order {OrderId} breached response SLA: {ElapsedMinutes} minutes (limit: {LimitMinutes})",
                                order.Id, elapsedMinutes, slaProfile.ResponseSlaMinutes.Value);
                            
                            // Update order with SLA breach info (using existing KpiBreachedAt field)
                            order.KpiBreachedAt = DateTime.UtcNow;
                            order.KpiCategory = "SlaBreach";
                            
                            // Notify if configured
                            if (slaProfile.NotifyOnBreach)
                            {
                                await SendSlaBreachNotificationAsync(order, "Response", elapsedMinutes, slaProfile.ResponseSlaMinutes.Value, cancellationToken);
                            }
                        }
                    }
                }
            }

            // Calculate resolution SLA if configured
            if (slaProfile.ResolutionSlaMinutes.HasValue &&
                !string.IsNullOrEmpty(slaProfile.ResolutionSlaFromStatus) &&
                !string.IsNullOrEmpty(slaProfile.ResolutionSlaToStatus))
            {
                if (newStatus == slaProfile.ResolutionSlaToStatus)
                {
                    var fromStatusLog = await _context.OrderStatusLogs
                        .Where(sl => sl.OrderId == order.Id && sl.ToStatus == slaProfile.ResolutionSlaFromStatus)
                        .OrderBy(sl => sl.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (fromStatusLog != null)
                    {
                        var elapsedMinutes = await CalculateBusinessMinutesAsync(
                            fromStatusLog.CreatedAt,
                            DateTime.UtcNow,
                            order.CompanyId.Value,
                            order.DepartmentId,
                            slaProfile,
                            cancellationToken);

                        if (elapsedMinutes > slaProfile.ResolutionSlaMinutes.Value)
                        {
                            _logger.LogWarning(
                                "Order {OrderId} breached resolution SLA: {ElapsedMinutes} minutes (limit: {LimitMinutes})",
                                order.Id, elapsedMinutes, slaProfile.ResolutionSlaMinutes.Value);
                            
                            order.KpiBreachedAt = DateTime.UtcNow;
                            order.KpiCategory = "SlaBreach";
                            
                            if (slaProfile.NotifyOnBreach)
                            {
                                await SendSlaBreachNotificationAsync(order, "Resolution", elapsedMinutes, slaProfile.ResolutionSlaMinutes.Value, cancellationToken);
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating SLA for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Calculate elapsed time in business minutes (excluding non-business hours, weekends, holidays)
    /// </summary>
    private async Task<int> CalculateBusinessMinutesAsync(
        DateTime startTime,
        DateTime endTime,
        Guid companyId,
        Guid? departmentId,
        SlaProfileDto slaProfile,
        CancellationToken cancellationToken)
    {
        var totalMinutes = (int)(endTime - startTime).TotalMinutes;

        // If SLA profile excludes non-business hours, calculate business minutes
        if (slaProfile.ExcludeNonBusinessHours)
        {
            var businessHours = await _businessHoursService.GetEffectiveBusinessHoursAsync(
                companyId, departmentId, DateTime.UtcNow, cancellationToken);

            if (businessHours != null)
            {
                return await CalculateBusinessMinutesBetweenAsync(
                    startTime, endTime, companyId, departmentId, businessHours, cancellationToken);
            }
        }

        return totalMinutes;
    }

    /// <summary>
    /// Calculate business minutes between two dates, excluding non-business hours, weekends, and holidays
    /// </summary>
    private async Task<int> CalculateBusinessMinutesBetweenAsync(
        DateTime startTime,
        DateTime endTime,
        Guid companyId,
        Guid? departmentId,
        BusinessHoursDto businessHours,
        CancellationToken cancellationToken)
    {
        var businessMinutes = 0;
        var current = startTime;

        while (current < endTime)
        {
            // Check if current time is a public holiday
            var isHoliday = await _businessHoursService.IsPublicHolidayAsync(companyId, current, cancellationToken);
            if (isHoliday)
            {
                current = current.Date.AddDays(1).Date; // Move to next day
                continue;
            }

            // Check if current time is within business hours for the day
            var isBusinessHours = await _businessHoursService.IsBusinessHoursAsync(companyId, current, departmentId, cancellationToken);
            if (isBusinessHours)
            {
                // Calculate minutes until end of current day or endTime, whichever comes first
                var dayEnd = current.Date.AddDays(1);
                var effectiveEnd = endTime < dayEnd ? endTime : dayEnd;
                
                // Get business hours end for this day
                var dayOfWeek = current.DayOfWeek;
                var dayEndTime = GetDayEndTime(businessHours, dayOfWeek);
                
                if (!string.IsNullOrEmpty(dayEndTime))
                {
                    var dayEndDateTime = current.Date.Add(TimeSpan.Parse(dayEndTime));
                    if (effectiveEnd > dayEndDateTime)
                    {
                        effectiveEnd = dayEndDateTime;
                    }
                }

                businessMinutes += (int)(effectiveEnd - current).TotalMinutes;
                current = effectiveEnd;
            }
            else
            {
                // Move to next business hour or next day
                var dayOfWeek = current.DayOfWeek;
                var dayStartTime = GetDayStartTime(businessHours, dayOfWeek);
                
                if (!string.IsNullOrEmpty(dayStartTime))
                {
                    var dayStartDateTime = current.Date.Add(TimeSpan.Parse(dayStartTime));
                    if (dayStartDateTime > current && dayStartDateTime < endTime)
                    {
                        current = dayStartDateTime;
                        continue;
                    }
                }

                // Move to next day
                current = current.Date.AddDays(1).Date;
            }
        }

        return businessMinutes;
    }

    private static string? GetDayStartTime(BusinessHoursDto businessHours, DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => businessHours.MondayStart,
            DayOfWeek.Tuesday => businessHours.TuesdayStart,
            DayOfWeek.Wednesday => businessHours.WednesdayStart,
            DayOfWeek.Thursday => businessHours.ThursdayStart,
            DayOfWeek.Friday => businessHours.FridayStart,
            DayOfWeek.Saturday => businessHours.SaturdayStart,
            DayOfWeek.Sunday => businessHours.SundayStart,
            _ => null
        };
    }

    private static string? GetDayEndTime(BusinessHoursDto businessHours, DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => businessHours.MondayEnd,
            DayOfWeek.Tuesday => businessHours.TuesdayEnd,
            DayOfWeek.Wednesday => businessHours.WednesdayEnd,
            DayOfWeek.Thursday => businessHours.ThursdayEnd,
            DayOfWeek.Friday => businessHours.FridayEnd,
            DayOfWeek.Saturday => businessHours.SaturdayEnd,
            DayOfWeek.Sunday => businessHours.SundayEnd,
            _ => null
        };
    }

    /// <summary>
    /// Send SLA breach notification
    /// </summary>
    private async Task SendSlaBreachNotificationAsync(Order order, string slaType, int elapsedMinutes, int limitMinutes, CancellationToken cancellationToken)
    {
        try
        {
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var title = $"SLA Breach: {slaType} SLA Exceeded";
            var message = $"Order {order.ServiceId} ({orderTypeName}) has exceeded {slaType} SLA. " +
                        $"Elapsed: {elapsedMinutes} minutes, Limit: {limitMinutes} minutes.";

            // Get users to notify (department members or managers)
            var userIds = new List<Guid>();
            if (order.DepartmentId.HasValue)
            {
                userIds.AddRange(await _notificationService.ResolveUsersByDepartmentAsync(order.DepartmentId.Value, cancellationToken));
            }

            // Also notify users with Manager role
            var managerUserIds = await _notificationService.ResolveUsersByRoleAsync("Manager", order.CompanyId, cancellationToken);
            userIds.AddRange(managerUserIds);

            // Remove duplicates
            userIds = userIds.Distinct().ToList();

            // Create notifications for each user
            foreach (var userId in userIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = userId,
                    Type = "SlaBreach",
                    Title = title,
                    Message = message,
                    Priority = "High",
                    CompanyId = order.CompanyId,
                    RelatedEntityId = order.Id,
                    RelatedEntityType = "Order",
                    ActionUrl = $"/orders/{order.Id}",
                    ActionText = "View Order"
                }, cancellationToken);
            }

            _logger.LogInformation("SLA breach notifications sent for order {OrderId} to {UserCount} users", order.Id, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SLA breach notification for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Execute automation rules after order status change
    /// </summary>
    private async Task ExecuteAutomationRulesAsync(Order order, string oldStatus, string newStatus, CancellationToken cancellationToken)
    {
        if (order.CompanyId == null) return;

        try
        {
            // Get applicable automation rules
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var rules = await _automationRuleService.GetApplicableRulesAsync(
                order.CompanyId.Value,
                "Order",
                currentStatus: newStatus,
                partnerId: order.PartnerId,
                departmentId: order.DepartmentId,
                orderType: orderTypeName,
                cancellationToken);

            foreach (var rule in rules)
            {
                try
                {
                    // Parse action config JSON
                    Dictionary<string, object>? actionConfig = null;
                    if (!string.IsNullOrEmpty(rule.ActionConfigJson))
                    {
                        try
                        {
                            actionConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ActionConfigJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse action config for rule {RuleId}", rule.Id);
                        }
                    }

                    // Execute rule based on type
                    switch (rule.RuleType.ToLowerInvariant())
                    {
                        case "auto-assign":
                            await HandleAutoAssignAsync(order, rule, actionConfig, cancellationToken);
                            break;

                        case "auto-escalate":
                            await HandleAutoEscalateAsync(order, rule, actionConfig, cancellationToken);
                            break;

                        case "auto-notify":
                            await HandleAutoNotifyAsync(order, rule, actionConfig, cancellationToken);
                            break;

                        case "auto-status-change":
                            await HandleAutoStatusChangeAsync(order, rule, actionConfig, cancellationToken);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing automation rule {RuleId} for order {OrderId}", rule.Id, order.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing automation rules for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Handle auto-assign automation rule
    /// </summary>
    private async Task HandleAutoAssignAsync(Order order, AutomationRuleDto rule, Dictionary<string, object>? actionConfig, CancellationToken cancellationToken)
    {
        try
        {
            var role = actionConfig?.ContainsKey("assignToRole") == true 
                ? actionConfig["assignToRole"]?.ToString() 
                : rule.TargetRole;

            if (string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("Auto-assign rule {RuleId} has no target role", rule.Id);
                return;
            }

            // Get users with the target role
            var userIds = await _notificationService.ResolveUsersByRoleAsync(role, order.CompanyId, cancellationToken);
            
            if (userIds.Count == 0)
            {
                _logger.LogWarning("No users found with role {Role} for auto-assignment", role);
                return;
            }

            // For now, assign to first available user (could be enhanced with load balancing)
            // In a real scenario, you might want to check availability, workload, etc.
            var assignedUserId = userIds.First();
            
            // Get service installer ID from user (if user is a service installer)
            // This is a simplified implementation - in reality, you'd need to check User -> ServiceInstaller relationship
            var serviceInstaller = await _context.Set<ServiceInstaller>()
                .FirstOrDefaultAsync(si => si.UserId == assignedUserId && si.CompanyId == order.CompanyId, cancellationToken);

            if (serviceInstaller != null)
            {
                order.AssignedSiId = serviceInstaller.Id;
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation(
                    "Auto-assigned order {OrderId} to service installer {SiId} via rule {RuleId}",
                    order.Id, serviceInstaller.Id, rule.Id);
            }
            else
            {
                _logger.LogWarning(
                    "User {UserId} with role {Role} is not a service installer, cannot auto-assign order {OrderId}",
                    assignedUserId, role, order.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling auto-assign for rule {RuleId} on order {OrderId}", rule.Id, order.Id);
        }
    }

    /// <summary>
    /// Handle auto-escalate automation rule
    /// </summary>
    private async Task HandleAutoEscalateAsync(Order order, AutomationRuleDto rule, Dictionary<string, object>? actionConfig, CancellationToken cancellationToken)
    {
        try
        {
            var escalateRole = actionConfig?.ContainsKey("escalateToRole") == true 
                ? actionConfig["escalateToRole"]?.ToString() 
                : rule.TargetRole;

            if (string.IsNullOrEmpty(escalateRole))
            {
                _logger.LogWarning("Auto-escalate rule {RuleId} has no target role", rule.Id);
                return;
            }

            // Get users with the escalation role
            var userIds = await _notificationService.ResolveUsersByRoleAsync(escalateRole, order.CompanyId, cancellationToken);
            
            // Send escalation notification
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var title = $"Order Escalated: {order.ServiceId}";
            var message = $"Order {order.ServiceId} ({orderTypeName}) has been escalated to {escalateRole} role.";

            foreach (var userId in userIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = userId,
                    Type = "OrderEscalated",
                    Title = title,
                    Message = message,
                    Priority = "High",
                    CompanyId = order.CompanyId,
                    RelatedEntityId = order.Id,
                    RelatedEntityType = "Order",
                    ActionUrl = $"/orders/{order.Id}",
                    ActionText = "View Order"
                }, cancellationToken);
            }

            _logger.LogInformation(
                "Auto-escalated order {OrderId} to role {Role} via rule {RuleId}, notified {UserCount} users",
                order.Id, escalateRole, rule.Id, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling auto-escalate for rule {RuleId} on order {OrderId}", rule.Id, order.Id);
        }
    }

    /// <summary>
    /// Handle auto-notify automation rule
    /// </summary>
    private async Task HandleAutoNotifyAsync(Order order, AutomationRuleDto rule, Dictionary<string, object>? actionConfig, CancellationToken cancellationToken)
    {
        try
        {
            var notifyRolesStr = actionConfig?.ContainsKey("notifyRoles") == true 
                ? actionConfig["notifyRoles"]?.ToString() 
                : rule.TargetRole;

            if (string.IsNullOrEmpty(notifyRolesStr))
            {
                _logger.LogWarning("Auto-notify rule {RuleId} has no target roles", rule.Id);
                return;
            }

            // Parse roles (could be comma-separated)
            var roles = notifyRolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var allUserIds = new List<Guid>();

            foreach (var role in roles)
            {
                var userIds = await _notificationService.ResolveUsersByRoleAsync(role, order.CompanyId, cancellationToken);
                allUserIds.AddRange(userIds);
            }

            // Remove duplicates
            allUserIds = allUserIds.Distinct().ToList();

            if (allUserIds.Count == 0)
            {
                _logger.LogWarning("No users found for auto-notify rule {RuleId}", rule.Id);
                return;
            }

            // Send notifications
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var title = (actionConfig != null && actionConfig.ContainsKey("notificationTitle")) 
                ? actionConfig["notificationTitle"]?.ToString() 
                : $"Order Status Changed: {order.ServiceId}";
            var message = (actionConfig != null && actionConfig.ContainsKey("notificationMessage"))
                ? actionConfig["notificationMessage"]?.ToString()
                : $"Order {order.ServiceId} ({orderTypeName}) status changed to {order.Status}.";

            foreach (var userId in allUserIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = userId,
                    Type = "OrderStatusChanged",
                    Title = title ?? $"Order Status Changed: {order.ServiceId}",
                    Message = message ?? $"Order {order.ServiceId} ({orderTypeName}) status changed to {order.Status}.",
                    Priority = "Normal", // Priority is int in AutomationRule, but notification expects string - use default
                    CompanyId = order.CompanyId,
                    RelatedEntityId = order.Id,
                    RelatedEntityType = "Order",
                    ActionUrl = $"/orders/{order.Id}",
                    ActionText = "View Order"
                }, cancellationToken);
            }

            _logger.LogInformation(
                "Auto-notified {UserCount} users about order {OrderId} via rule {RuleId}",
                allUserIds.Count, order.Id, rule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling auto-notify for rule {RuleId} on order {OrderId}", rule.Id, order.Id);
        }
    }

    /// <summary>
    /// Handle auto-status-change automation rule
    /// </summary>
    private async Task HandleAutoStatusChangeAsync(Order order, AutomationRuleDto rule, Dictionary<string, object>? actionConfig, CancellationToken cancellationToken)
    {
        try
        {
            var targetStatus = actionConfig?.ContainsKey("targetStatus") == true 
                ? actionConfig["targetStatus"]?.ToString() 
                : rule.TargetStatus;

            if (string.IsNullOrEmpty(targetStatus))
            {
                _logger.LogWarning("Auto-status-change rule {RuleId} has no target status", rule.Id);
                return;
            }

            // Recursively call ChangeOrderStatusAsync
            // Note: This will trigger workflow validation and all integrations again
            var changeDto = new ChangeOrderStatusDto
            {
                Status = targetStatus,
                Reason = $"Auto-status-change via automation rule {rule.Name}",
                Metadata = new Dictionary<string, object>
                {
                    ["automationRuleId"] = rule.Id.ToString(),
                    ["source"] = "AutomationRule"
                }
            };

            // Get current user from context or use system user
            // In a real scenario, you'd get this from ICurrentUserService
            var systemUserId = Guid.Empty; // This should be replaced with actual system user ID

            await ChangeOrderStatusAsync(
                order.Id,
                changeDto,
                order.CompanyId,
                order.DepartmentId,
                systemUserId,
                cancellationToken);

            _logger.LogInformation(
                "Auto-changed order {OrderId} status to {Status} via rule {RuleId}",
                order.Id, targetStatus, rule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling auto-status-change for rule {RuleId} on order {OrderId}", rule.Id, order.Id);
        }
    }

    /// <summary>
    /// Check and execute escalation rules for an order
    /// </summary>
    private async Task CheckEscalationRulesAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.CompanyId == null) return;

        try
        {
            // Get applicable escalation rules
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var rules = await _escalationRuleService.GetApplicableRulesAsync(
                order.CompanyId.Value,
                "Order",
                currentStatus: order.Status,
                partnerId: order.PartnerId,
                departmentId: order.DepartmentId,
                orderType: orderTypeName,
                cancellationToken);

            foreach (var rule in rules)
            {
                try
                {
                    bool shouldEscalate = false;

                    // Parse trigger config JSON
                    Dictionary<string, object>? triggerConfig = null;
                    if (!string.IsNullOrEmpty(rule.TriggerConditionsJson))
                    {
                        try
                        {
                            triggerConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.TriggerConditionsJson);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse trigger config for escalation rule {RuleId}", rule.Id);
                        }
                    }

                    // Check trigger conditions
                    switch (rule.TriggerType.ToLowerInvariant())
                    {
                        case "time-based":
                            var timeMinutes = rule.TriggerDelayMinutes;
                            if (triggerConfig?.ContainsKey("timeMinutes") == true)
                            {
                                timeMinutes = Convert.ToInt32(triggerConfig["timeMinutes"]);
                            }

                            if (timeMinutes.HasValue)
                            {
                                var statusLog = await _context.OrderStatusLogs
                                    .Where(sl => sl.OrderId == order.Id && sl.ToStatus == order.Status)
                                    .OrderByDescending(sl => sl.CreatedAt)
                                    .FirstOrDefaultAsync(cancellationToken);

                                if (statusLog != null)
                                {
                                    var elapsedMinutes = (int)(DateTime.UtcNow - statusLog.CreatedAt).TotalMinutes;
                                    if (elapsedMinutes >= timeMinutes.Value)
                                    {
                                        shouldEscalate = true;
                                    }
                                }
                            }
                            break;

                        case "status-based":
                            // Already filtered by status in GetApplicableRulesAsync
                            shouldEscalate = true;
                            break;

                        case "condition-based":
                            // Check custom conditions from trigger config
                            if (triggerConfig != null)
                            {
                                shouldEscalate = EvaluateEscalationConditions(order, triggerConfig);
                            }
                            break;
                    }

                    if (shouldEscalate)
                    {
                        _logger.LogInformation(
                            "Escalation rule {RuleId} triggered for order {OrderId}",
                            rule.Id, order.Id);

                        // Execute escalation action
                        if (!string.IsNullOrEmpty(rule.TargetRole))
                        {
                            await EscalateToRoleAsync(order, rule.TargetRole, cancellationToken);
                        }

                        if (rule.TargetUserId.HasValue)
                        {
                            await EscalateToUserAsync(order, rule.TargetUserId.Value, cancellationToken);
                        }

                        if (!string.IsNullOrEmpty(rule.TargetStatus))
                        {
                            await EscalateToStatusAsync(order, rule.TargetStatus, rule.Id, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking escalation rule {RuleId} for order {OrderId}", rule.Id, order.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking escalation rules for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Evaluate escalation conditions from trigger config
    /// </summary>
    private bool EvaluateEscalationConditions(Order order, Dictionary<string, object> triggerConfig)
    {
        // Check priority condition
        if (triggerConfig.ContainsKey("priority"))
        {
            var requiredPriority = triggerConfig["priority"]?.ToString();
            if (!string.IsNullOrEmpty(requiredPriority) && order.Priority != requiredPriority)
            {
                return false;
            }
        }

        // Check VIP condition
        if (triggerConfig.ContainsKey("isVip"))
        {
            var requiresVip = Convert.ToBoolean(triggerConfig["isVip"]);
            // Note: This would need to check order's VIP status, which we'd need to load
            // For now, we'll skip this check or implement it if needed
        }

        // Add more condition checks as needed
        return true;
    }

    /// <summary>
    /// Escalate order to a role
    /// </summary>
    private async Task EscalateToRoleAsync(Order order, string role, CancellationToken cancellationToken)
    {
        try
        {
            var userIds = await _notificationService.ResolveUsersByRoleAsync(role, order.CompanyId, cancellationToken);
            
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var title = $"Order Escalated: {order.ServiceId}";
            var message = $"Order {order.ServiceId} ({orderTypeName}) has been escalated to {role} role.";

            foreach (var userId in userIds)
            {
                await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                {
                    UserId = userId,
                    Type = "OrderEscalated",
                    Title = title,
                    Message = message,
                    Priority = "High",
                    CompanyId = order.CompanyId,
                    RelatedEntityId = order.Id,
                    RelatedEntityType = "Order",
                    ActionUrl = $"/orders/{order.Id}",
                    ActionText = "View Order"
                }, cancellationToken);
            }

            _logger.LogInformation("Escalated order {OrderId} to role {Role}, notified {UserCount} users", order.Id, role, userIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating order {OrderId} to role {Role}", order.Id, role);
        }
    }

    /// <summary>
    /// Escalate order to a specific user
    /// </summary>
    private async Task EscalateToUserAsync(Order order, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var orderTypeName = await GetOrderTypeNameAsync(order.OrderTypeId, cancellationToken);
            var title = $"Order Escalated: {order.ServiceId}";
            var message = $"Order {order.ServiceId} ({orderTypeName}) has been escalated to you.";

            await _notificationService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = userId,
                Type = "OrderEscalated",
                Title = title,
                Message = message,
                Priority = "High",
                CompanyId = order.CompanyId,
                RelatedEntityId = order.Id,
                RelatedEntityType = "Order",
                ActionUrl = $"/orders/{order.Id}",
                ActionText = "View Order"
            }, cancellationToken);

            _logger.LogInformation("Escalated order {OrderId} to user {UserId}", order.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating order {OrderId} to user {UserId}", order.Id, userId);
        }
    }

    /// <summary>
    /// Escalate order by changing status
    /// </summary>
    private async Task EscalateToStatusAsync(Order order, string targetStatus, Guid escalationRuleId, CancellationToken cancellationToken)
    {
        try
        {
            var changeDto = new ChangeOrderStatusDto
            {
                Status = targetStatus,
                Reason = $"Escalated via escalation rule",
                Metadata = new Dictionary<string, object>
                {
                    ["escalationRuleId"] = escalationRuleId.ToString(),
                    ["source"] = "EscalationRule"
                }
            };

            // Get current user from context or use system user
            var systemUserId = Guid.Empty; // This should be replaced with actual system user ID

            await ChangeOrderStatusAsync(
                order.Id,
                changeDto,
                order.CompanyId,
                order.DepartmentId,
                systemUserId,
                cancellationToken);

            _logger.LogInformation("Escalated order {OrderId} to status {Status} via escalation rule {RuleId}", 
                order.Id, targetStatus, escalationRuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating order {OrderId} to status {Status}", order.Id, targetStatus);
        }
    }

    #endregion

    #region Delivery Order Management

    /// <summary>
    /// Create a delivery order for an assigned order with materials
    /// This automatically creates a delivery order when an order is assigned to an SI,
    /// based on the materials defined in OrderMaterialUsage
    /// </summary>
    private async Task CreateDeliveryOrderForAssignedOrderAsync(Order order, Guid userId, CancellationToken cancellationToken)
    {
        if (order.CompanyId == null || !order.AssignedSiId.HasValue)
        {
            return; // Cannot create delivery order without company or SI assignment
        }

        try
        {
            // Check if delivery order already exists for this order
            var existingDo = await _context.Set<DeliveryOrder>()
                .FirstOrDefaultAsync(d => d.OrderId == order.Id && !d.IsDeleted, cancellationToken);

            if (existingDo != null)
            {
                _logger.LogInformation("Delivery order already exists for order {OrderId}: {DeliveryOrderId}", order.Id, existingDo.Id);
                return;
            }

            // Get order materials
            var orderMaterials = await _context.Set<OrderMaterialUsage>()
                .Where(om => om.OrderId == order.Id && !om.IsDeleted)
                .Include(om => om.Material)
                .ToListAsync(cancellationToken);

            if (orderMaterials.Count == 0)
            {
                _logger.LogInformation("Order {OrderId} has no materials, skipping delivery order creation", order.Id);
                return; // No materials to deliver
            }

            // Get SI information
            var si = await _context.Set<ServiceInstaller>()
                .FirstOrDefaultAsync(s => s.Id == order.AssignedSiId.Value && s.CompanyId == order.CompanyId, cancellationToken);

            if (si == null)
            {
                _logger.LogWarning("Service installer {SiId} not found for order {OrderId}, skipping delivery order creation", order.AssignedSiId, order.Id);
                return;
            }

            // Generate DO number
            var doNumber = await GenerateDeliveryOrderNumberAsync(order.CompanyId.Value, cancellationToken);

            // Create delivery order
            var deliveryOrder = new DeliveryOrder
            {
                Id = Guid.NewGuid(),
                CompanyId = order.CompanyId.Value,
                DoNumber = doNumber,
                DoDate = DateTime.UtcNow,
                DoType = "Outbound",
                Status = "Pending",
                OrderId = order.Id,
                RecipientName = si.Name ?? $"SI-{si.EmployeeId ?? si.Id.ToString()}",
                RecipientPhone = si.Phone,
                RecipientEmail = si.Email,
                DeliveryAddress = order.AddressLine1,
                City = order.City,
                State = order.State,
                Postcode = order.Postcode,
                ExpectedDeliveryDate = order.AppointmentDate,
                Notes = $"Auto-generated delivery order for Order {order.ServiceId}",
                InternalNotes = $"Order: {order.ServiceId}, SI: {si.EmployeeId ?? si.Id.ToString()}",
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<DeliveryOrder>().Add(deliveryOrder);

            // Create delivery order items from order materials
            int lineNumber = 1;
            foreach (var material in orderMaterials)
            {
                var materialEntity = material.Material;
                if (materialEntity == null)
                {
                    _logger.LogWarning("Material {MaterialId} not found for order material usage {UsageId}, skipping", material.MaterialId, material.Id);
                    continue;
                }

                var deliveryOrderItem = new DeliveryOrderItem
                {
                    Id = Guid.NewGuid(),
                    CompanyId = order.CompanyId.Value,
                    DeliveryOrderId = deliveryOrder.Id,
                    MaterialId = material.MaterialId,
                    LineNumber = lineNumber++,
                    Description = materialEntity.Description ?? materialEntity.ItemCode ?? "Material",
                    Sku = materialEntity.ItemCode,
                    Unit = materialEntity.UnitOfMeasure ?? "pcs",
                    Quantity = material.Quantity,
                    QuantityDelivered = 0,
                    SerialNumbers = material.SerialisedItemId.HasValue ? material.SerialisedItemId.ToString() : null,
                    Notes = material.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<DeliveryOrderItem>().Add(deliveryOrderItem);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Create stock movements (warehouse → SI bag) for all materials
            try
            {
                await CreateStockMovementsForDeliveryOrderAsync(deliveryOrder, order, si.Id, userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock movements for delivery order {DeliveryOrderId}, continuing without stock movements", deliveryOrder.Id);
                // Don't throw - delivery order is created, stock movements are optional
            }

            _logger.LogInformation(
                "Created delivery order {DeliveryOrderId} ({DoNumber}) for order {OrderId} with {ItemCount} items assigned to SI {SiId}",
                deliveryOrder.Id, deliveryOrder.DoNumber, order.Id, orderMaterials.Count, order.AssignedSiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery order for order {OrderId}", order.Id);
            // Don't throw - delivery order creation failure shouldn't fail the order assignment
        }
    }

    /// <summary>
    /// Generate a unique delivery order number
    /// Format: DO-YYYYMMDD-XXXX (e.g., DO-20250101-0001)
    /// </summary>
    private async Task<string> GenerateDeliveryOrderNumberAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow;
        var prefix = $"DO-{today:yyyyMMdd}-";
        
        // Get the highest sequence number for today
        var lastDo = await _context.Set<DeliveryOrder>()
            .Where(d => d.CompanyId == companyId && d.DoNumber.StartsWith(prefix))
            .OrderByDescending(d => d.DoNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (lastDo != null && lastDo.DoNumber.StartsWith(prefix))
        {
            var lastSequenceStr = lastDo.DoNumber.Substring(prefix.Length);
            if (int.TryParse(lastSequenceStr, out int lastSequence))
            {
                sequence = lastSequence + 1;
            }
        }

        return $"{prefix}{sequence:D4}";
    }

    /// <summary>
    /// Create stock movements for delivery order items (warehouse → SI bag). Uses legacy CreateStockMovementAsync (ledger-only; do not reintroduce StockBalance.Quantity writes).
    /// </summary>
    private async Task CreateStockMovementsForDeliveryOrderAsync(
        DeliveryOrder deliveryOrder, 
        Order order, 
        Guid siId, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        if (order.CompanyId == null)
        {
            return;
        }

        // Get or create SI bag location
        var siLocationId = await GetOrCreateSiBagLocationAsync(order.CompanyId.Value, siId, cancellationToken);
        if (!siLocationId.HasValue)
        {
            _logger.LogWarning("Could not get or create SI bag location for SI {SiId}, skipping stock movements", siId);
            return;
        }

        // Get default warehouse location (first active warehouse location)
        var warehouseLocationId = await GetDefaultWarehouseLocationAsync(order.CompanyId.Value, cancellationToken);
        if (!warehouseLocationId.HasValue)
        {
            _logger.LogWarning("No warehouse location found for company {CompanyId}, skipping stock movements", order.CompanyId);
            return;
        }

        // Get delivery order items with materials
        var deliveryOrderItems = await _context.Set<DeliveryOrderItem>()
            .Where(di => di.DeliveryOrderId == deliveryOrder.Id && !di.IsDeleted)
            .Include(di => di.Material)
            .ToListAsync(cancellationToken);

        // Get "IssueToSI" movement type
        var issueToSiMovementType = await _context.Set<MovementType>()
            .FirstOrDefaultAsync(mt => mt.CompanyId == order.CompanyId.Value && mt.Code == "IssueToSI" && mt.IsActive, cancellationToken);

        if (issueToSiMovementType == null)
        {
            _logger.LogWarning("IssueToSI movement type not found for company {CompanyId}, skipping stock movements", order.CompanyId);
            return;
        }

        // Update delivery order with source and destination locations
        deliveryOrder.SourceLocationId = warehouseLocationId;
        deliveryOrder.DestinationLocationId = siLocationId;
        await _context.SaveChangesAsync(cancellationToken);

        // Create stock movements for each item
        var movementTypeCode = issueToSiMovementType.Code;
        var movementsCreated = 0;

        foreach (var item in deliveryOrderItems)
        {
            if (item.Material == null) continue;

            try
            {
                // For serialized materials, we need to handle serial numbers differently
                // For now, create movement for the quantity (serialized items will be handled separately)
                var createMovementDto = new CreateStockMovementDto
                {
                    FromLocationId = warehouseLocationId,
                    ToLocationId = siLocationId,
                    MaterialId = item.MaterialId,
                    Quantity = item.Quantity,
                    MovementType = movementTypeCode,
                    OrderId = order.Id,
                    ServiceInstallerId = siId,
                    Remarks = $"Auto-generated from Delivery Order {deliveryOrder.DoNumber} for Order {order.ServiceId}"
                };

#pragma warning disable CS0618 // Legacy path: CreateStockMovementAsync is obsolete; ledger-only. Do not add new call sites.
                var stockMovement = await _inventoryService.CreateStockMovementAsync(
                    createMovementDto, 
                    order.CompanyId.Value, 
                    userId, 
                    cancellationToken);
#pragma warning restore CS0618

                movementsCreated++;

                // Update OrderMaterialUsage with StockMovementId if we can find it
                var materialUsage = await _context.Set<OrderMaterialUsage>()
                    .FirstOrDefaultAsync(om => om.OrderId == order.Id && om.MaterialId == item.MaterialId && !om.IsDeleted, cancellationToken);

                if (materialUsage != null && stockMovement != null)
                {
                    materialUsage.StockMovementId = stockMovement.Id;
                    materialUsage.SourceLocationId = warehouseLocationId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock movement for material {MaterialId} in delivery order {DeliveryOrderId}", item.MaterialId, deliveryOrder.Id);
                // Continue with next item
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {MovementCount} stock movements for delivery order {DeliveryOrderId} (Order {OrderId})",
            movementsCreated, deliveryOrder.Id, order.Id);
    }

    /// <summary>
    /// Get or create SI bag location for a service installer
    /// </summary>
    private async Task<Guid?> GetOrCreateSiBagLocationAsync(Guid companyId, Guid siId, CancellationToken cancellationToken)
    {
        // Find existing SI location
        var existingLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => 
                sl.CompanyId == companyId && 
                sl.LinkedServiceInstallerId == siId && 
                sl.IsActive && 
                !sl.IsDeleted, 
                cancellationToken);

        if (existingLocation != null)
        {
            return existingLocation.Id;
        }

        // Get SI location type
        var siLocationType = await _context.Set<LocationType>()
            .FirstOrDefaultAsync(lt => lt.CompanyId == companyId && lt.Code == "SI" && lt.IsActive, cancellationToken);

        if (siLocationType == null)
        {
            _logger.LogWarning("SI location type not found for company {CompanyId}", companyId);
            return null;
        }

        // Get SI information for location name
        var si = await _context.Set<ServiceInstaller>()
            .FirstOrDefaultAsync(s => s.Id == siId && s.CompanyId == companyId, cancellationToken);

        if (si == null)
        {
            _logger.LogWarning("Service installer {SiId} not found", siId);
            return null;
        }

        // Create new SI location
        var newLocation = new StockLocation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = $"SI Bag - {si.Name ?? si.EmployeeId ?? si.Id.ToString()}",
            Type = siLocationType.Code,
            LocationTypeId = siLocationType.Id,
            LinkedServiceInstallerId = siId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StockLocations.Add(newLocation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created SI bag location {LocationId} for SI {SiId}", newLocation.Id, siId);

        return newLocation.Id;
    }

    /// <summary>
    /// Get default warehouse location (first active warehouse location)
    /// </summary>
    private async Task<Guid?> GetDefaultWarehouseLocationAsync(Guid companyId, CancellationToken cancellationToken)
    {
        // Get Warehouse location type
        var warehouseLocationType = await _context.Set<LocationType>()
            .FirstOrDefaultAsync(lt => lt.CompanyId == companyId && lt.Code == "Warehouse" && lt.IsActive, cancellationToken);

        if (warehouseLocationType == null)
        {
            _logger.LogWarning("Warehouse location type not found for company {CompanyId}", companyId);
            return null;
        }

        // Find first active warehouse location
        var warehouseLocation = await _context.StockLocations
            .FirstOrDefaultAsync(sl => 
                sl.CompanyId == companyId && 
                sl.LocationTypeId == warehouseLocationType.Id && 
                sl.IsActive && 
                !sl.IsDeleted, 
                cancellationToken);

        if (warehouseLocation != null)
        {
            return warehouseLocation.Id;
        }

        // If no warehouse location exists, try to find/create from Warehouse entity
        var warehouse = await _context.Set<Domain.Settings.Entities.Warehouse>()
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.IsActive, cancellationToken);

        if (warehouse == null)
        {
            _logger.LogWarning("No active warehouse found for company {CompanyId}", companyId);
            return null;
        }

        // Create stock location for warehouse
        var newWarehouseLocation = new StockLocation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = warehouse.Name,
            Type = warehouseLocationType.Code,
            LocationTypeId = warehouseLocationType.Id,
            WarehouseId = warehouse.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StockLocations.Add(newWarehouseLocation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created warehouse location {LocationId} for warehouse {WarehouseId}", newWarehouseLocation.Id, warehouse.Id);

        return newWarehouseLocation.Id;
    }

    #endregion
}

