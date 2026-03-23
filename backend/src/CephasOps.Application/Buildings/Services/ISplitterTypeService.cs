using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// SplitterType service interface
/// </summary>
public interface ISplitterTypeService
{
    Task<List<SplitterTypeDto>> GetSplitterTypesAsync(Guid? companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<SplitterTypeDto?> GetSplitterTypeByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SplitterTypeDto> CreateSplitterTypeAsync(CreateSplitterTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<SplitterTypeDto> UpdateSplitterTypeAsync(Guid id, UpdateSplitterTypeDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteSplitterTypeAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

