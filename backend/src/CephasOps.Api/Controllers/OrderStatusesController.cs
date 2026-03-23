using CephasOps.Api.Common;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Order status and workflow configuration endpoints
/// </summary>
[ApiController]
[Route("api/order-statuses")]
[Authorize]
public class OrderStatusesController : ControllerBase
{
    private readonly ILogger<OrderStatusesController> _logger;
    private readonly IWorkflowEngineService? _workflowEngineService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ITenantProvider? _tenantProvider;

    // ========================================
    // ORDER WORKFLOW - Main flow (incl. invoice rejection loop)
    // ========================================
    private static readonly List<OrderStatusDto> OrderWorkflowStatuses = new()
    {
        new() { Code = "Pending", Name = "Pending", Description = "Order created via parser/manual/API. Not yet assigned.", Order = 1, Color = "#94a3b8", Icon = "Clock", TriggeredBy = "System", WorkflowType = "Order", Phase = "Creation" },
        new() { Code = "Assigned", Name = "Assigned", Description = "SI assigned and appointment confirmed.", Order = 2, Color = "#3b82f6", Icon = "Users", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "FieldWork" },
        new() { Code = "OnTheWay", Name = "On The Way", Description = "SI is travelling to the site.", Order = 3, Color = "#8b5cf6", Icon = "Truck", TriggeredBy = "SI", WorkflowType = "Order", Phase = "FieldWork" },
        new() { Code = "MetCustomer", Name = "Met Customer", Description = "SI has met the customer on-site.", Order = 4, Color = "#06b6d4", Icon = "Users", TriggeredBy = "SI", WorkflowType = "Order", Phase = "FieldWork" },
        new() { Code = "Blocker", Name = "Blocker", Description = "Job cannot proceed (Customer/Building/Network issue).", Order = 5, Color = "#ef4444", Icon = "AlertTriangle", TriggeredBy = "SI/Admin", WorkflowType = "Order", Phase = "FieldWork" },
        new() { Code = "ReschedulePendingApproval", Name = "Reschedule Pending Approval", Description = "Waiting for TIME approval for reschedule.", Order = 6, Color = "#f59e0b", Icon = "Clock", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "FieldWork" },
        new() { Code = "OrderCompleted", Name = "Order Completed", Description = "Physical job done. Splitter + materials recorded.", Order = 7, Color = "#10b981", Icon = "Wrench", TriggeredBy = "SI", WorkflowType = "Order", Phase = "FieldWork" },
        new() { Code = "DocketsReceived", Name = "Dockets Received", Description = "Admin received docket from SI.", Order = 8, Color = "#14b8a6", Icon = "FileText", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Documentation" },
        new() { Code = "DocketsVerified", Name = "Dockets Verified", Description = "Docket validated and QA passed.", Order = 9, Color = "#0ea5e9", Icon = "CheckCircle", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Documentation" },
        new() { Code = "DocketsRejected", Name = "Dockets Rejected", Description = "Docket rejected. SI must correct and resubmit.", Order = 9, Color = "#ef4444", Icon = "AlertTriangle", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Documentation" },
        new() { Code = "DocketsUploaded", Name = "Dockets Uploaded", Description = "Docket uploaded to partner portal.", Order = 10, Color = "#6366f1", Icon = "FileText", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Documentation" },
        new() { Code = "ReadyForInvoice", Name = "Ready For Invoice", Description = "All validations passed. Ready for billing.", Order = 11, Color = "#8b5cf6", Icon = "Receipt", TriggeredBy = "System", WorkflowType = "Order", Phase = "Billing" },
        new() { Code = "Invoiced", Name = "Invoiced", Description = "Invoice generated. Due date = upload + 45 days.", Order = 12, Color = "#a855f7", Icon = "Receipt", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Billing" },
        new() { Code = "SubmittedToPortal", Name = "Submitted To Portal", Description = "Invoice uploaded to partner portal (e.g. TIME).", Order = 13, Color = "#d946ef", Icon = "FileText", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Billing" },
        new() { Code = "Rejected", Name = "Invoice Rejected", Description = "TIME rejected the invoice. Path 1: regenerate (→ ReadyForInvoice). Path 2: correct in portal (→ Reinvoice).", Order = 14, Color = "#ef4444", Icon = "AlertTriangle", TriggeredBy = "System/Admin", WorkflowType = "Order", Phase = "Billing" },
        new() { Code = "Reinvoice", Name = "Reinvoice", Description = "Admin correcting in partner portal. After correction → Invoiced.", Order = 15, Color = "#f59e0b", Icon = "RefreshCw", TriggeredBy = "Admin", WorkflowType = "Order", Phase = "Billing" },
        new() { Code = "Completed", Name = "Completed", Description = "Payment received. Order fully closed.", Order = 16, Color = "#22c55e", Icon = "CheckCircle", TriggeredBy = "System", WorkflowType = "Order", Phase = "Closure" },
        new() { Code = "Cancelled", Name = "Cancelled", Description = "Order cancelled. Cannot be reactivated.", Order = 17, Color = "#64748b", Icon = "Ban", TriggeredBy = "Admin/System", WorkflowType = "Order", Phase = "Closure" }
    };

    // ========================================
    // RMA WORKFLOW - Material Return Authorization
    // ========================================
    private static readonly List<OrderStatusDto> RmaWorkflowStatuses = new()
    {
        new() { Code = "RMARequested", Name = "RMA Requested", Description = "Faulty device identified. RMA request created.", Order = 1, Color = "#f97316", Icon = "AlertTriangle", TriggeredBy = "Admin/SI", WorkflowType = "RMA", Phase = "Initiation" },
        new() { Code = "RMAPendingReview", Name = "Pending Review", Description = "Admin reviewing RMA request and device serials.", Order = 2, Color = "#eab308", Icon = "Eye", TriggeredBy = "Admin", WorkflowType = "RMA", Phase = "Review" },
        new() { Code = "RMAMraReceived", Name = "MRA Document Received", Description = "Partner MRA email/PDF received and attached.", Order = 3, Color = "#84cc16", Icon = "FileText", TriggeredBy = "System/Admin", WorkflowType = "RMA", Phase = "Review" },
        new() { Code = "RMAApproved", Name = "RMA Approved", Description = "RMA approved. Ready for shipment to partner.", Order = 4, Color = "#22c55e", Icon = "CheckCircle", TriggeredBy = "Admin", WorkflowType = "RMA", Phase = "Processing" },
        new() { Code = "RMAInTransit", Name = "In Transit to Partner", Description = "Faulty devices shipped to partner for repair/replacement.", Order = 5, Color = "#3b82f6", Icon = "Truck", TriggeredBy = "Warehouse", WorkflowType = "RMA", Phase = "Processing" },
        new() { Code = "RMAAtPartner", Name = "At Partner", Description = "Devices received by partner. Awaiting resolution.", Order = 6, Color = "#8b5cf6", Icon = "Building", TriggeredBy = "Partner", WorkflowType = "RMA", Phase = "Processing" },
        new() { Code = "RMARepaired", Name = "Repaired", Description = "Device repaired and returned to warehouse.", Order = 7, Color = "#10b981", Icon = "Wrench", TriggeredBy = "Partner", WorkflowType = "RMA", Phase = "Resolution" },
        new() { Code = "RMAReplaced", Name = "Replaced", Description = "Device replaced with new unit.", Order = 8, Color = "#06b6d4", Icon = "RefreshCw", TriggeredBy = "Partner", WorkflowType = "RMA", Phase = "Resolution" },
        new() { Code = "RMACredited", Name = "Credited", Description = "Credit note issued by partner.", Order = 9, Color = "#a855f7", Icon = "CreditCard", TriggeredBy = "Partner", WorkflowType = "RMA", Phase = "Resolution" },
        new() { Code = "RMAScrapped", Name = "Scrapped", Description = "Device scrapped. Warranty void or beyond repair.", Order = 10, Color = "#64748b", Icon = "Trash2", TriggeredBy = "Admin/Partner", WorkflowType = "RMA", Phase = "Resolution" },
        new() { Code = "RMAClosed", Name = "RMA Closed", Description = "RMA process completed. All records updated.", Order = 11, Color = "#22c55e", Icon = "CheckCircle", TriggeredBy = "Admin", WorkflowType = "RMA", Phase = "Closure" }
    };

    // ========================================
    // KPI WORKFLOW - Performance Tracking
    // ========================================
    private static readonly List<OrderStatusDto> KpiWorkflowStatuses = new()
    {
        // SI KPI Statuses
        new() { Code = "KpiPending", Name = "KPI Pending", Description = "Job in progress. KPI not yet calculated.", Order = 1, Color = "#94a3b8", Icon = "Clock", TriggeredBy = "System", WorkflowType = "KPI", Phase = "SI Performance", KpiCategory = "SI" },
        new() { Code = "KpiOnTime", Name = "On Time", Description = "SI completed job within SLA window.", Order = 2, Color = "#22c55e", Icon = "CheckCircle", TriggeredBy = "System", WorkflowType = "KPI", Phase = "SI Performance", KpiCategory = "SI" },
        new() { Code = "KpiLate", Name = "Late", Description = "SI completed job but exceeded SLA. Minor breach.", Order = 3, Color = "#f59e0b", Icon = "Clock", TriggeredBy = "System", WorkflowType = "KPI", Phase = "SI Performance", KpiCategory = "SI" },
        new() { Code = "KpiExceededSla", Name = "Exceeded SLA", Description = "SI significantly exceeded SLA. Major breach.", Order = 4, Color = "#ef4444", Icon = "AlertTriangle", TriggeredBy = "System", WorkflowType = "KPI", Phase = "SI Performance", KpiCategory = "SI" },
        new() { Code = "KpiExcused", Name = "Excused", Description = "KPI breach excused due to valid reason (blocker/reschedule).", Order = 5, Color = "#3b82f6", Icon = "Shield", TriggeredBy = "Admin", WorkflowType = "KPI", Phase = "SI Performance", KpiCategory = "SI" },
        
        // Admin KPI Statuses
        new() { Code = "KpiDocketPending", Name = "Docket KPI Pending", Description = "Docket received. Processing time KPI started.", Order = 6, Color = "#94a3b8", Icon = "FileText", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Admin Performance", KpiCategory = "Admin" },
        new() { Code = "KpiDocketOnTime", Name = "Docket On Time", Description = "Admin processed docket within SLA.", Order = 7, Color = "#22c55e", Icon = "CheckCircle", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Admin Performance", KpiCategory = "Admin" },
        new() { Code = "KpiDocketLate", Name = "Docket Late", Description = "Admin processed docket but exceeded SLA.", Order = 8, Color = "#f59e0b", Icon = "Clock", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Admin Performance", KpiCategory = "Admin" },
        new() { Code = "KpiInvoicePending", Name = "Invoice KPI Pending", Description = "Ready for invoice. Billing KPI started.", Order = 9, Color = "#94a3b8", Icon = "Receipt", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Admin Performance", KpiCategory = "Admin" },
        new() { Code = "KpiInvoiceOnTime", Name = "Invoice On Time", Description = "Invoice generated within SLA window.", Order = 10, Color = "#22c55e", Icon = "CheckCircle", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Admin Performance", KpiCategory = "Admin" },
        new() { Code = "KpiInvoiceLate", Name = "Invoice Late", Description = "Invoice generated but exceeded SLA.", Order = 11, Color = "#f59e0b", Icon = "Clock", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Admin Performance", KpiCategory = "Admin" },
        
        // Employer/Company KPI
        new() { Code = "KpiEmployerPending", Name = "Employer Review Pending", Description = "SI performance pending employer review.", Order = 12, Color = "#94a3b8", Icon = "Users", TriggeredBy = "System", WorkflowType = "KPI", Phase = "Employer Review", KpiCategory = "Employer" },
        new() { Code = "KpiEmployerApproved", Name = "Employer Approved", Description = "SI performance approved by employer.", Order = 13, Color = "#22c55e", Icon = "ThumbsUp", TriggeredBy = "Employer", WorkflowType = "KPI", Phase = "Employer Review", KpiCategory = "Employer" },
        new() { Code = "KpiEmployerFlagged", Name = "Employer Flagged", Description = "SI performance flagged for review/action.", Order = 14, Color = "#ef4444", Icon = "Flag", TriggeredBy = "Employer", WorkflowType = "KPI", Phase = "Employer Review", KpiCategory = "Employer" }
    };

    // Combined list for backwards compatibility
    private static List<OrderStatusDto> AllStatuses => OrderWorkflowStatuses
        .Concat(RmaWorkflowStatuses)
        .Concat(KpiWorkflowStatuses)
        .ToList();

    public OrderStatusesController(
        ILogger<OrderStatusesController> logger,
        IWorkflowEngineService? workflowEngineService = null,
        ICurrentUserService? currentUserService = null,
        ITenantProvider? tenantProvider = null)
    {
        _logger = logger;
        _workflowEngineService = workflowEngineService;
        _currentUserService = currentUserService;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get all statuses (all workflow types)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<OrderStatusDto>>> GetOrderStatuses([FromQuery] string? workflowType = null)
    {
        _logger.LogInformation("Getting order statuses. WorkflowType filter: {WorkflowType}", workflowType);
        
        if (string.IsNullOrEmpty(workflowType))
        {
            return this.Success(AllStatuses);
        }
        
        var filtered = AllStatuses.Where(s => s.WorkflowType.Equals(workflowType, StringComparison.OrdinalIgnoreCase)).ToList();
        return this.Success(filtered);
    }

    /// <summary>
    /// Get available workflow types
    /// </summary>
    [HttpGet("workflow-types")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowTypeDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<WorkflowTypeDto>>> GetWorkflowTypes()
    {
        var workflowTypes = new List<WorkflowTypeDto>
        {
            new() { Code = "Order", Name = "Order Workflow", Description = "Main order lifecycle from creation to payment", StatusCount = OrderWorkflowStatuses.Count, Color = "#3b82f6" },
            new() { Code = "RMA", Name = "RMA Workflow", Description = "Material Return Authorization process", StatusCount = RmaWorkflowStatuses.Count, Color = "#f97316" },
            new() { Code = "KPI", Name = "KPI Workflow", Description = "Performance tracking for SI, Admin, and Employer", StatusCount = KpiWorkflowStatuses.Count, Color = "#22c55e" }
        };
        return this.Success(workflowTypes);
    }

    /// <summary>
    /// Get order status by code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<OrderStatusDto>> GetOrderStatus(string code)
    {
        var status = AllStatuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (status == null)
        {
            return this.NotFound<OrderStatusDto>($"Order status '{code}' not found");
        }
        return this.Success(status);
    }

    /// <summary>
    /// Get order statuses for a specific phase
    /// </summary>
    [HttpGet("phase/{phase}")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusDto>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<OrderStatusDto>>> GetOrderStatusesByPhase(string phase)
    {
        var statuses = AllStatuses.Where(s => s.Phase.Equals(phase, StringComparison.OrdinalIgnoreCase)).ToList();
        return this.Success(statuses);
    }

    /// <summary>
    /// Get valid next statuses from a given status
    /// 
    /// If entityId and entityType are provided, this endpoint delegates to the workflow engine
    /// to get actual allowed transitions (with guard conditions and role checks).
    /// Otherwise, returns hardcoded transitions for backward compatibility.
    /// 
    /// For actual allowed transitions with full validation, use:
    /// GET /api/workflow/allowed-transitions?entityType=Order&amp;entityId={orderId}&amp;currentStatus={status}
    /// </summary>
    [HttpGet("{code}/transitions")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderStatusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<OrderStatusDto>>>> GetValidTransitions(
        string code,
        [FromQuery] Guid? entityId = null,
        [FromQuery] string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var current = AllStatuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (current == null)
        {
            return this.Success(new List<OrderStatusDto>());
        }

        // If entityId and entityType are provided, delegate to workflow engine for actual allowed transitions
        if (entityId.HasValue && !string.IsNullOrEmpty(entityType) && _workflowEngineService != null && _currentUserService != null && _tenantProvider != null)
        {
            try
            {
                var (companyId, err) = this.RequireCompanyId(_tenantProvider);
                if (err != null) return err;
                var userRoles = _currentUserService.Roles ?? new List<string>();

                var allowedTransitions = await _workflowEngineService.GetAllowedTransitionsAsync(
                    companyId,
                    entityType,
                    entityId.Value,
                    code,
                    userRoles,
                    cancellationToken);

                // Map WorkflowTransitionDto.ToStatus to OrderStatusDto
                var validStatusCodes = allowedTransitions
                    .Select(t => t.ToStatus)
                    .Distinct()
                    .ToList();

                var workflowValidStatuses = AllStatuses
                    .Where(s => validStatusCodes.Contains(s.Code))
                    .OrderBy(s => s.Order)
                    .ToList();

                _logger.LogInformation(
                    "Retrieved {Count} allowed transitions from workflow engine for status {Code}, entityType: {EntityType}, entityId: {EntityId}",
                    workflowValidStatuses.Count, code, entityType, entityId);

                return this.Success(workflowValidStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error getting allowed transitions from workflow engine for status {Code}, entityType: {EntityType}, entityId: {EntityId}. Falling back to hardcoded transitions.",
                    code, entityType, entityId);
                // Fall through to hardcoded transitions
            }
        }

        // Fallback: Hardcoded transitions for backward compatibility or when entity context is not available
        // ⚠️ NOTE: These may not match actual workflow engine settings if workflow definitions have been customized.
        var validNextCodes = code switch
        {
            // Order Workflow transitions
            "Pending" => new[] { "Assigned", "Cancelled" },
            "Assigned" => new[] { "OnTheWay", "Blocker", "Cancelled", "ReschedulePendingApproval" },
            "OnTheWay" => new[] { "MetCustomer", "Blocker" },
            "MetCustomer" => new[] { "OrderCompleted", "Blocker" },
            "Blocker" => new[] { "Assigned", "MetCustomer", "ReschedulePendingApproval", "Cancelled" },
            "ReschedulePendingApproval" => new[] { "Assigned", "Cancelled" },
            "OrderCompleted" => new[] { "DocketsReceived" },
            "DocketsReceived" => new[] { "DocketsVerified", "DocketsRejected" },
            "DocketsVerified" => new[] { "DocketsUploaded" },
            "DocketsRejected" => new[] { "DocketsReceived" },
            "DocketsUploaded" => new[] { "ReadyForInvoice" },
            "ReadyForInvoice" => new[] { "Invoiced" },
            "Invoiced" => new[] { "SubmittedToPortal", "Rejected" },
            "SubmittedToPortal" => new[] { "Completed", "Rejected" },
            "Rejected" => new[] { "ReadyForInvoice", "Reinvoice" },
            "Reinvoice" => new[] { "Invoiced" },
            "Completed" => Array.Empty<string>(),
            "Cancelled" => Array.Empty<string>(),
            
            // RMA Workflow transitions
            "RMARequested" => new[] { "RMAPendingReview" },
            "RMAPendingReview" => new[] { "RMAMraReceived", "RMAApproved" },
            "RMAMraReceived" => new[] { "RMAApproved" },
            "RMAApproved" => new[] { "RMAInTransit" },
            "RMAInTransit" => new[] { "RMAAtPartner" },
            "RMAAtPartner" => new[] { "RMARepaired", "RMAReplaced", "RMACredited", "RMAScrapped" },
            "RMARepaired" => new[] { "RMAClosed" },
            "RMAReplaced" => new[] { "RMAClosed" },
            "RMACredited" => new[] { "RMAClosed" },
            "RMAScrapped" => new[] { "RMAClosed" },
            "RMAClosed" => Array.Empty<string>(),
            
            // KPI transitions (mostly system-driven)
            "KpiPending" => new[] { "KpiOnTime", "KpiLate", "KpiExceededSla", "KpiExcused" },
            "KpiOnTime" => Array.Empty<string>(),
            "KpiLate" => new[] { "KpiExcused" },
            "KpiExceededSla" => new[] { "KpiExcused" },
            "KpiExcused" => Array.Empty<string>(),
            
            _ => Array.Empty<string>()
        };

        var validStatuses = AllStatuses.Where(s => validNextCodes.Contains(s.Code)).ToList();
        return this.Success(validStatuses);
    }

    /// <summary>
    /// Create a new order status
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse<OrderStatusDto>> CreateOrderStatus([FromBody] CreateOrderStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return this.Error<OrderStatusDto>("Code and Name are required", 400);
        }

        // Check for duplicate code
        if (AllStatuses.Any(s => s.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase)))
        {
            return this.Error<OrderStatusDto>($"Order status with code '{dto.Code}' already exists", 400);
        }

        var targetList = dto.WorkflowType?.ToLower() switch
        {
            "rma" => RmaWorkflowStatuses,
            "kpi" => KpiWorkflowStatuses,
            _ => OrderWorkflowStatuses
        };

        var newStatus = new OrderStatusDto
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Order = dto.Order > 0 ? dto.Order : targetList.Max(s => s.Order) + 1,
            Color = dto.Color ?? "#3b82f6",
            Icon = dto.Icon ?? "Clock",
            TriggeredBy = dto.TriggeredBy ?? "Admin",
            WorkflowType = dto.WorkflowType ?? "Order",
            Phase = dto.Phase ?? "Custom",
            KpiCategory = dto.KpiCategory,
            CanEdit = true,
            CanDelete = true
        };

        targetList.Add(newStatus);
        _logger.LogInformation("Created order status: {Code} in workflow {Workflow}", newStatus.Code, newStatus.WorkflowType);

        return this.StatusCode(201, ApiResponse<OrderStatusDto>.SuccessResponse(newStatus, "Order status created successfully"));
    }

    /// <summary>
    /// Update an existing order status
    /// </summary>
    [HttpPut("{code}")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderStatusDto>), StatusCodes.Status404NotFound)]
    public ActionResult<ApiResponse<OrderStatusDto>> UpdateOrderStatus(string code, [FromBody] UpdateOrderStatusDto dto)
    {
        var status = AllStatuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (status == null)
        {
            return this.NotFound<OrderStatusDto>($"Order status '{code}' not found");
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(dto.Name))
            status.Name = dto.Name;
        if (dto.Description != null)
            status.Description = dto.Description;
        if (dto.Order.HasValue)
            status.Order = dto.Order.Value;
        if (!string.IsNullOrWhiteSpace(dto.Color))
            status.Color = dto.Color;
        if (!string.IsNullOrWhiteSpace(dto.Icon))
            status.Icon = dto.Icon;
        if (!string.IsNullOrWhiteSpace(dto.TriggeredBy))
            status.TriggeredBy = dto.TriggeredBy;
        if (!string.IsNullOrWhiteSpace(dto.Phase))
            status.Phase = dto.Phase;

        _logger.LogInformation("Updated order status: {Code}", code);
        return this.Success(status, "Order status updated successfully");
    }

    /// <summary>
    /// Delete an order status
    /// </summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<ApiResponse> DeleteOrderStatus(string code)
    {
        var status = AllStatuses.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        if (status == null)
        {
            return StatusCode(404, ApiResponse.ErrorResponse($"Order status '{code}' not found"));
        }

        if (!status.CanDelete)
        {
            return StatusCode(400, ApiResponse.ErrorResponse($"Order status '{code}' cannot be deleted (system status)"));
        }

        // Remove from appropriate list
        OrderWorkflowStatuses.Remove(status);
        RmaWorkflowStatuses.Remove(status);
        KpiWorkflowStatuses.Remove(status);
        
        _logger.LogInformation("Deleted order status: {Code}", code);

        return this.StatusCode(204, ApiResponse.SuccessResponse("Order status deleted successfully"));
    }
}

/// <summary>
/// Order status DTO
/// </summary>
public class OrderStatusDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public string Icon { get; set; } = "Clock";
    public string TriggeredBy { get; set; } = "Admin";
    public string WorkflowType { get; set; } = "Order"; // Order, RMA, KPI
    public string Phase { get; set; } = "Custom";
    public string? KpiCategory { get; set; } // SI, Admin, Employer (for KPI workflow)
    public bool CanEdit { get; set; } = true;
    public bool CanDelete { get; set; } = false;
}

/// <summary>
/// Workflow type DTO
/// </summary>
public class WorkflowTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusCount { get; set; }
    public string Color { get; set; } = "#3b82f6";
}

/// <summary>
/// DTO for creating a new order status
/// </summary>
public class CreateOrderStatusDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public string? TriggeredBy { get; set; }
    public string? WorkflowType { get; set; }
    public string? Phase { get; set; }
    public string? KpiCategory { get; set; }
}

/// <summary>
/// DTO for updating an order status
/// </summary>
public class UpdateOrderStatusDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Order { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public string? TriggeredBy { get; set; }
    public string? Phase { get; set; }
}
