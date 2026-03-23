using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// SLA profile service interface
/// </summary>
public interface ISlaProfileService
{
    Task<List<SlaProfileDto>> GetProfilesAsync(Guid companyId, string? orderType = null, Guid? partnerId = null, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<SlaProfileDto?> GetProfileByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<SlaProfileDto?> GetEffectiveProfileAsync(Guid companyId, Guid? partnerId, string orderType, Guid? departmentId, bool isVip = false, DateTime? effectiveDate = null, CancellationToken cancellationToken = default);
    Task<SlaProfileDto> CreateProfileAsync(CreateSlaProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<SlaProfileDto> UpdateProfileAsync(Guid id, UpdateSlaProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteProfileAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<SlaProfileDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
}

