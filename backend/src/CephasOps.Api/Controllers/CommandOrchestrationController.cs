using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Commands;
using CephasOps.Application.Commands.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Operator diagnostics: command execution history, failed commands, workflow instances.
/// Phase 9 command bus and workflow orchestration.
/// </summary>
[ApiController]
[Route("api/command-orchestration")]
[Authorize(Policy = "Jobs")]
public class CommandOrchestrationController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly ICommandDiagnosticsQueryService _commandDiagnostics;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CommandOrchestrationController> _logger;

    public CommandOrchestrationController(
        ICommandDiagnosticsQueryService commandDiagnostics,
        IWorkflowOrchestrator workflowOrchestrator,
        ICurrentUserService currentUser,
        ITenantProvider tenantProvider,
        ILogger<CommandOrchestrationController> logger)
    {
        _commandDiagnostics = commandDiagnostics;
        _workflowOrchestrator = workflowOrchestrator;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    private Guid? ScopeCompanyId()
    {
        if (_currentUser.IsSuperAdmin) return null;
        return _tenantProvider.CurrentTenantId;
    }

    /// <summary>List command executions with optional filters.</summary>
    [HttpGet("command-executions")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListCommandExecutions(
        [FromQuery] string? status = null,
        [FromQuery] string? commandType = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] Guid? workflowInstanceId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        take = Math.Min(MaxPageSize, Math.Max(1, take));
        var (items, totalCount) = await _commandDiagnostics.GetExecutionsAsync(
            status, commandType, correlationId, workflowInstanceId, skip, take, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, totalCount, skip, take }));
    }

    /// <summary>Get a single command execution by id.</summary>
    [HttpGet("command-executions/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<CommandExecutionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CommandExecutionDetailDto>>> GetCommandExecution(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dto = await _commandDiagnostics.GetByIdAsync(id, cancellationToken);
        if (dto == null)
            return this.NotFound("Command execution not found.");
        return this.Success(dto);
    }

    /// <summary>List failed command executions (for retry and investigation).</summary>
    [HttpGet("command-executions/failed")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListFailedCommandExecutions(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        take = Math.Min(MaxPageSize, Math.Max(1, take));
        var items = await _commandDiagnostics.GetFailedAsync(take, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items }));
    }

    /// <summary>List workflow instances with optional filters.</summary>
    [HttpGet("workflow-instances")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> ListWorkflowInstances(
        [FromQuery] string? workflowType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var scopeCompany = ScopeCompanyId();
        if (!_currentUser.IsSuperAdmin && companyId.HasValue && companyId != scopeCompany)
            return Forbid();
        take = Math.Min(MaxPageSize, Math.Max(1, take));
        var (items, totalCount) = await _workflowOrchestrator.ListInstancesAsync(
            workflowType, entityType, status, companyId ?? scopeCompany, skip, take, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, totalCount, skip, take }));
    }

    /// <summary>Get workflow instance by id.</summary>
    [HttpGet("workflow-instances/{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsView)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowInstanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceDto>>> GetWorkflowInstance(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dto = await _workflowOrchestrator.GetInstanceAsync(id, cancellationToken);
        if (dto == null)
            return this.NotFound("Workflow instance not found.");
        if (!_currentUser.IsSuperAdmin && dto.CompanyId.HasValue && dto.CompanyId != ScopeCompanyId())
            return Forbid();
        return this.Success(dto);
    }
}
