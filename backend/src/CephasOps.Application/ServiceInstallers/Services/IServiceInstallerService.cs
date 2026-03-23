using CephasOps.Application.ServiceInstallers.DTOs;
using CephasOps.Application.Common.DTOs;
using CephasOps.Domain.ServiceInstallers.Enums;

namespace CephasOps.Application.ServiceInstallers.Services;

/// <summary>
/// Service Installer service interface
/// </summary>
public interface IServiceInstallerService
{
    Task<List<ServiceInstallerDto>> GetServiceInstallersAsync(
        Guid? companyId, 
        Guid? departmentId = null, 
        bool? isActive = null,
        InstallerType? installerType = null,
        InstallerLevel? siLevel = null,
        List<Guid>? skillIds = null,
        CancellationToken cancellationToken = default);
    
    Task<ServiceInstallerDto?> GetServiceInstallerByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<ServiceInstallerDto> CreateServiceInstallerAsync(CreateServiceInstallerDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<ServiceInstallerDto> UpdateServiceInstallerAsync(Guid id, UpdateServiceInstallerDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteServiceInstallerAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available installers for job assignment (filters by availability, skills, level, type)
    /// </summary>
    Task<List<ServiceInstallerDto>> GetAvailableInstallersAsync(
        Guid? companyId,
        Guid? departmentId = null,
        InstallerType? installerType = null,
        InstallerLevel? siLevel = null,
        List<Guid>? requiredSkillIds = null,
        CancellationToken cancellationToken = default);
    
    // CSV Import feature not yet implemented
    // Task<ImportResult<ServiceInstallerCsvDto>> ImportServiceInstallersAsync(List<ServiceInstallerCsvDto> records, Guid? companyId, CancellationToken cancellationToken = default);

    Task<List<ServiceInstallerContactDto>> GetContactsAsync(Guid serviceInstallerId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<ServiceInstallerContactDto> CreateContactAsync(Guid serviceInstallerId, CreateServiceInstallerContactDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<ServiceInstallerContactDto> UpdateContactAsync(Guid contactId, UpdateServiceInstallerContactDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteContactAsync(Guid contactId, Guid? companyId, CancellationToken cancellationToken = default);
    
    // Skills management
    Task<List<ServiceInstallerSkillDto>> GetInstallerSkillsAsync(Guid serviceInstallerId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<List<ServiceInstallerSkillDto>> AssignSkillsAsync(Guid serviceInstallerId, List<Guid> skillIds, Guid? companyId, CancellationToken cancellationToken = default);
    Task RemoveSkillAsync(Guid serviceInstallerId, Guid skillId, Guid? companyId, CancellationToken cancellationToken = default);
}

