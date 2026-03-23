using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize(Policy = "Reports")]
[ApiController]
[Route("api/report-definitions")]
public class ReportDefinitionsController : ControllerBase
{
    private readonly IReportDefinitionService _service;

    public ReportDefinitionsController(IReportDefinitionService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ReportDefinitionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ReportDefinitionDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ReportDefinitionDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<ReportDefinitionDto>>($"Failed to get report definitions: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<ReportDefinitionDto>($"Report definition with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<ReportDefinitionDto>($"Failed to get report definition: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Create([FromQuery] Guid companyId, [FromBody] ReportDefinitionDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<ReportDefinitionDto>.SuccessResponse(item, "Report definition created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<ReportDefinitionDto>($"Failed to create report definition: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ReportDefinitionDto>>> Update(Guid id, [FromBody] ReportDefinitionDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Report definition updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<ReportDefinitionDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<ReportDefinitionDto>($"Failed to update report definition: {ex.Message}", 500);
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
            return this.StatusCode(204, ApiResponse.SuccessResponse("Report definition deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete report definition: {ex.Message}"));
        }
    }
}

