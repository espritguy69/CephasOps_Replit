namespace CephasOps.Application.Workers;

/// <summary>
/// Holds the current process worker id. Set by WorkerHeartbeatHostedService after registration.
/// Registered as singleton; implements IWorkerIdentity for read-only access.
/// </summary>
public sealed class WorkerIdentityHolder : IWorkerIdentity
{
    public Guid? WorkerId { get; private set; }

    public void SetWorkerId(Guid workerId)
    {
        WorkerId = workerId;
    }
}
