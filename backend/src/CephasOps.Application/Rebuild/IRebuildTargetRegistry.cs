namespace CephasOps.Application.Rebuild;

/// <summary>
/// Registry of rebuildable operational state targets. Discoverable; no hardcoded target branching.
/// </summary>
public interface IRebuildTargetRegistry
{
    IReadOnlyList<RebuildTargetDescriptor> GetAll();
    RebuildTargetDescriptor? GetById(string targetId);
    bool IsSupported(string targetId);
}
