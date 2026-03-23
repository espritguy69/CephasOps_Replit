using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Material template service interface
/// </summary>
public interface IMaterialTemplateService
{
    Task<List<MaterialTemplateDto>> GetTemplatesAsync(Guid companyId, string? orderType = null, Guid? installationMethodId = null, Guid? buildingTypeId = null, Guid? partnerId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<MaterialTemplateDto?> GetTemplateByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<MaterialTemplateDto?> GetEffectiveTemplateAsync(Guid companyId, Guid? partnerId, string orderType, Guid? installationMethodId, Guid? buildingTypeId, CancellationToken cancellationToken = default);
    Task<MaterialTemplateDto> CreateTemplateAsync(CreateMaterialTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<MaterialTemplateDto> UpdateTemplateAsync(Guid id, UpdateMaterialTemplateDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<MaterialTemplateDto> SetAsDefaultAsync(Guid id, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<MaterialTemplateDto> CloneTemplateAsync(Guid sourceId, string newName, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
}

