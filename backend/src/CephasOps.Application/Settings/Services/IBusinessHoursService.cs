using CephasOps.Application.Settings.DTOs;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Business Hours service interface
/// </summary>
public interface IBusinessHoursService
{
    Task<List<BusinessHoursDto>> GetBusinessHoursAsync(Guid companyId, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<BusinessHoursDto?> GetBusinessHoursByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<BusinessHoursDto?> GetEffectiveBusinessHoursAsync(Guid companyId, Guid? departmentId = null, DateTime? effectiveDate = null, CancellationToken cancellationToken = default);
    Task<bool> IsBusinessHoursAsync(Guid companyId, DateTime dateTime, Guid? departmentId = null, CancellationToken cancellationToken = default);
    Task<BusinessHoursDto> CreateBusinessHoursAsync(CreateBusinessHoursDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<BusinessHoursDto> UpdateBusinessHoursAsync(Guid id, UpdateBusinessHoursDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteBusinessHoursAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<List<PublicHolidayDto>> GetPublicHolidaysAsync(Guid companyId, int? year = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PublicHolidayDto> CreatePublicHolidayAsync(CreatePublicHolidayDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<PublicHolidayDto> UpdatePublicHolidayAsync(Guid id, UpdatePublicHolidayDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task DeletePublicHolidayAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> IsPublicHolidayAsync(Guid companyId, DateTime date, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a template business hours configuration (8am-6pm Monday-Friday)
    /// </summary>
    Task<BusinessHoursDto> CreateTemplateBusinessHoursAsync(
        string name = "Standard Business Hours (8am-6pm)",
        Guid? departmentId = null,
        Guid? companyId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}

