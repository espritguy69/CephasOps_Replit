using CephasOps.Application.Buildings.DTOs;

namespace CephasOps.Application.Buildings.Services;

/// <summary>
/// InstallationMethod service interface
/// </summary>
public interface IInstallationMethodService
{
    Task<List<InstallationMethodDto>> GetInstallationMethodsAsync(Guid? companyId, Guid? departmentId = null, string? category = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<InstallationMethodDto?> GetInstallationMethodByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<InstallationMethodDto> CreateInstallationMethodAsync(CreateInstallationMethodDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<InstallationMethodDto> UpdateInstallationMethodAsync(Guid id, UpdateInstallationMethodDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteInstallationMethodAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}

