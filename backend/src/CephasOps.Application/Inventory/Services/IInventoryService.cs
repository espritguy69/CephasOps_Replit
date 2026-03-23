using CephasOps.Application.Inventory.DTOs;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Inventory service interface
/// </summary>
public interface IInventoryService
{
    Task<List<MaterialDto>> GetMaterialsAsync(Guid? companyId, Guid? departmentId = null, string? category = null, string? search = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<MaterialDto?> GetMaterialByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<MaterialDto?> GetMaterialByBarcodeAsync(string barcode, Guid? companyId, CancellationToken cancellationToken = default);
    Task<MaterialDto> CreateMaterialAsync(CreateMaterialDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<MaterialDto> UpdateMaterialAsync(Guid id, UpdateMaterialDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteMaterialAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    
    Task<List<StockBalanceDto>> GetStockBalancesAsync(Guid? companyId, Guid? locationId = null, Guid? materialId = null, CancellationToken cancellationToken = default);
    Task<List<StockMovementDto>> GetStockMovementsAsync(Guid? companyId, Guid? materialId = null, Guid? locationId = null, string? movementType = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>Legacy write path: creates StockMovement and ledger entries only (no StockBalance.Quantity). Prefer IStockLedgerService receive/transfer/issue/return for new code. Ledger is the single source of truth.</summary>
    [Obsolete("Legacy write path. Prefer IStockLedgerService (ReceiveAsync, TransferAsync, IssueAsync, ReturnAsync). Ledger is the source of truth for quantities.")]
    Task<StockMovementDto> CreateStockMovementAsync(CreateStockMovementDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<List<StockLocationDto>> GetStockLocationsAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
}
