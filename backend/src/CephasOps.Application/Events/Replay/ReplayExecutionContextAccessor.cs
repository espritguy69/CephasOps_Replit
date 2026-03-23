using System.Runtime.CompilerServices;
using CephasOps.Domain.Events;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// AsyncLocal-based accessor for replay execution context. Set before replay dispatch, clear after.
/// </summary>
public sealed class ReplayExecutionContextAccessor : IReplayExecutionContextAccessor
{
    private static readonly AsyncLocal<IReplayExecutionContext?> AsyncContext = new();

    public IReplayExecutionContext? Current => AsyncContext.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(IReplayExecutionContext? context)
    {
        AsyncContext.Value = context;
    }
}
