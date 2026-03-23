using CephasOps.Application.Pnl.DTOs;

namespace CephasOps.Application.Pnl.Services;

/// <summary>
/// P&amp;L service interface
/// </summary>
public interface IPnlService
{
    Task<PnlSummaryDto> GetPnlSummaryAsync(Guid companyId, Guid? periodId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<PnlOrderDetailDto>> GetPnlOrderDetailsAsync(Guid companyId, Guid? orderId = null, Guid? periodId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<PnlDetailPerOrderDto>> GetPnlDetailPerOrderAsync(Guid companyId, Guid? orderId = null, Guid? partnerId = null, Guid? departmentId = null, Guid? serviceInstallerId = null, string? orderType = null, string? kpiResult = null, string? period = null, CancellationToken cancellationToken = default);
    Task RebuildPnlAsync(Guid companyId, string period, CancellationToken cancellationToken = default);
    
    Task<List<PnlPeriodDto>> GetPnlPeriodsAsync(Guid companyId, string? year = null, CancellationToken cancellationToken = default);
    Task<PnlPeriodDto?> GetPnlPeriodByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    
    Task<List<OverheadEntryDto>> GetOverheadEntriesAsync(Guid companyId, Guid? costCentreId = null, string? period = null, CancellationToken cancellationToken = default);
    Task<OverheadEntryDto> CreateOverheadEntryAsync(CreateOverheadEntryDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteOverheadEntryAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}

