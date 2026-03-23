using CephasOps.Application.Companies.DTOs;

namespace CephasOps.Application.Companies.Services;

/// <summary>
/// Vertical service interface
/// </summary>
public interface IVerticalService
{
    Task<List<VerticalDto>> GetVerticalsAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<VerticalDto?> GetVerticalByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<VerticalDto> CreateVerticalAsync(CreateVerticalDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<VerticalDto> UpdateVerticalAsync(Guid id, UpdateVerticalDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteVerticalAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}


