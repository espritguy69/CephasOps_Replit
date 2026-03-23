namespace CephasOps.Domain.Orders.Enums;

/// <summary>
/// Blocker reason constants per ORDER_LIFECYCLE.md sections 3.5.2 and 3.5.3.
/// Defines which reasons are allowed Pre-Customer vs Post-Customer.
/// </summary>
public static class BlockerReason
{
    // ========================================
    // DUAL REASONS (Valid both Pre and Post)
    // ========================================

    /// <summary>
    /// Customer wants to postpone - valid both before and after MetCustomer
    /// </summary>
    public const string CustomerWantsToPostpone = "CustomerWantsToPostpone";

    /// <summary>
    /// Customer provided wrong location - valid both before and after MetCustomer
    /// </summary>
    public const string CustomerProvidedWrongLocation = "CustomerProvidedWrongLocation";

    // ========================================
    // PRE-CUSTOMER ONLY REASONS (Before MetCustomer)
    // ========================================

    // Building / Access
    public const string BuildingManagementDeniedAccess = "BuildingManagementDeniedAccess";
    public const string SecurityGuardDeniesEntry = "SecurityGuardDeniesEntry";
    public const string MdfLocked = "MdfLocked";
    public const string IdfLocked = "IdfLocked";
    public const string RiserLocked = "RiserLocked";
    public const string FwsRoomLocked = "FwsRoomLocked";
    public const string NoAccessCardPermission = "NoAccessCardPermission";
    public const string LiftUnavailable = "LiftUnavailable";
    public const string SplitterRoomLocked = "SplitterRoomLocked";
    public const string SplitterMissing = "SplitterMissing";
    public const string WrongBuildingLabel = "WrongBuildingLabel";
    public const string CannotLocateBuildingUnit = "CannotLocateBuildingUnit";

    // Network / Infrastructure (before entering unit)
    public const string OltNetworkOutage = "OltNetworkOutage";
    public const string PolePathBlockedUnsafe = "PolePathBlockedUnsafe";

    // Safety / Environment
    public const string UnsafeEnvironment = "UnsafeEnvironment";

    // ========================================
    // POST-CUSTOMER ONLY REASONS (After MetCustomer)
    // ========================================

    // Customer Rejection
    public const string CustomerDeclinesInstallation = "CustomerDeclinesInstallation";
    public const string CustomerRefusesCablingFee = "CustomerRefusesCablingFee";
    public const string CustomerRefusesMethodOfWork = "CustomerRefusesMethodOfWork";
    public const string CustomerDemandsDifferentRouting = "CustomerDemandsDifferentRouting";
    public const string CustomerInsistsDifferentTime = "CustomerInsistsDifferentTime";

    // Technical Inside Unit
    public const string OnuFaulty = "OnuFaulty";
    public const string RouterFaulty = "RouterFaulty";
    public const string CustomerPowerSocketFaulty = "CustomerPowerSocketFaulty";
    public const string NoFeasibleCablePath = "NoFeasibleCablePath";
    public const string WallNotSuitable = "WallNotSuitable";

    // Network / Light Test
    public const string Losi = "Losi"; // No light
    public const string Lobi = "Lobi"; // Low light
    public const string OltIssueDiscoveredDuringTest = "OltIssueDiscoveredDuringTest";
    public const string PortMismatchAtSplitter = "PortMismatchAtSplitter";

    /// <summary>
    /// Reasons valid for Pre-Customer blockers (status = Assigned or OnTheWay)
    /// </summary>
    public static readonly string[] PreCustomerReasons = new[]
    {
        // Dual reasons
        CustomerWantsToPostpone,
        CustomerProvidedWrongLocation,
        // Building / Access
        BuildingManagementDeniedAccess,
        SecurityGuardDeniesEntry,
        MdfLocked,
        IdfLocked,
        RiserLocked,
        FwsRoomLocked,
        NoAccessCardPermission,
        LiftUnavailable,
        SplitterRoomLocked,
        SplitterMissing,
        WrongBuildingLabel,
        CannotLocateBuildingUnit,
        // Network / Infrastructure
        OltNetworkOutage,
        PolePathBlockedUnsafe,
        // Safety
        UnsafeEnvironment
    };

    /// <summary>
    /// Reasons valid for Post-Customer blockers (status = MetCustomer)
    /// </summary>
    public static readonly string[] PostCustomerReasons = new[]
    {
        // Dual reasons
        CustomerWantsToPostpone,
        CustomerProvidedWrongLocation,
        // Customer Rejection
        CustomerDeclinesInstallation,
        CustomerRefusesCablingFee,
        CustomerRefusesMethodOfWork,
        CustomerDemandsDifferentRouting,
        CustomerInsistsDifferentTime,
        // Technical Inside Unit
        OnuFaulty,
        RouterFaulty,
        CustomerPowerSocketFaulty,
        NoFeasibleCablePath,
        WallNotSuitable,
        // Network / Light Test
        Losi,
        Lobi,
        OltIssueDiscoveredDuringTest,
        PortMismatchAtSplitter
    };

    /// <summary>
    /// All valid blocker reasons
    /// </summary>
    public static readonly string[] AllReasons = PreCustomerReasons
        .Union(PostCustomerReasons)
        .Distinct()
        .ToArray();

    /// <summary>
    /// Check if a reason is valid for Pre-Customer blocker
    /// </summary>
    public static bool IsValidPreCustomerReason(string reason) =>
        PreCustomerReasons.Contains(reason);

    /// <summary>
    /// Check if a reason is valid for Post-Customer blocker
    /// </summary>
    public static bool IsValidPostCustomerReason(string reason) =>
        PostCustomerReasons.Contains(reason);

    /// <summary>
    /// Check if a reason is valid for the given current status
    /// </summary>
    public static bool IsValidReasonForStatus(string reason, string currentStatus)
    {
        if (OrderStatus.IsPreCustomerBlockerContext(currentStatus))
        {
            return IsValidPreCustomerReason(reason);
        }

        if (OrderStatus.IsPostCustomerBlockerContext(currentStatus))
        {
            return IsValidPostCustomerReason(reason);
        }

        return false;
    }

    /// <summary>
    /// Get allowed reasons for a given status
    /// </summary>
    public static string[] GetAllowedReasonsForStatus(string currentStatus)
    {
        if (OrderStatus.IsPreCustomerBlockerContext(currentStatus))
        {
            return PreCustomerReasons;
        }

        if (OrderStatus.IsPostCustomerBlockerContext(currentStatus))
        {
            return PostCustomerReasons;
        }

        return Array.Empty<string>();
    }
}

