using CephasOps.Application.Settings.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Api.Common;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bins")]
public class BinsController : ControllerBase
{
    private readonly IBinService _service;
    private readonly IInventoryService _inventoryService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ApplicationDbContext _context;

    public BinsController(
        IBinService service,
        IInventoryService inventoryService,
        ITenantProvider tenantProvider,
        ApplicationDbContext context)
    {
        _service = service;
        _inventoryService = inventoryService;
        _tenantProvider = tenantProvider;
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BinDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<BinDto>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<BinDto>>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<BinDto>>>> GetAll([FromQuery] Guid? companyId, [FromQuery] bool? isActive = null)
    {
        var (scopeCompanyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        // Non-SuperAdmin: require query companyId to match tenant scope (or use scope when companyId not provided)
        var effectiveCompanyId = companyId ?? scopeCompanyId;
        if (scopeCompanyId != default && companyId.HasValue && companyId.Value != scopeCompanyId)
            return this.Forbidden<List<BinDto>>("Company scope not allowed.");
        if (effectiveCompanyId == default)
            return this.Forbidden<List<BinDto>>("Company context is required for this operation.");
        try
        {
            var items = await _service.GetAllAsync(effectiveCompanyId, isActive);
            return this.Success(items);
        }
        catch (Exception ex)
        {
            return this.Error<List<BinDto>>($"Failed to get bins: {ex.Message}", 500);
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BinDto>>> GetById(Guid id)
    {
        var (scopeCompanyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return this.NotFound<BinDto>($"Bin with ID {id} not found");
            if (item.CompanyId.HasValue && item.CompanyId.Value != scopeCompanyId)
                return this.NotFound<BinDto>($"Bin with ID {id} not found");
            return this.Success(item);
        }
        catch (Exception ex)
        {
            return this.Error<BinDto>($"Failed to get bin: {ex.Message}", 500);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BinDto>>> Create([FromQuery] Guid? companyId, [FromBody] BinDto dto)
    {
        var (scopeCompanyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        var effectiveCompanyId = companyId ?? scopeCompanyId;
        if (scopeCompanyId != default && companyId.HasValue && companyId.Value != scopeCompanyId)
            return this.Forbidden<BinDto>("Company scope not allowed.");
        if (effectiveCompanyId == default)
            return this.Forbidden<BinDto>("Company context is required for this operation.");
        try
        {
            var item = await _service.CreateAsync(effectiveCompanyId, dto);
            return this.StatusCode(201, ApiResponse<BinDto>.SuccessResponse(item, "Bin created successfully"));
        }
        catch (Exception ex)
        {
            return this.Error<BinDto>($"Failed to create bin: {ex.Message}", 500);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BinDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BinDto>>> Update(Guid id, [FromBody] BinDto dto)
    {
        var (scopeCompanyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null || (item.CompanyId.HasValue && item.CompanyId.Value != scopeCompanyId))
                return this.NotFound<BinDto>($"Bin with ID {id} not found");
            item = await _service.UpdateAsync(id, dto);
            return this.Success(item, "Bin updated successfully");
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound<BinDto>(ex.Message);
        }
        catch (Exception ex)
        {
            return this.Error<BinDto>($"Failed to update bin: {ex.Message}", 500);
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (scopeCompanyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null || (item.CompanyId.HasValue && item.CompanyId.Value != scopeCompanyId))
                return this.NotFound("Bin not found");
            await _service.DeleteAsync(id);
            return this.StatusCode(204, ApiResponse.SuccessResponse("Bin deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return this.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResponse($"Failed to delete bin: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get bin contents (stock balances) by bin code
    /// </summary>
    [HttpGet("by-code/{code}/contents")]
    [ProducesResponseType(typeof(ApiResponse<BinContentsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BinContentsDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BinContentsDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<BinContentsDto>>> GetBinContents(string code, CancellationToken cancellationToken = default)
    {
        var (companyId, err) = this.RequireCompanyId(_tenantProvider);
        if (err != null) return err;

        try
        {
            // Find bin by code
            var bin = await _context.Set<Domain.Settings.Entities.Bin>()
                .FirstOrDefaultAsync(b => b.Code == code && b.CompanyId == companyId && !b.IsDeleted,
                    cancellationToken);

            if (bin == null)
            {
                return this.NotFound<BinContentsDto>($"Bin with code '{code}' not found");
            }

            // Get warehouse's stock location
            var warehouseLocation = await _context.StockLocations
                .FirstOrDefaultAsync(sl => sl.WarehouseId == bin.WarehouseId && 
                    sl.CompanyId == companyId, 
                    cancellationToken);

            if (warehouseLocation == null)
            {
                return this.Success(new BinContentsDto
                {
                    Bin = new BinDto
                    {
                        Id = bin.Id,
                        Code = bin.Code,
                        Name = bin.Name,
                        Section = bin.Section,
                        Capacity = bin.Capacity,
                        CurrentStock = bin.CurrentStock,
                        UtilizationPercent = bin.UtilizationPercent
                    },
                    StockBalances = new List<StockBalanceDto>()
                });
            }

            // Get stock balances for this location
            var stockBalances = await _inventoryService.GetStockBalancesAsync(
                companyId != Guid.Empty ? companyId : null, 
                warehouseLocation.Id, 
                null, 
                cancellationToken);

            return this.Success(new BinContentsDto
            {
                Bin = new BinDto
                {
                    Id = bin.Id,
                    Code = bin.Code,
                    Name = bin.Name,
                    Section = bin.Section,
                    Capacity = bin.Capacity,
                    CurrentStock = bin.CurrentStock,
                    UtilizationPercent = bin.UtilizationPercent
                },
                StockBalances = stockBalances
            });
        }
        catch (Exception ex)
        {
            return this.Error<BinContentsDto>($"Failed to get bin contents: {ex.Message}", 500);
        }
    }
}

public class BinContentsDto
{
    public BinDto Bin { get; set; } = null!;
    public List<StockBalanceDto> StockBalances { get; set; } = new();
}
