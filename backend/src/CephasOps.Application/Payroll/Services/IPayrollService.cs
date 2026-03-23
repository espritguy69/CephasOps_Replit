using CephasOps.Application.Payroll.DTOs;

namespace CephasOps.Application.Payroll.Services;

/// <summary>
/// Payroll service interface
/// </summary>
public interface IPayrollService
{
    Task<List<PayrollPeriodDto>> GetPayrollPeriodsAsync(Guid companyId, string? year = null, string? status = null, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto?> GetPayrollPeriodByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<PayrollPeriodDto> CreatePayrollPeriodAsync(CreatePayrollPeriodDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    
    Task<List<PayrollRunDto>> GetPayrollRunsAsync(Guid companyId, Guid? periodId = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<PayrollRunDto?> GetPayrollRunByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> CreatePayrollRunAsync(CreatePayrollRunDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> FinalizePayrollRunAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<PayrollRunDto> MarkPayrollRunPaidAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    
    Task<List<JobEarningRecordDto>> GetJobEarningRecordsAsync(Guid companyId, Guid? siId = null, string? period = null, Guid? orderId = null, CancellationToken cancellationToken = default);
    
    Task<List<SiRatePlanDto>> GetSiRatePlansAsync(Guid companyId, Guid? siId = null, Guid? departmentId = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<SiRatePlanDto?> GetSiRatePlanByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
    Task<SiRatePlanDto> CreateSiRatePlanAsync(CreateSiRatePlanDto dto, Guid companyId, CancellationToken cancellationToken = default);
    Task<SiRatePlanDto> UpdateSiRatePlanAsync(Guid id, CreateSiRatePlanDto dto, Guid companyId, CancellationToken cancellationToken = default);
    Task DeleteSiRatePlanAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Create payroll period request DTO
/// </summary>
public class CreatePayrollPeriodDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

