using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Application.Common.Services;

/// <summary>
/// Resolves order scope context (PartnerId, DepartmentId, OrderTypeCode) from an Order entity.
/// OrderTypeCode uses parent order type code when subtype; see WORKFLOW_RESOLUTION_RULES.md.
/// </summary>
public class EffectiveScopeResolver : IEffectiveScopeResolver
{
    private readonly ApplicationDbContext _context;

    public EffectiveScopeResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<EffectiveOrderScope?> ResolveFromEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(entityType, "Order", StringComparison.OrdinalIgnoreCase))
            return null;

        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == entityId)
            .Select(o => new { o.PartnerId, o.DepartmentId, o.OrderTypeId })
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            return null;

        var orderTypeCode = await GetOrderTypeCodeForScopeAsync(order.OrderTypeId, cancellationToken);

        return new EffectiveOrderScope
        {
            PartnerId = order.PartnerId,
            DepartmentId = order.DepartmentId,
            OrderTypeCode = orderTypeCode
        };
    }

    /// <inheritdoc />
    public async Task<string?> GetOrderTypeCodeForScopeAsync(Guid orderTypeId, CancellationToken cancellationToken = default)
    {
        if (orderTypeId == Guid.Empty)
            return null;

        var orderType = await _context.OrderTypes
            .AsNoTracking()
            .Include(ot => ot.ParentOrderType)
            .FirstOrDefaultAsync(ot => ot.Id == orderTypeId, cancellationToken);

        if (orderType == null)
            return null;

        return orderType.ParentOrderTypeId.HasValue && orderType.ParentOrderType != null
            ? orderType.ParentOrderType.Code
            : orderType.Code;
    }
}
