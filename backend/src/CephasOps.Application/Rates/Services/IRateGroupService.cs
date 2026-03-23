using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Rates.Services;

/// <summary>
/// Rate group CRUD and order type/subtype mapping (Phase 1 — no impact on payout resolution).
/// </summary>
public interface IRateGroupService
{
    Task<List<RateGroupDto>> ListRateGroupsAsync(Guid? companyId, bool? isActive, CancellationToken cancellationToken = default);
    Task<RateGroupDto?> GetRateGroupByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<RateGroupDto> CreateRateGroupAsync(CreateRateGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<RateGroupDto> UpdateRateGroupAsync(Guid id, UpdateRateGroupDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteRateGroupAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<List<OrderTypeSubtypeRateGroupMappingDto>> ListMappingsAsync(Guid? companyId, Guid? rateGroupId, Guid? orderTypeId, CancellationToken cancellationToken = default);
    Task<OrderTypeSubtypeRateGroupMappingDto> AssignRateGroupToOrderTypeSubtypeAsync(AssignRateGroupToOrderTypeSubtypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task UnassignRateGroupMappingAsync(Guid mappingId, Guid? companyId, CancellationToken cancellationToken = default);
}
