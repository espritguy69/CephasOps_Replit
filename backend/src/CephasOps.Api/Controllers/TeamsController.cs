using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _service;

    public TeamsController(ITeamService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TeamDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TeamDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TeamDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<TeamDto>>($"Failed to get teams: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TeamDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<TeamDto>($"Team with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<TeamDto>($"Failed to get team: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TeamDto>>> Create([FromQuery] Guid companyId, [FromBody] CreateTeamDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<TeamDto>.SuccessResponse(item, "Team created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<TeamDto>($"Failed to create team: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TeamDto>>> Update(Guid id, [FromBody] UpdateTeamDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Team updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<TeamDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<TeamDto>($"Failed to update team: {ex.Message}", 500);
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
            return this.StatusCode(204, ApiResponse.SuccessResponse("Team deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete team: {ex.Message}"));
        }
    }
}
