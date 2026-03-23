using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Common.Services;

/// <summary>
/// Resolves full order-derived pricing context in one place.
/// Loads Order (with Partner for PartnerGroupId) and OrderType (with Parent) to populate OrderPricingContext.
/// Preserves parent-order-type behavior: subtype resolves to parent code for scope; own code available via OrderTypeId lookup if needed.
/// </summary>
public class OrderPricingContextResolver : IOrderPricingContextResolver
{
    private readonly ApplicationDbContext _context;

    public OrderPricingContextResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<OrderPricingContext?> ResolveFromOrderAsync(Guid orderId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.CompanyId == companyId)
            .Select(o => new
            {
                o.PartnerId,
                o.DepartmentId,
                o.OrderTypeId,
                o.OrderCategoryId,
                o.InstallationMethodId,
                PartnerGroupId = o.Partner != null ? o.Partner.GroupId : (Guid?)null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            return null;

        var (orderTypeCode, parentOrderTypeCode) = await GetOrderTypeCodesAsync(order.OrderTypeId, cancellationToken);

        return new OrderPricingContext
        {
            PartnerId = order.PartnerId,
            DepartmentId = order.DepartmentId,
            OrderTypeId = order.OrderTypeId,
            OrderTypeCode = orderTypeCode,
            ParentOrderTypeCode = parentOrderTypeCode,
            OrderCategoryId = order.OrderCategoryId,
            InstallationMethodId = order.InstallationMethodId,
            PartnerGroupId = order.PartnerGroupId
        };
    }

    /// <summary>
    /// Returns (scopeCode, parentCode): scopeCode = parent's Code when subtype else own Code; parentCode = parent's Code when subtype else null.
    /// </summary>
    private async Task<(string? ScopeCode, string? ParentCode)> GetOrderTypeCodesAsync(Guid orderTypeId, CancellationToken cancellationToken)
    {
        if (orderTypeId == Guid.Empty)
            return (null, null);

        var orderType = await _context.OrderTypes
            .AsNoTracking()
            .Include(ot => ot.ParentOrderType)
            .FirstOrDefaultAsync(ot => ot.Id == orderTypeId, cancellationToken);

        if (orderType == null)
            return (null, null);

        var isSubtype = orderType.ParentOrderTypeId.HasValue && orderType.ParentOrderType != null;
        var scopeCode = isSubtype ? orderType.ParentOrderType!.Code : orderType.Code;
        var parentCode = isSubtype ? orderType.ParentOrderType!.Code : null;

        return (scopeCode, parentCode);
    }
}
