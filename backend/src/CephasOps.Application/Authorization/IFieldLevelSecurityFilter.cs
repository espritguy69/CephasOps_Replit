using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Payroll.DTOs;
using CephasOps.Application.Rates.DTOs;

namespace CephasOps.Application.Authorization;

/// <summary>
/// RBAC v3: Filters sensitive fields on DTOs when the current user lacks field-level permissions.
/// Does not throw; hides or masks fields by setting them to null.
/// </summary>
public interface IFieldLevelSecurityFilter
{
    /// <summary>
    /// Returns true if the current user has the given permission (or is SuperAdmin).
    /// </summary>
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks order financial fields (RevenueAmount, PayoutAmount, ProfitAmount) when user lacks orders.view.price.
    /// </summary>
    Task ApplyOrderDtoAsync(OrderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks order financial fields on a list.
    /// </summary>
    Task ApplyOrderDtosAsync(IEnumerable<OrderDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks PayrollRunDto.TotalAmount and nested Lines when user lacks payroll.view.payout.
    /// </summary>
    Task ApplyPayrollRunDtoAsync(PayrollRunDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks PayrollRunDto pay fields on a list (including nested Lines).
    /// </summary>
    Task ApplyPayrollRunDtosAsync(IEnumerable<PayrollRunDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks PayrollLineDto pay fields when user lacks payroll.view.payout.
    /// </summary>
    Task ApplyPayrollLineDtoAsync(PayrollLineDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks JobEarningRecordDto pay fields when user lacks payroll.view.payout.
    /// </summary>
    Task ApplyJobEarningRecordDtoAsync(JobEarningRecordDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks SiRatePlanDto rate amounts when user lacks payroll.view.payout.
    /// </summary>
    Task ApplySiRatePlanDtoAsync(SiRatePlanDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks SiRatePlanDto rate amounts on a list.
    /// </summary>
    Task ApplySiRatePlanDtosAsync(IEnumerable<SiRatePlanDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks JobEarningRecordDto pay fields on a list.
    /// </summary>
    Task ApplyJobEarningRecordDtosAsync(IEnumerable<JobEarningRecordDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks MaterialDto.DefaultCost on a list.
    /// </summary>
    Task ApplyMaterialDtosAsync(IEnumerable<MaterialDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks BaseWorkRateDto.Amount on a list.
    /// </summary>
    Task ApplyBaseWorkRateDtosAsync(IEnumerable<BaseWorkRateDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks MaterialDto.DefaultCost when user lacks inventory.view.cost.
    /// </summary>
    Task ApplyMaterialDtoAsync(MaterialDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks GponRateResolutionResult revenue/payout/margin when user lacks rates.view.amounts.
    /// </summary>
    Task ApplyGponRateResolutionResultAsync(GponRateResolutionResult dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Masks BaseWorkRateDto.Amount when user lacks rates.view.amounts.
    /// </summary>
    Task ApplyBaseWorkRateDtoAsync(BaseWorkRateDto dto, CancellationToken cancellationToken = default);
}
