namespace CephasOps.Application.Tasks;

/// <summary>
/// Well-known task statuses used across the application.
/// </summary>
public static class TaskStatusConstants
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string OnHold = "OnHold";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static bool IsCompleted(string? status) =>
        string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase);

    public static bool IsCancelled(string? status) =>
        string.Equals(status, Cancelled, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Task priority constants.
/// </summary>
public static class TaskPriorityConstants
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";
    public const string Urgent = "Urgent";
}

