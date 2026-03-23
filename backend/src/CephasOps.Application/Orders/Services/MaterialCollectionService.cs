using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Service for checking material collection requirements when orders are assigned to SIs
/// </summary>
public class MaterialCollectionService : IMaterialPackProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IMaterialTemplateService _materialTemplateService;
    private readonly IStockLedgerService _stockLedgerService;
    private readonly ILogger<MaterialCollectionService> _logger;

    public MaterialCollectionService(
        ApplicationDbContext context,
        IMaterialTemplateService materialTemplateService,
        IStockLedgerService stockLedgerService,
        ILogger<MaterialCollectionService> logger)
    {
        _context = context;
        _materialTemplateService = materialTemplateService;
        _stockLedgerService = stockLedgerService;
        _logger = logger;
    }

    /// <summary>
    /// Check if SI has required materials for an order
    /// </summary>
    public async Task<MaterialCollectionCheckResultDto> CheckMaterialCollectionAsync(
        Guid orderId,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking material collection for order {OrderId}", orderId);

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        if (!order.AssignedSiId.HasValue)
        {
            _logger.LogWarning("Order {OrderId} has no assigned SI", orderId);
            return new MaterialCollectionCheckResultDto
            {
                OrderId = orderId,
                RequiresCollection = false,
                MissingMaterials = new List<MissingMaterialDto>(),
                Message = "No SI assigned to order"
            };
        }

        var siId = order.AssignedSiId.Value;

        // Get required materials from template
        var requiredMaterials = await GetRequiredMaterialsAsync(
            order,
            companyId ?? order.CompanyId ?? Guid.Empty,
            cancellationToken);

        if (requiredMaterials.Count == 0)
        {
            _logger.LogInformation("No material template found for order {OrderId}", orderId);
            return new MaterialCollectionCheckResultDto
            {
                OrderId = orderId,
                RequiresCollection = false,
                MissingMaterials = new List<MissingMaterialDto>(),
                Message = "No material template configured for this order type"
            };
        }

        // Get SI's current inventory (ledger-derived)
        var effectiveCompanyId = companyId ?? order.CompanyId ?? Guid.Empty;
        var siInventory = await GetSiInventoryAsync(siId, effectiveCompanyId, cancellationToken);

        // Compare required vs available
        var missingMaterials = new List<MissingMaterialDto>();
        foreach (var required in requiredMaterials)
        {
            var available = siInventory.GetValueOrDefault(required.MaterialId, 0);
            var missing = Math.Max(0, required.Quantity - available);

            if (missing > 0)
            {
                var material = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Id == required.MaterialId, cancellationToken);

                missingMaterials.Add(new MissingMaterialDto
                {
                    MaterialId = required.MaterialId,
                    MaterialCode = material?.ItemCode ?? string.Empty,
                    MaterialName = material?.Description ?? string.Empty,
                    RequiredQuantity = required.Quantity,
                    AvailableQuantity = available,
                    MissingQuantity = missing,
                    UnitOfMeasure = material?.UnitOfMeasure ?? "pcs"
                });
            }
        }

        return new MaterialCollectionCheckResultDto
        {
            OrderId = orderId,
            ServiceInstallerId = siId,
            RequiresCollection = missingMaterials.Count > 0,
            MissingMaterials = missingMaterials,
            Message = missingMaterials.Count > 0
                ? $"{missingMaterials.Count} material(s) need to be collected from warehouse"
                : "All required materials are available"
        };
    }

    /// <summary>
    /// Get required materials for an order based on material template
    /// </summary>
    private async Task<List<RequiredMaterialDto>> GetRequiredMaterialsAsync(
        Order order,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        // Get OrderType Code from OrderTypeId
        string orderTypeCode = string.Empty;
        if (order.OrderTypeId != Guid.Empty)
        {
            var orderType = await _context.OrderTypes
                .FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);
            orderTypeCode = orderType?.Code ?? string.Empty;
        }

        var template = await _materialTemplateService.GetEffectiveTemplateAsync(
            companyId,
            order.PartnerId,
            orderTypeCode,
            order.InstallationMethodId,
            null, // BuildingTypeId is deprecated
            cancellationToken);

        if (template == null || template.Items == null || template.Items.Count == 0)
        {
            return new List<RequiredMaterialDto>();
        }

        return template.Items.Select(item => new RequiredMaterialDto
        {
            MaterialId = item.MaterialId,
            Quantity = item.Quantity
        }).ToList();
    }

    /// <summary>
    /// Get SI's current inventory (materials in their possession). Uses ledger-derived balances.
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> GetSiInventoryAsync(
        Guid siId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var siLocation = await _context.StockLocations
            .FirstOrDefaultAsync(
                sl => sl.LinkedServiceInstallerId == siId && sl.Type == "SI" && sl.IsActive,
                cancellationToken);

        if (siLocation == null)
        {
            _logger.LogWarning("No stock location found for SI {SiId}", siId);
            return new Dictionary<Guid, decimal>();
        }

        var ledgerBalances = await _stockLedgerService.GetLedgerDerivedBalancesAsync(
            companyId, locationId: siLocation.Id, materialId: null, cancellationToken);

        return ledgerBalances
            .GroupBy(b => b.MaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.OnHand));
    }

    /// <summary>
    /// Get materials required for an order (for display purposes)
    /// </summary>
    public async Task<List<RequiredMaterialDisplayDto>> GetRequiredMaterialsForOrderAsync(
        Guid orderId,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        // Get OrderType Code from OrderTypeId
        string orderTypeCode = string.Empty;
        if (order.OrderTypeId != Guid.Empty)
        {
            var orderType = await _context.OrderTypes
                .FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);
            orderTypeCode = orderType?.Code ?? string.Empty;
        }

        var template = await _materialTemplateService.GetEffectiveTemplateAsync(
            companyId ?? order.CompanyId ?? Guid.Empty,
            order.PartnerId,
            orderTypeCode,
            order.InstallationMethodId,
            null, // BuildingTypeId is deprecated
            cancellationToken);

        if (template == null || template.Items == null || template.Items.Count == 0)
        {
            return new List<RequiredMaterialDisplayDto>();
        }

        var materialIds = template.Items.Select(i => i.MaterialId).ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

        return template.Items.Select(item =>
        {
            var material = materials.GetValueOrDefault(item.MaterialId);
            return new RequiredMaterialDisplayDto
            {
                MaterialId = item.MaterialId,
                MaterialCode = material?.ItemCode ?? string.Empty,
                MaterialName = material?.Description ?? string.Empty,
                Quantity = item.Quantity,
                UnitOfMeasure = material?.UnitOfMeasure ?? "pcs",
                IsSerialised = material?.IsSerialised ?? false
            };
        }).ToList();
    }

    /// <summary>
    /// Get the material pack for an order: required materials list plus missing (to collect). Uses existing check logic.
    /// </summary>
    public async Task<MaterialPackDto> GetMaterialPackAsync(
        Guid orderId,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var check = await CheckMaterialCollectionAsync(orderId, companyId, cancellationToken);
        var required = await GetRequiredMaterialsForOrderAsync(orderId, companyId, cancellationToken);
        return new MaterialPackDto
        {
            OrderId = check.OrderId,
            ServiceInstallerId = check.ServiceInstallerId,
            RequiresCollection = check.RequiresCollection,
            Message = check.Message,
            RequiredMaterials = required,
            MissingMaterials = check.MissingMaterials
        };
    }
}

/// <summary>
/// Required material DTO (internal)
/// </summary>
internal class RequiredMaterialDto
{
    public Guid MaterialId { get; set; }
    public decimal Quantity { get; set; }
}

