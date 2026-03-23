using CephasOps.Application.Departments.DTOs;

namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Department service interface
/// </summary>
public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetDepartmentsAsync(Guid? companyId, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpdateDepartmentDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteDepartmentAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
    
    Task<List<MaterialAllocationDto>> GetMaterialAllocationsAsync(Guid departmentId, Guid? companyId, CancellationToken cancellationToken = default);
    Task<MaterialAllocationDto> CreateMaterialAllocationAsync(Guid departmentId, CreateMaterialAllocationDto dto, Guid? companyId, CancellationToken cancellationToken = default);
    Task DeleteMaterialAllocationAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default);
}
