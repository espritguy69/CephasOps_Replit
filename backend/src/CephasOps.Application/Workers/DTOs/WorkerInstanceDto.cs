namespace CephasOps.Application.Workers.DTOs;

public class WorkerInstanceDto
{
    public Guid Id { get; set; }
    public string HostName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Seconds since last heartbeat. Null if never heartbeaten.</summary>
    public double? HeartbeatAgeSeconds { get; set; }
    /// <summary>True when worker has not heartbeaten within configured inactive timeout.</summary>
    public bool IsStale { get; set; }
}

public class WorkerInstanceDetailDto : WorkerInstanceDto
{
    public IReadOnlyList<OwnedReplayOperationDto> OwnedReplayOperations { get; set; } = Array.Empty<OwnedReplayOperationDto>();
    public IReadOnlyList<OwnedRebuildOperationDto> OwnedRebuildOperations { get; set; } = Array.Empty<OwnedRebuildOperationDto>();
}

public class OwnedReplayOperationDto
{
    public Guid OperationId { get; set; }
    public string? State { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
}

public class OwnedRebuildOperationDto
{
    public Guid OperationId { get; set; }
    public string? State { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
}
