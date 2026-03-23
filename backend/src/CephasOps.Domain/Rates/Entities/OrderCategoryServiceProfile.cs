using CephasOps.Domain.Common;
using CephasOps.Domain.Orders.Entities;

namespace CephasOps.Domain.Rates.Entities;

/// <summary>
/// Maps an Order Category to a Service Profile. Each order category can belong to at most one
/// service profile per company. Used so pricing can target a profile (e.g. RESIDENTIAL_FIBER)
/// instead of duplicating rates for each category (FTTH, FTTR).
/// </summary>
public class OrderCategoryServiceProfile : CompanyScopedEntity
{
    public Guid OrderCategoryId { get; set; }
    public Guid ServiceProfileId { get; set; }

    public OrderCategory OrderCategory { get; set; } = null!;
    public ServiceProfile ServiceProfile { get; set; } = null!;
}
