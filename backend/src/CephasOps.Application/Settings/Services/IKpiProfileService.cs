using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// KPI profile service interface
/// </summary>
public interface IKpiProfileService
{
    Task<List<KpiProfileDto>> GetProfilesAsync(Guid companyId, string? orderType = null, Guid? partnerId = null, Guid? installationMethodId = null, Guid? buildingTypeId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<KpiProfileDto?> GetProfileByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<KpiProfileDto?> GetEffectiveProfileAsync(Guid companyId, Guid? partnerId, string orderType, Guid? installationMethodId, Guid? buildingTypeId, DateTime? jobDate = null, CancellationToken cancellationToken = default);
    Task<KpiProfileDto> CreateProfileAsync(CreateKpiProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<KpiProfileDto> UpdateProfileAsync(Guid id, UpdateKpiProfileDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteProfileAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<KpiProfileDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<KpiEvaluationResultDto> EvaluateOrderAsync(Guid orderId, Guid companyId, CancellationToken cancellationToken = default);
}

