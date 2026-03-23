using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tax-codes")]
public class TaxCodesController : ControllerBase
{
    private readonly ITaxCodeService _service;

    public TaxCodesController(ITaxCodeService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TaxCodeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TaxCodeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TaxCodeDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<TaxCodeDto>>($"Failed to get tax codes: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaxCodeDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<TaxCodeDto>($"Tax code with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<TaxCodeDto>($"Failed to get tax code: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaxCodeDto>>> Create([FromQuery] Guid companyId, [FromBody] CreateTaxCodeDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<TaxCodeDto>.SuccessResponse(item, "Tax code created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<TaxCodeDto>($"Failed to create tax code: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TaxCodeDto>>> Update(Guid id, [FromBody] UpdateTaxCodeDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            if (item == null)
                return this.NotFound<TaxCodeDto>($"Tax code with ID {id} not found");
            return this.Success(item, "Tax code updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<TaxCodeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<TaxCodeDto>($"Failed to update tax code: {ex.Message}", 500);
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
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return StatusCode(404, ApiResponse.ErrorResponse($"Tax code with ID {id} not found"));
            return this.StatusCode(204, ApiResponse.SuccessResponse("Tax code deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete tax code: {ex.Message}"));
        }
    }
}
