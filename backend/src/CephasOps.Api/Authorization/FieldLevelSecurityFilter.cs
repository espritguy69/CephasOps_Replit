using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Inventory.DTOs;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Payroll.DTOs;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Domain.Authorization;

namespace CephasOps.Api.Authorization;

/// <summary>
/// RBAC v3: Masks sensitive fields on DTOs when the current user lacks field-level permissions.
/// SuperAdmin always sees everything.
/// </summary>
public class FieldLevelSecurityFilter : IFieldLevelSecurityFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserPermissionProvider _userPermissionProvider;

    public FieldLevelSecurityFilter(
        ICurrentUserService currentUserService,
        IUserPermissionProvider userPermissionProvider)
    {
        _currentUserService = currentUserService;
        _userPermissionProvider = userPermissionProvider;
    }

    public async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (_currentUserService.IsSuperAdmin)
            return true;
        var userId = _currentUserService.UserId;
        if (userId == null)
            return false;
        var permissions = await _userPermissionProvider.GetPermissionNamesAsync(userId.Value, cancellationToken);
        return permissions.Contains(permission, StringComparer.Ordinal);
    }

    public async Task ApplyOrderDtoAsync(OrderDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.OrdersViewPrice, cancellationToken))
            return;
        dto.RevenueAmount = null;
        dto.PayoutAmount = null;
        dto.ProfitAmount = null;
    }

    public async Task ApplyOrderDtosAsync(IEnumerable<OrderDto> dtos, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.OrdersViewPrice, cancellationToken))
            return;
        foreach (var dto in dtos)
        {
            dto.RevenueAmount = null;
            dto.PayoutAmount = null;
            dto.ProfitAmount = null;
        }
    }

    public async Task ApplyPayrollRunDtoAsync(PayrollRunDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.PayrollViewPayout, cancellationToken))
            return;
        dto.TotalAmount = 0;
        foreach (var line in dto.Lines)
            await ApplyPayrollLineDtoAsync(line, cancellationToken);
    }

    public async Task ApplyPayrollRunDtosAsync(IEnumerable<PayrollRunDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
            await ApplyPayrollRunDtoAsync(dto, cancellationToken);
    }

    public async Task ApplyPayrollLineDtoAsync(PayrollLineDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.PayrollViewPayout, cancellationToken))
            return;
        dto.TotalPay = 0;
        dto.Adjustments = 0;
        dto.NetPay = 0;
    }

    public async Task ApplyJobEarningRecordDtoAsync(JobEarningRecordDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.PayrollViewPayout, cancellationToken))
            return;
        dto.BaseRate = 0;
        dto.KpiAdjustment = 0;
        dto.FinalPay = 0;
    }

    public async Task ApplyJobEarningRecordDtosAsync(IEnumerable<JobEarningRecordDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
            await ApplyJobEarningRecordDtoAsync(dto, cancellationToken);
    }

    public async Task ApplySiRatePlanDtoAsync(SiRatePlanDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.PayrollViewPayout, cancellationToken))
            return;
        dto.PrelaidRate = null;
        dto.NonPrelaidRate = null;
        dto.SduRate = null;
        dto.RdfPoleRate = null;
        dto.ActivationRate = null;
        dto.ModificationRate = null;
        dto.AssuranceRate = null;
        dto.AssuranceRepullRate = null;
        dto.FttrRate = null;
        dto.FttcRate = null;
        dto.OnTimeBonus = null;
        dto.LatePenalty = null;
        dto.ReworkRate = null;
    }

    public async Task ApplySiRatePlanDtosAsync(IEnumerable<SiRatePlanDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
            await ApplySiRatePlanDtoAsync(dto, cancellationToken);
    }

    public async Task ApplyMaterialDtoAsync(MaterialDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.InventoryViewCost, cancellationToken))
            return;
        dto.DefaultCost = null;
    }

    public async Task ApplyMaterialDtosAsync(IEnumerable<MaterialDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
            await ApplyMaterialDtoAsync(dto, cancellationToken);
    }

    public async Task ApplyGponRateResolutionResultAsync(GponRateResolutionResult dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.RatesViewAmounts, cancellationToken))
            return;
        dto.RevenueAmount = null;
        dto.RevenueSource = null;
        dto.RevenueRateId = null;
        dto.PayoutAmount = null;
        dto.PayoutSource = null;
        dto.PayoutRateId = null;
        dto.BaseAmountBeforeModifiers = null;
        dto.PayoutPath = null;
        dto.ResolutionMatchLevel = null;
    }

    public async Task ApplyBaseWorkRateDtoAsync(BaseWorkRateDto dto, CancellationToken cancellationToken = default)
    {
        if (await HasPermissionAsync(PermissionCatalog.RatesViewAmounts, cancellationToken))
            return;
        dto.Amount = 0;
    }

    public async Task ApplyBaseWorkRateDtosAsync(IEnumerable<BaseWorkRateDto> dtos, CancellationToken cancellationToken = default)
    {
        foreach (var dto in dtos)
            await ApplyBaseWorkRateDtoAsync(dto, cancellationToken);
    }
}
