using System.Threading;
using CephasOps.Infrastructure.Persistence;

namespace CephasOps.Application.Tests;

/// <summary>
/// SynchronizationContext that sets TenantScope before each callback and runs the callback inside
/// a freshly captured ExecutionContext so AsyncLocal flows. Use when xUnit does not flow AsyncLocal across await.
/// </summary>
public sealed class TenantPreservingSyncContext : SynchronizationContext
{
    private readonly Guid? _tenantId;
    private readonly SynchronizationContext _inner;

    public TenantPreservingSyncContext(Guid? tenantId, ExecutionContext? _ = null, SynchronizationContext? inner = null)
    {
        _tenantId = tenantId;
        _inner = inner ?? new SynchronizationContext();
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        _inner.Post(_ =>
        {
            var prev = TenantScope.CurrentTenantId;
            try
            {
                TenantScope.CurrentTenantId = _tenantId;
                var ec = ExecutionContext.Capture();
                if (ec != null)
                    ExecutionContext.Run(ec, s => d(s), state);
                else
                    d(state);
            }
            finally
            {
                TenantScope.CurrentTenantId = prev;
            }
        }, null);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        var prev = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _tenantId;
            var ec = ExecutionContext.Capture();
            if (ec != null)
                ExecutionContext.Run(ec, s => d(s), state);
            else
                _inner.Send(d, state);
        }
        finally
        {
            TenantScope.CurrentTenantId = prev;
        }
    }
}
