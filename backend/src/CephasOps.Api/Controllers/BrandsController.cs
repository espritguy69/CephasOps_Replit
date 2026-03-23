using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _service;

    public BrandsController(IBrandService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BrandDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BrandDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<BrandDto>>($"Failed to get brands: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<BrandDto>($"Brand with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<BrandDto>($"Failed to get brand: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> Create([FromQuery] Guid companyId, [FromBody] CreateBrandDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<BrandDto>.SuccessResponse(item, "Brand created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<BrandDto>($"Failed to create brand: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BrandDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BrandDto>>> Update(Guid id, [FromBody] UpdateBrandDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Brand updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BrandDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<BrandDto>($"Failed to update brand: {ex.Message}", 500);
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
            return this.StatusCode(204, ApiResponse.SuccessResponse("Brand deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete brand: {ex.Message}"));
        }
    }
}
