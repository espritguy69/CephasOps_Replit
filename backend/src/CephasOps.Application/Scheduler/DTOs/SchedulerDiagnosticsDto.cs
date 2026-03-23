namespace CephasOps.Application.Scheduler.DTOs;

public class SchedulerDiagnosticsDto
{
    public int PollIntervalSeconds { get; set; }
    public int MaxJobsPerPoll { get; set; }
    public Guid? WorkerId { get; set; }
    public DateTime? LastPollUtc { get; set; }
    public int TotalDiscovered { get; set; }
    public int TotalClaimAttempts { get; set; }
    public int TotalClaimSuccess { get; set; }
    public int TotalClaimFailure { get; set; }
    public IReadOnlyList<Guid> RecentDiscovered { get; set; } = Array.Empty<Guid>();
    public IReadOnlyList<ClaimAttemptDto> RecentClaimAttempts { get; set; } = Array.Empty<ClaimAttemptDto>();
}

public class ClaimAttemptDto
{
    public Guid JobId { get; set; }
    public bool Success { get; set; }
}
