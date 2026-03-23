using CephasOps.Application.Agent.DTOs;

namespace CephasOps.Application.Agent.Services;

/// <summary>
/// Agent Mode Service - Automated decision-making and workflow execution
/// </summary>
public interface IAgentModeService
{
    /// <summary>
    /// Process email replies automatically based on templates and patterns
    /// </summary>
    Task<AgentProcessingResult> ProcessEmailReplyAsync(Guid emailMessageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect and handle payment rejections
    /// </summary>
    Task<AgentProcessingResult> HandlePaymentRejectionAsync(Guid paymentId, string rejectionReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Intelligently route orders based on content, location, and rules
    /// </summary>
    Task<AgentRoutingResult> RouteOrderIntelligentlyAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate smart KPIs based on workflow audit data
    /// </summary>
    Task<AgentKpiResult> CalculateSmartKpisAsync(Guid? departmentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-approve routine reschedules based on rules
    /// </summary>
    Task<AgentProcessingResult> AutoApproveRescheduleAsync(Guid rescheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process all pending agent tasks
    /// </summary>
    Task<List<AgentProcessingResult>> ProcessPendingTasksAsync(CancellationToken cancellationToken = default);
}

