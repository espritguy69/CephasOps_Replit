namespace CephasOps.Domain.Parser.Enums;

/// <summary>
/// Defines the action to take when an email rule matches
/// </summary>
public enum EmailRuleActionType
{
    /// <summary>
    /// Route the email to a specific department
    /// </summary>
    RouteToDepartment = 1,

    /// <summary>
    /// Route the email to a specific user
    /// </summary>
    RouteToUser = 2,

    /// <summary>
    /// Mark the email as VIP only (no routing)
    /// </summary>
    MarkVipOnly = 3,

    /// <summary>
    /// Ignore/skip processing this email
    /// </summary>
    Ignore = 4,

    /// <summary>
    /// Mark as VIP and route to a department
    /// </summary>
    MarkVipAndRouteToDepartment = 5,

    /// <summary>
    /// Mark as VIP and route to a user
    /// </summary>
    MarkVipAndRouteToUser = 6
}

