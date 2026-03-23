using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly IVendorService _service;

    public VendorsController(IVendorService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<VendorDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<VendorDto>>($"Failed to get vendors: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<VendorDto>($"Vendor with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<VendorDto>($"Failed to get vendor: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> Create([FromQuery] Guid companyId, [FromBody] CreateVendorDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<VendorDto>.SuccessResponse(item, "Vendor created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<VendorDto>($"Failed to create vendor: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<VendorDto>>> Update(Guid id, [FromBody] UpdateVendorDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            if (item == null)
                return this.NotFound<VendorDto>($"Vendor with ID {id} not found");
            return this.Success(item, "Vendor updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<VendorDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<VendorDto>($"Failed to update vendor: {ex.Message}", 500);
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
                return StatusCode(404, ApiResponse.ErrorResponse($"Vendor with ID {id} not found"));
            return this.StatusCode(204, ApiResponse.SuccessResponse("Vendor deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete vendor: {ex.Message}"));
        }
    }
}
