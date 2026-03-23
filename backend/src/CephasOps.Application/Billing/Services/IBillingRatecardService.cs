using CephasOps.Application.Billing.DTOs;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Billing ratecard service interface
/// </summary>
public interface IBillingRatecardService
{
    Task<List<BillingRatecardDto>> GetBillingRatecardsAsync(
        Guid companyId, 
        Guid? partnerId = null, 
        Guid? orderTypeId = null,
        Guid? departmentId = null,
        string? serviceCategory = null,
        Guid? installationMethodId = null,
        bool? isActive = null, 
        CancellationToken cancellationToken = default);
    
    Task<BillingRatecardDto?> GetBillingRatecardByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    
    Task<BillingRatecardDto> CreateBillingRatecardAsync(CreateBillingRatecardDto dto, Guid companyId, CancellationToken cancellationToken = default);
    
    Task<BillingRatecardDto> UpdateBillingRatecardAsync(Guid id, UpdateBillingRatecardDto dto, Guid companyId, CancellationToken cancellationToken = default);
    
    Task DeleteBillingRatecardAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}

