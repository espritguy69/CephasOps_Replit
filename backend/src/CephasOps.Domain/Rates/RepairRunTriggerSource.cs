namespace CephasOps.Domain.Rates;

/// <summary>
/// What triggered a payout snapshot repair run.
/// </summary>
public static class RepairRunTriggerSource
{
    public const string Scheduler = "Scheduler";
    public const string Manual = "Manual";
}
