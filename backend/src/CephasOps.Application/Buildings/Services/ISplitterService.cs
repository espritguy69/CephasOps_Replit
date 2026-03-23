using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// Splitter service interface
/// </summary>
public interface ISplitterService
{
    Task<List<SplitterDto>> GetSplittersAsync(Guid? companyId, Guid? buildingId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<SplitterDto?> GetSplitterByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SplitterDto> CreateSplitterAsync(CreateSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SplitterDto> UpdateSplitterAsync(Guid id, UpdateSplitterDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteSplitterAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SplitterPortDto> UpdateSplitterPortAsync(Guid portId, UpdateSplitterPortDto dto, Guid? companyId, CancellationToken cancellationToken = default);
}

