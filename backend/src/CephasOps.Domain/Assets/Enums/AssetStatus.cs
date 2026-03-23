namespace CephasOps.Domain.Assets.Enums;

/// <summary>
/// Status of an asset in its lifecycle
/// </summary>
public enum AssetStatus
{
    /// <summary>
    /// Asset is active and in use
    /// </summary>
    Active = 1,

    /// <summary>
    /// Asset is under maintenance/repair
    /// </summary>
    UnderMaintenance = 2,

    /// <summary>
    /// Asset is reserved but not yet deployed
    /// </summary>
    Reserved = 3,

    /// <summary>
    /// Asset is temporarily out of service
    /// </summary>
    OutOfService = 4,

    /// <summary>
    /// Asset has been disposed/sold
    /// </summary>
    Disposed = 5,

    /// <summary>
    /// Asset has been written off
    /// </summary>
    WrittenOff = 6,

    /// <summary>
    /// Asset is pending disposal approval
    /// </summary>
    PendingDisposal = 7
}

