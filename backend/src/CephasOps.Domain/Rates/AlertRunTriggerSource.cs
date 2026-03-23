namespace CephasOps.Domain.Rates;

/// <summary>
/// What triggered a payout anomaly alert run.
/// </summary>
public static class AlertRunTriggerSource
{
    public const string Scheduler = "Scheduler";
    public const string Manual = "Manual";
}
