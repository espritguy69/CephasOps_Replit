using CephasOps.Application.RMA.DTOs;

namespace CephasOps.Application.RMA.Services;

/// <summary>
/// RMA service interface
/// </summary>
public interface IRMAService
{
    Task<List<RmaRequestDto>> GetRmaRequestsAsync(Guid? companyId, Guid? partnerId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<List<RmaRequestDto>> GetRmaRequestsByOrderAsync(Guid orderId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<RmaRequestDto?> GetRmaRequestByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<RmaRequestDto> CreateRmaRequestAsync(CreateRmaRequestDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<RmaRequestDto> UpdateRmaRequestAsync(Guid id, UpdateRmaRequestDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteRmaRequestAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}
