using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/service-plans")]
public class ServicePlansController : ControllerBase
{
    private readonly IServicePlanService _service;

    public ServicePlansController(IServicePlanService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ServicePlanDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ServicePlanDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ServicePlanDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<ServicePlanDto>>($"Failed to get service plans: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServicePlanDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<ServicePlanDto>($"Service plan with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<ServicePlanDto>($"Failed to get service plan: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServicePlanDto>>> Create([FromQuery] Guid companyId, [FromBody] ServicePlanDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<ServicePlanDto>.SuccessResponse(item, "Service plan created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<ServicePlanDto>($"Failed to create service plan: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ServicePlanDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ServicePlanDto>>> Update(Guid id, [FromBody] ServicePlanDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Service plan updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<ServicePlanDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<ServicePlanDto>($"Failed to update service plan: {ex.Message}", 500);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Service plan deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete service plan: {ex.Message}"));
        }
    }
}
