using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/product-types")]
public class ProductTypesController : ControllerBase
{
    private readonly IProductTypeService _service;

    public ProductTypesController(IProductTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProductTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ProductTypeDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ProductTypeDto>>>> GetAll([FromQuery] Guid companyId, [FromQuery] bool? isActive = null)
    {
        try
        {
            var items = await _service.GetAllAsync(companyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<ProductTypeDto>>($"Failed to get product types: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductTypeDto>>> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<ProductTypeDto>($"Product type with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<ProductTypeDto>($"Failed to get product type: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductTypeDto>>> Create([FromQuery] Guid companyId, [FromBody] ProductTypeDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(companyId, dto);
            return this.StatusCode(201, ApiResponse<ProductTypeDto>.SuccessResponse(item, "Product type created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<ProductTypeDto>($"Failed to create product type: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ProductTypeDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ProductTypeDto>>> Update(Guid id, [FromBody] ProductTypeDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Product type updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<ProductTypeDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<ProductTypeDto>($"Failed to update product type: {ex.Message}", 500);
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
            return this.StatusCode(204, ApiResponse.SuccessResponse("Product type deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return StatusCode(404, ApiResponse.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete product type: {ex.Message}"));
        }
    }
}
