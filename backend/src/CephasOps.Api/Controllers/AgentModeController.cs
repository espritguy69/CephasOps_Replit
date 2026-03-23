using CephasOps.Application.Agent.DTOs;
using CephasOps.Application.Agent.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Departments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CephasOps.Api.Common;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Agent Mode endpoints - Automated decision-making and workflow execution
/// </summary>
[ApiController]
[Route("api/agent")]
[Authorize]
public class AgentModeController : ControllerBase
{
    private readonly IAgentModeService _agentModeService;
    private readonly IDepartmentAccessService _departmentAccessService;
    private readonly IDepartmentRequestContext _departmentRequestContext;
    private readonly ILogger<AgentModeController> _logger;

    public AgentModeController(
        IAgentModeService agentModeService,
        IDepartmentAccessService departmentAccessService,
        IDepartmentRequestContext departmentRequestContext,
        ILogger<AgentModeController> logger)
    {
        _agentModeService = agentModeService;
        _departmentAccessService = departmentAccessService;
        _departmentRequestContext = departmentRequestContext;
        _logger = logger;
    }

    /// <summary>
    /// Process email reply automatically
    /// </summary>
    [HttpPost("process-email-reply/{emailMessageId}")]
    [ProducesResponseType(typeof(ApiResponse<AgentProcessingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentProcessingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AgentProcessingResult>>> ProcessEmailReply(
        Guid emailMessageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _agentModeService.ProcessEmailReplyAsync(emailMessageId, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email reply {EmailId}", emailMessageId);
            return this.InternalServerError<AgentProcessingResult>($"Failed to process email reply: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle payment rejection
    /// </summary>
    [HttpPost("handle-payment-rejection")]
    [ProducesResponseType(typeof(ApiResponse<AgentProcessingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentProcessingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AgentProcessingResult>>> HandlePaymentRejection(
        [FromBody] PaymentRejectionDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _agentModeService.HandlePaymentRejectionAsync(
                dto.PaymentId, dto.RejectionReason, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment rejection {PaymentId}", dto.PaymentId);
            return this.InternalServerError<AgentProcessingResult>($"Failed to handle payment rejection: {ex.Message}");
        }
    }

    /// <summary>
    /// Route order intelligently
    /// </summary>
    [HttpPost("route-order/{orderId}")]
    [ProducesResponseType(typeof(ApiResponse<AgentRoutingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentRoutingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AgentRoutingResult>>> RouteOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _agentModeService.RouteOrderIntelligentlyAsync(orderId, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing order {OrderId}", orderId);
            return this.InternalServerError<AgentRoutingResult>($"Failed to route order: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate smart KPIs
    /// </summary>
    [HttpGet("calculate-kpis")]
    [ProducesResponseType(typeof(ApiResponse<AgentKpiResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentKpiResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AgentKpiResult>>> CalculateKpis(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        Guid? departmentScope;
        try
        {
            departmentScope = await _departmentAccessService.ResolveDepartmentScopeAsync(departmentId ?? _departmentRequestContext.DepartmentId, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            return this.Error<AgentKpiResult>("You do not have access to this department", 403);
        }

        try
        {
            var result = await _agentModeService.CalculateSmartKpisAsync(
                departmentScope, fromDate, toDate, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating KPIs");
            return this.InternalServerError<AgentKpiResult>($"Failed to calculate KPIs: {ex.Message}");
        }
    }

    /// <summary>
    /// Auto-approve reschedule
    /// </summary>
    [HttpPost("auto-approve-reschedule/{rescheduleId}")]
    [ProducesResponseType(typeof(ApiResponse<AgentProcessingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AgentProcessingResult>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<AgentProcessingResult>>> AutoApproveReschedule(
        Guid rescheduleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _agentModeService.AutoApproveRescheduleAsync(rescheduleId, cancellationToken);
            return this.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-approving reschedule {RescheduleId}", rescheduleId);
            return this.InternalServerError<AgentProcessingResult>($"Failed to auto-approve reschedule: {ex.Message}");
        }
    }

    /// <summary>
    /// Process all pending agent tasks
    /// </summary>
    [HttpPost("process-pending-tasks")]
    [ProducesResponseType(typeof(ApiResponse<List<AgentProcessingResult>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<AgentProcessingResult>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<AgentProcessingResult>>>> ProcessPendingTasks(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _agentModeService.ProcessPendingTasksAsync(cancellationToken);
            return this.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending tasks");
            return this.InternalServerError<List<AgentProcessingResult>>($"Failed to process pending tasks: {ex.Message}");
        }
    }
}

