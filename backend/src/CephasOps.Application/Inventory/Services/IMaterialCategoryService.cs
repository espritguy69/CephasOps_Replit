using CephasOps.Application.Inventory.DTOs;

namespace CephasOps.Application.Inventory.Services;

/// <summary>
/// Material category service interface
/// </summary>
public interface IMaterialCategoryService
{
    Task<List<MaterialCategoryDto>> GetMaterialCategoriesAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<MaterialCategoryDto?> GetMaterialCategoryByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<MaterialCategoryDto> CreateMaterialCategoryAsync(CreateMaterialCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<MaterialCategoryDto> UpdateMaterialCategoryAsync(Guid id, UpdateMaterialCategoryDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteMaterialCategoryAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}
