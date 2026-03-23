namespace CephasOps.Domain.Orders.Enums;

/// <summary>
/// Blocker category enum per ORDER_LIFECYCLE.md section 3.5.4
/// </summary>
public enum BlockerCategory
{
    /// <summary>
    /// Customer-related blockers (e.g., customer wants to postpone, refuses installation)
    /// </summary>
    Customer,

    /// <summary>
    /// Building/access-related blockers (e.g., MDF locked, security denies entry)
    /// </summary>
    Building,

    /// <summary>
    /// Network/infrastructure blockers (e.g., OLT outage, LOSi/LOBi)
    /// </summary>
    Network,

    /// <summary>
    /// Technical blockers inside unit (e.g., ONU faulty, no feasible cable path)
    /// </summary>
    Technical,

    /// <summary>
    /// SI-related blockers (e.g., SI unavailable, vehicle breakdown)
    /// </summary>
    SI,

    /// <summary>
    /// Weather-related blockers
    /// </summary>
    Weather,

    /// <summary>
    /// Other blockers not fitting above categories
    /// </summary>
    Other
}

