using CephasOps.Application.Events.DTOs;

namespace CephasOps.Application.Events.Replay;

/// <summary>
/// Registry of replay targets with metadata (ordering, capabilities, limitations). Phase 2.
/// </summary>
public interface IReplayTargetRegistry
{
    /// <summary>All registered targets (supported and unsupported).</summary>
    IReadOnlyList<ReplayTargetDescriptorDto> GetAll();

    /// <summary>Get target by id; returns null if not found.</summary>
    ReplayTargetDescriptorDto? GetById(string targetId);

    /// <summary>Returns true if target is supported and can be used for execute/preview.</summary>
    bool IsSupported(string targetId);
}
