using CephasOps.Application.Common.DTOs;

namespace CephasOps.Application.Common.Interfaces;

/// <summary>
/// Resolves the full order-derived pricing context from an Order.
/// Centralizes derivation of PartnerId, DepartmentId, OrderTypeId, OrderTypeCode (scope),
/// ParentOrderTypeCode, OrderCategoryId, InstallationMethodId, and PartnerGroupId.
/// Used for workflow scope and as a single source for pricing-driving fields; does not change rate or billing selection logic.
/// </summary>
public interface IOrderPricingContextResolver
{
    /// <summary>
    /// Resolve pricing context from an order. Returns null when the order is not found or not in the given company.
    /// OrderTypeCode is parent code when order type is a subtype, else own code; ParentOrderTypeCode is set when subtype.
    /// </summary>
    Task<OrderPricingContext?> ResolveFromOrderAsync(Guid orderId, Guid companyId, CancellationToken cancellationToken = default);
}
