using CephasOps.Api.Authorization;
using CephasOps.Api.Common;
using CephasOps.Application.Workers;
using CephasOps.Application.Workers.DTOs;
using CephasOps.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

/// <summary>
/// Worker coordination visibility: list workers, heartbeat status, jobs owned.
/// Admin diagnostics for distributed worker architecture.
/// </summary>
[ApiController]
[Route("api/system/workers")]
[Authorize(Policy = "Jobs")]
public class SystemWorkersController : ControllerBase
{
    private readonly IWorkerCoordinator _coordinator;
    private readonly ILogger<SystemWorkersController> _logger;

    public SystemWorkersController(IWorkerCoordinator coordinator, ILogger<SystemWorkersController> logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    /// <summary>List all worker instances (active and inactive) with heartbeat age and stale detection.</summary>
    [HttpGet]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WorkerInstanceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkerInstanceDto>>>> List(CancellationToken cancellationToken)
    {
        var list = await _coordinator.ListWorkersAsync(cancellationToken).ConfigureAwait(false);
        return this.Success(list);
    }

    /// <summary>Get a single worker by id with owned replay/rebuild operations.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.JobsAdmin)]
    [ProducesResponseType(typeof(ApiResponse<WorkerInstanceDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WorkerInstanceDetailDto>>> Get(Guid id, CancellationToken cancellationToken)
    {
        var worker = await _coordinator.GetWorkerAsync(id, cancellationToken).ConfigureAwait(false);
        if (worker == null)
            return this.NotFound<WorkerInstanceDetailDto>("Worker not found.");
        return this.Success(worker);
    }
}
