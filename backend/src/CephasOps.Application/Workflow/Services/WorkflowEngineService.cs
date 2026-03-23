using CephasOps.Application.Audit.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Events;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow;
using CephasOps.Application.Scheduler.Services;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Service for executing workflow transitions and managing workflow jobs
/// Uses registries for guard conditions and side effects - fully settings-driven, no hardcoding
/// </summary>
public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowEngineService> _logger;
    private readonly IWorkflowDefinitionsService _workflowDefinitionsService;
    private readonly IOrderPricingContextResolver _orderPricingContextResolver;
    private readonly IEffectiveScopeResolver _effectiveScopeResolver;
    private readonly GuardConditionValidatorRegistry _guardConditionValidatorRegistry;
    private readonly SideEffectExecutorRegistry _sideEffectExecutorRegistry;
    private readonly ISchedulerService _schedulerService;
    private readonly IAuditLogService? _auditLogService;
    private readonly IEventStore? _eventStore;
    private readonly IPlatformEventEnvelopeBuilder? _envelopeBuilder;

    public WorkflowEngineService(
        ApplicationDbContext context,
        ILogger<WorkflowEngineService> logger,
        IWorkflowDefinitionsService workflowDefinitionsService,
        IOrderPricingContextResolver orderPricingContextResolver,
        IEffectiveScopeResolver effectiveScopeResolver,
        GuardConditionValidatorRegistry guardConditionValidatorRegistry,
        SideEffectExecutorRegistry sideEffectExecutorRegistry,
        ISchedulerService schedulerService,
        IAuditLogService? auditLogService = null,
        IEventStore? eventStore = null,
        IPlatformEventEnvelopeBuilder? envelopeBuilder = null)
    {
        _context = context;
        _logger = logger;
        _workflowDefinitionsService = workflowDefinitionsService;
        _orderPricingContextResolver = orderPricingContextResolver;
        _effectiveScopeResolver = effectiveScopeResolver;
        _guardConditionValidatorRegistry = guardConditionValidatorRegistry;
        _sideEffectExecutorRegistry = sideEffectExecutorRegistry;
        _schedulerService = schedulerService;
        _auditLogService = auditLogService;
        _eventStore = eventStore;
        _envelopeBuilder = envelopeBuilder;
    }

    /// <summary>
    /// Resolves PartnerId, DepartmentId, OrderTypeCode for workflow definition lookup.
    /// For Order: uses shared order pricing context resolver (company-scoped). For other entity types: uses scope resolver.
    /// DTO overrides (when provided) take precedence for ExecuteTransitionAsync.
    /// </summary>
    private async Task<(Guid? PartnerId, Guid? DepartmentId, string? OrderTypeCode)> ResolveWorkflowScopeAsync(
        Guid companyId,
        string entityType,
        Guid entityId,
        Guid? dtoPartnerId,
        Guid? dtoDepartmentId,
        string? dtoOrderTypeCode,
        CancellationToken cancellationToken)
    {
        if (string.Equals(entityType, "Order", StringComparison.OrdinalIgnoreCase))
        {
            var ctx = await _orderPricingContextResolver.ResolveFromOrderAsync(entityId, companyId, cancellationToken);
            return (dtoPartnerId ?? ctx?.PartnerId, dtoDepartmentId ?? ctx?.DepartmentId, dtoOrderTypeCode ?? ctx?.OrderTypeCode);
        }

        var scope = await _effectiveScopeResolver.ResolveFromEntityAsync(entityType, entityId, cancellationToken);
        return (dtoPartnerId ?? scope?.PartnerId, dtoDepartmentId ?? scope?.DepartmentId, dtoOrderTypeCode ?? scope?.OrderTypeCode);
    }

    public async Task<WorkflowJobDto> ExecuteTransitionAsync(
        Guid companyId,
        ExecuteTransitionDto dto,
        Guid? initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing workflow transition for company {CompanyId}, entityType: {EntityType}, entityId: {EntityId}, targetStatus: {TargetStatus}",
            companyId, dto.EntityType, dto.EntityId, dto.TargetStatus);

        // Resolve workflow context: use DTO when provided, else from shared order pricing context (Order) or scope resolver (other entity types)
        var (partnerId, departmentId, orderTypeCode) = await ResolveWorkflowScopeAsync(companyId, dto.EntityType, dto.EntityId, dto.PartnerId, dto.DepartmentId, dto.OrderTypeCode, cancellationToken);

        // Get effective workflow definition (priority: Partner → Department → OrderType → General)
        var workflowDefinition = await _workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(
            companyId, dto.EntityType, partnerId, departmentId, orderTypeCode, cancellationToken);

        if (workflowDefinition == null)
        {
            throw new InvalidOperationException($"No active workflow definition found for entity type '{dto.EntityType}'.");
        }

        // Get current entity status (this is entity-specific and needs to be implemented per entity type)
        var currentStatus = await GetCurrentEntityStatusAsync(dto.EntityType, dto.EntityId, companyId, cancellationToken);

        // SI workflow hardening: reject duplicate/no-op transitions (no job created, no side effects)
        if (string.Equals(dto.EntityType, SiWorkflowGuard.OrderEntityType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(currentStatus, dto.TargetStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Order is already in the requested status. Duplicate or no-op transition is not allowed.");
        }

        // SI workflow hardening: enforce canonical order transitions (defense-in-depth over DB)
        if (string.Equals(dto.EntityType, SiWorkflowGuard.OrderEntityType, StringComparison.OrdinalIgnoreCase))
        {
            SiWorkflowGuard.RequireValidOrderTransition(currentStatus, dto.TargetStatus, "Order workflow");
            // Reschedule/issue: require reason for audit trail
            if (string.Equals(dto.TargetStatus, OrderStatus.ReschedulePendingApproval, StringComparison.OrdinalIgnoreCase))
            {
                var reason = dto.Payload?.GetValueOrDefault("reason")?.ToString();
                SiWorkflowGuard.RequireRescheduleReason(reason, "Order workflow");
            }
        }

        // Find allowed transition (source of truth: WorkflowTransitions, seeded by 07_gpon_order_workflow.sql)
        var transition = workflowDefinition.Transitions
            .FirstOrDefault(t => t.IsActive
                && (t.FromStatus == null || t.FromStatus == currentStatus)
                && t.ToStatus == dto.TargetStatus);

        if (transition == null)
        {
            var allowedNext = workflowDefinition.Transitions
                .Where(t => t.IsActive && (t.FromStatus == null || t.FromStatus == currentStatus))
                .Select(t => t.ToStatus)
                .Distinct()
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            throw new InvalidWorkflowTransitionException(
                currentStatus,
                dto.TargetStatus,
                allowedNext,
                dto.EntityType);
        }

        // Create workflow job
        var job = new WorkflowJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkflowDefinitionId = workflowDefinition.Id,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            CurrentStatus = currentStatus,
            TargetStatus = dto.TargetStatus,
            State = WorkflowJobState.Pending,
            PayloadJson = dto.Payload != null ? JsonSerializer.Serialize(dto.Payload) : null,
            InitiatedByUserId = initiatedByUserId,
            CorrelationId = dto.CorrelationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkflowJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created workflow job {JobId} for transition", job.Id);

        // Execute the workflow (validate guards, apply side effects)
        try
        {
            job.State = WorkflowJobState.Running;
            job.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Get company ID from entity (needed for registry lookups)
            var entityCompanyId = await GetEntityCompanyIdAsync(dto.EntityType, dto.EntityId, companyId, cancellationToken);
            if (!entityCompanyId.HasValue)
            {
                throw new InvalidOperationException($"Could not determine company ID for entity {dto.EntityType}/{dto.EntityId}");
            }

            // Validate guard conditions using registry (settings-driven, no hardcoding)
            await ValidateGuardConditionsAsync(transition, dto.EntityType, dto.EntityId, entityCompanyId.Value, cancellationToken);

            // Enrich payload so side effects (e.g. OrderStatusLog) always have initiator and installer when available
            var payloadForSideEffects = await EnrichPayloadForSideEffectsAsync(dto.Payload, dto.EntityType, dto.EntityId, entityCompanyId.Value, initiatedByUserId, cancellationToken);

            // Execute side effects using registry (settings-driven, no hardcoding)
            await ExecuteSideEffectsAsync(transition, dto.EntityType, dto.EntityId, entityCompanyId.Value, payloadForSideEffects, cancellationToken);

            // Update entity status (entity-specific implementation needed)
            await UpdateEntityStatusAsync(dto.EntityType, dto.EntityId, dto.TargetStatus, entityCompanyId, cancellationToken);

            // Audit: log order status change (who did what, when)
            if (dto.EntityType.Equals("Order", StringComparison.OrdinalIgnoreCase) && _auditLogService != null)
            {
                var fieldChangesJson = JsonSerializer.Serialize(new[] { new { field = "Status", oldValue = currentStatus, newValue = dto.TargetStatus } });
                await _auditLogService.LogAuditAsync(
                    entityCompanyId,
                    initiatedByUserId,
                    "Order",
                    dto.EntityId,
                    "StatusChanged",
                    fieldChangesJson,
                    "Api",
                    null,
                    null,
                    cancellationToken);
            }

            // Notifications for order status changes are driven by OrderStatusChangedEvent (Phase 2).
            // No inline notification call here; handler(s) registered for OrderStatusChangedEvent create
            // notification dispatch work and a dedicated worker performs delivery.

            // Stage domain events in same transaction (Phase 4 outbox-style). Dispatcher worker will process after commit.
            // Phase 8: pass platform envelope so RootEventId, PartitionKey, SourceService, SourceModule, CapturedAtUtc, Priority flow.
            if (_eventStore != null)
            {
                var evt = new WorkflowTransitionCompletedEvent
                {
                    EventId = Guid.NewGuid(),
                    OccurredAtUtc = DateTime.UtcNow,
                    CorrelationId = dto.CorrelationId ?? job.CorrelationId,
                    CompanyId = companyId,
                    TriggeredByUserId = initiatedByUserId,
                    WorkflowDefinitionId = workflowDefinition.Id,
                    WorkflowTransitionId = transition.Id,
                    WorkflowJobId = job.Id,
                    FromStatus = currentStatus,
                    ToStatus = dto.TargetStatus,
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId
                };
                evt.RootEventId = evt.EventId;
                var envelope = _envelopeBuilder?.Build(evt);
                _eventStore.AppendInCurrentTransaction(evt, envelope);

                if (dto.EntityType.Equals("Order", StringComparison.OrdinalIgnoreCase))
                {
                    var orderEvt = new OrderStatusChangedEvent
                    {
                        EventId = Guid.NewGuid(),
                        OccurredAtUtc = evt.OccurredAtUtc,
                        CompanyId = companyId,
                        TriggeredByUserId = initiatedByUserId,
                        OrderId = dto.EntityId,
                        PreviousStatus = currentStatus,
                        NewStatus = dto.TargetStatus
                    };
                    EventLineageHelper.SetLineageFrom(orderEvt, evt);
                    var orderEnvelope = _envelopeBuilder?.Build(orderEvt);
                    _eventStore.AppendInCurrentTransaction(orderEvt, orderEnvelope);

                    if (string.Equals(dto.TargetStatus, "Assigned", StringComparison.OrdinalIgnoreCase))
                    {
                        var assignedEvt = new OrderAssignedEvent
                        {
                            EventId = Guid.NewGuid(),
                            OccurredAtUtc = evt.OccurredAtUtc,
                            CompanyId = companyId,
                            TriggeredByUserId = initiatedByUserId,
                            OrderId = dto.EntityId,
                            WorkflowJobId = job.Id
                        };
                        EventLineageHelper.SetLineageFrom(assignedEvt, orderEvt);
                        var assignedEnvelope = _envelopeBuilder?.Build(assignedEvt);
                        _eventStore.AppendInCurrentTransaction(assignedEvt, assignedEnvelope);
                    }
                    if (string.Equals(dto.TargetStatus, "OrderCompleted", StringComparison.OrdinalIgnoreCase) || string.Equals(dto.TargetStatus, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        var completedEvt = new OrderCompletedEvent
                        {
                            EventId = Guid.NewGuid(),
                            OccurredAtUtc = evt.OccurredAtUtc,
                            CompanyId = companyId,
                            TriggeredByUserId = initiatedByUserId,
                            OrderId = dto.EntityId,
                            WorkflowJobId = job.Id
                        };
                        EventLineageHelper.SetLineageFrom(completedEvt, orderEvt);
                        var completedEnvelope = _envelopeBuilder?.Build(completedEvt);
                        _eventStore.AppendInCurrentTransaction(completedEvt, completedEnvelope);
                    }
                }
            }

            job.State = WorkflowJobState.Succeeded;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully executed workflow transition for entity {EntityType}/{EntityId}", dto.EntityType, dto.EntityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow transition for entity {EntityType}/{EntityId}", dto.EntityType, dto.EntityId);

            job.State = WorkflowJobState.Failed;
            job.LastError = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            throw;
        }

        return MapJobToDto(job);
    }

    public async Task<List<WorkflowTransitionDto>> GetAllowedTransitionsAsync(
        Guid companyId,
        string entityType,
        Guid entityId,
        string currentStatus,
        List<string> userRoles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting allowed transitions for company {CompanyId}, entityType: {EntityType}, entityId: {EntityId}, currentStatus: {CurrentStatus}",
            companyId, entityType, entityId, currentStatus);

        // Resolve scope via shared order pricing context (Order) or scope resolver (other)
        var (partnerId, departmentId, orderTypeCode) = await ResolveWorkflowScopeAsync(companyId, entityType, entityId, null, null, null, cancellationToken);
        var workflowDefinition = await _workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(
            companyId, entityType, partnerId, departmentId, orderTypeCode, cancellationToken);

        if (workflowDefinition == null)
        {
            return new List<WorkflowTransitionDto>();
        }

        var allowedTransitions = workflowDefinition.Transitions
            .Where(t => t.IsActive
                && (t.FromStatus == null || t.FromStatus == currentStatus)
                && (t.AllowedRoles.Count == 0 || t.AllowedRoles.Any(role => userRoles.Contains(role))))
            .OrderBy(t => t.DisplayOrder)
            .ToList();

        return allowedTransitions;
    }

    public async Task<bool> CanTransitionAsync(
        Guid companyId,
        string entityType,
        Guid entityId,
        string fromStatus,
        string toStatus,
        List<string> userRoles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if transition is allowed for company {CompanyId}, entityType: {EntityType}, entityId: {EntityId}, from: {FromStatus}, to: {ToStatus}",
            companyId, entityType, entityId, fromStatus, toStatus);

        // Resolve scope via shared order pricing context (Order) or scope resolver (other)
        var (partnerId, departmentId, orderTypeCode) = await ResolveWorkflowScopeAsync(companyId, entityType, entityId, null, null, null, cancellationToken);
        var workflowDefinition = await _workflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync(
            companyId, entityType, partnerId, departmentId, orderTypeCode, cancellationToken);

        if (workflowDefinition == null)
        {
            return false;
        }

        var transition = workflowDefinition.Transitions
            .FirstOrDefault(t => t.IsActive
                && (t.FromStatus == null || t.FromStatus == fromStatus)
                && t.ToStatus == toStatus
                && (t.AllowedRoles.Count == 0 || t.AllowedRoles.Any(role => userRoles.Contains(role))));

        return transition != null;
    }

    public async Task<WorkflowJobDto?> GetWorkflowJobAsync(
        Guid companyId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting workflow job {JobId} for company {CompanyId}", jobId, companyId);

        var job = await _context.WorkflowJobs
            .Include(wj => wj.WorkflowDefinition)
            .FirstOrDefaultAsync(wj => wj.Id == jobId && wj.CompanyId == companyId, cancellationToken);

        return job != null ? MapJobToDto(job) : null;
    }

    public async Task<List<WorkflowJobDto>> GetWorkflowJobsAsync(
        Guid companyId,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting workflow jobs for company {CompanyId}, entityType: {EntityType}, entityId: {EntityId}",
            companyId, entityType, entityId);

        var jobs = await _context.WorkflowJobs
            .Include(wj => wj.WorkflowDefinition)
            .Where(wj => wj.CompanyId == companyId
                && wj.EntityType == entityType
                && wj.EntityId == entityId)
            .OrderByDescending(wj => wj.CreatedAt)
            .ToListAsync(cancellationToken);

        return jobs.Select(MapJobToDto).ToList();
    }

    public async Task<List<WorkflowJobDto>> GetWorkflowJobsByStateAsync(
        Guid companyId,
        string state,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting workflow jobs for company {CompanyId} with state {State}", companyId, state);

        if (!Enum.TryParse<WorkflowJobState>(state, true, out var jobState))
        {
            throw new ArgumentException($"Invalid workflow job state: {state}", nameof(state));
        }

        var jobs = await _context.WorkflowJobs
            .Include(wj => wj.WorkflowDefinition)
            .Where(wj => wj.CompanyId == companyId && wj.State == jobState)
            .OrderByDescending(wj => wj.CreatedAt)
            .ToListAsync(cancellationToken);

        return jobs.Select(MapJobToDto).ToList();
    }

    // Private helper methods

    private async Task<Guid?> GetEntityCompanyIdAsync(string entityType, Guid entityId, Guid companyId, CancellationToken cancellationToken)
    {
        switch (entityType.ToLowerInvariant())
        {
            case "order":
                var orderCompanyId = await _context.Orders
                    .IgnoreQueryFilters()
                    .Where(o => o.Id == entityId && o.CompanyId == companyId)
                    .Select(o => o.CompanyId)
                    .FirstOrDefaultAsync(cancellationToken);
                return orderCompanyId == default ? null : orderCompanyId;

            case "invoice":
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == entityId, cancellationToken);
                return invoice?.CompanyId;

            default:
                _logger.LogWarning("Getting company ID for entity type '{EntityType}' is not yet implemented", entityType);
                return null;
        }
    }

    private async Task<string> GetCurrentEntityStatusAsync(string entityType, Guid entityId, Guid companyId, CancellationToken cancellationToken)
    {
        // Entity-specific status lookup. For Order we scope explicitly by companyId (defense-in-depth; caller already has company).
        switch (entityType.ToLowerInvariant())
        {
            case "order":
                // Explicit company-scoped read (defense-in-depth): do not rely on ambient TenantScope for this lookup.
                var orderStatus = await _context.Orders
                    .IgnoreQueryFilters()
                    .Where(o => o.Id == entityId && o.CompanyId == companyId)
                    .Select(o => o.Status)
                    .FirstOrDefaultAsync(cancellationToken);
                return orderStatus ?? "Unknown";

            case "invoice":
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == entityId, cancellationToken);
                return invoice?.Status ?? "Unknown";

            default:
                throw new NotSupportedException($"Getting current status for entity type '{entityType}' is not yet implemented.");
        }
    }

    private async Task ValidateGuardConditionsAsync(
        WorkflowTransitionDto transition,
        string entityType,
        Guid entityId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (transition.GuardConditions == null || transition.GuardConditions.Count == 0)
        {
            return; // No guard conditions to validate
        }

        _logger.LogInformation("Validating guard conditions for entity {EntityType}/{EntityId}", entityType, entityId);

        // Validate each guard condition using registry (NO HARDCODING - all from settings)
        foreach (var (guardConditionKey, value) in transition.GuardConditions)
        {
            // Use registry to validate - loads definition from settings
            bool conditionMet = await _guardConditionValidatorRegistry.ValidateAsync(
                companyId,
                guardConditionKey,
                entityType,
                entityId,
                cancellationToken);

            // Check if value is boolean true (guard condition must be met)
            bool required = value is bool boolValue ? boolValue : 
                          bool.TryParse(value?.ToString(), out var parsed) && parsed;

            if (required && !conditionMet)
            {
                // Provide detailed error message for scheduling conflicts
                if (guardConditionKey == "noSchedulingConflicts" && entityType == "Order")
                {
                    var conflictDetails = await GetConflictDetailsForOrderAsync(entityId, companyId, cancellationToken);
                    throw new InvalidOperationException(
                        $"Cannot start job: Order {entityId} has unresolved scheduling conflicts. {conflictDetails}");
                }

                throw new InvalidOperationException(
                    $"Guard condition '{guardConditionKey}' is required but not met for {entityType} {entityId}.");
            }

            _logger.LogInformation(
                "Guard condition '{Key}' validation result: {Result} for {EntityType} {EntityId}",
                guardConditionKey, conditionMet, entityType, entityId);
        }
    }

    /// <summary>
    /// Ensures side effects (e.g. CreateOrderStatusLog) receive userId and, for Order, siId so audit trail is complete.
    /// Client payload is preserved; we only set missing values.
    /// </summary>
    private async Task<Dictionary<string, object>?> EnrichPayloadForSideEffectsAsync(
        Dictionary<string, object>? payload,
        string entityType,
        Guid entityId,
        Guid companyId,
        Guid? initiatedByUserId,
        CancellationToken cancellationToken)
    {
        var result = payload != null
            ? new Dictionary<string, object>(payload, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (initiatedByUserId.HasValue && !result.ContainsKey("userId"))
            result["userId"] = initiatedByUserId.Value;

        if (string.Equals(entityType, SiWorkflowGuard.OrderEntityType, StringComparison.OrdinalIgnoreCase))
        {
            if (!result.ContainsKey("siId"))
            {
                var order = await _context.Orders
                    .IgnoreQueryFilters()
                    .Where(o => o.Id == entityId && o.CompanyId == companyId)
                    .Select(o => new { o.AssignedSiId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (order?.AssignedSiId != null)
                    result["siId"] = order.AssignedSiId.Value;
            }
        }

        return result.Count > 0 ? result : null;
    }

    private async Task ExecuteSideEffectsAsync(
        WorkflowTransitionDto transition,
        string entityType,
        Guid entityId,
        Guid companyId,
        Dictionary<string, object>? payload,
        CancellationToken cancellationToken)
    {
        if (transition.SideEffectsConfig == null || transition.SideEffectsConfig.Count == 0)
        {
            return; // No side effects to execute
        }

        _logger.LogInformation("Executing side effects for entity {EntityType}/{EntityId}", entityType, entityId);

        // Execute each side effect using registry (NO HARDCODING - all from settings)
        foreach (var (sideEffectKey, value) in transition.SideEffectsConfig)
        {
            try
            {
                // Check if side effect is enabled (value can be bool or config object)
                bool enabled = value is bool boolValue ? boolValue : true;
                if (!enabled) continue;

                // Use registry to execute - loads definition from settings
                await _sideEffectExecutorRegistry.ExecuteAsync(
                    companyId,
                    sideEffectKey,
                    entityType,
                    entityId,
                    transition,
                    payload,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing side effect '{Key}' for {EntityType} {EntityId}", 
                    sideEffectKey, entityType, entityId);
                throw; // Re-throw to fail the workflow transition
            }
        }
    }

    private async Task UpdateEntityStatusAsync(
        string entityType,
        Guid entityId,
        string newStatus,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating status for entity {EntityType}/{EntityId} to {NewStatus}", entityType, entityId, newStatus);

        switch (entityType.ToLowerInvariant())
        {
            case "order":
                if (!companyId.HasValue)
                {
                    throw new InvalidOperationException("Company ID is required to update Order status.");
                }
                var order = await _context.Orders
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(o => o.Id == entityId && o.CompanyId == companyId.Value, cancellationToken);
                if (order != null)
                {
                    order.Status = newStatus;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                break;

            case "invoice":
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.Id == entityId, cancellationToken);
                if (invoice != null)
                {
                    invoice.Status = newStatus;
                    invoice.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                break;

            default:
                throw new NotSupportedException($"Updating status for entity type '{entityType}' is not yet implemented.");
        }
    }

    private static WorkflowJobDto MapJobToDto(WorkflowJob job)
    {
        Dictionary<string, object>? payload = null;
        if (!string.IsNullOrEmpty(job.PayloadJson))
        {
            try
            {
                payload = JsonSerializer.Deserialize<Dictionary<string, object>>(job.PayloadJson);
            }
            catch
            {
                // If deserialization fails, leave null
            }
        }

        return new WorkflowJobDto
        {
            Id = job.Id,
            CompanyId = job.CompanyId,
            WorkflowDefinitionId = job.WorkflowDefinitionId,
            EntityType = job.EntityType,
            EntityId = job.EntityId,
            CurrentStatus = job.CurrentStatus,
            TargetStatus = job.TargetStatus,
            State = job.State.ToString(),
            LastError = job.LastError,
            Payload = payload,
            InitiatedByUserId = job.InitiatedByUserId,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            CorrelationId = job.CorrelationId
        };
    }

    /// <summary>
    /// Get detailed conflict information for an order to include in error messages
    /// </summary>
    private async Task<string> GetConflictDetailsForOrderAsync(
        Guid orderId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Load order to get DepartmentId for department-scoped conflict detection (RBAC consistency)
            var order = await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.DepartmentId })
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                return "Order not found.";
            }

            if (!order.DepartmentId.HasValue)
            {
                return "Order has no department; cannot determine conflict scope.";
            }

            // Find the scheduled slot for this order
            var slot = await _context.ScheduledSlots
                .Where(s => s.OrderId == orderId && s.Status != "Cancelled")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (slot == null)
            {
                return "No scheduled slot found for this order.";
            }

            // Get conflicts (department-scoped for RBAC; never call with departmentId: null)
            var conflicts = await _schedulerService.DetectSchedulingConflictsAsync(
                orderId: orderId,
                slotId: slot.Id,
                siId: slot.ServiceInstallerId,
                date: slot.Date,
                companyId: companyId,
                departmentId: order.DepartmentId,
                cancellationToken);

            if (conflicts.Count == 0)
            {
                return "No conflicts detected.";
            }

            var conflictDetails = string.Join("; ", conflicts.Select((c, idx) => 
                $"Conflict {idx + 1}: {c.ConflictDescription} (Order: {c.OrderServiceId ?? "N/A"}, Time: {c.WindowFrom:hh\\:mm}-{c.WindowTo:hh\\:mm})"));

            return $"Found {conflicts.Count} conflict(s). {conflictDetails} Please resolve conflicts before starting the job.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conflict details for order {OrderId}", orderId);
            return "Unable to retrieve conflict details. Please check the scheduler for conflicts.";
        }
    }
}

