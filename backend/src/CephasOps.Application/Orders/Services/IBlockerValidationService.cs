using CephasOps.Application.Orders.DTOs;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Service for validating blocker timing rules per ORDER_LIFECYCLE.md
/// </summary>
public interface IBlockerValidationService
{
    /// <summary>
    /// Validate if a blocker can be set from the current status with the given reason
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <param name="blockerReason">The blocker reason being set</param>
    /// <param name="blockerCategory">The blocker category</param>
    /// <returns>Validation result with errors if invalid</returns>
    BlockerValidationResult ValidateBlockerTransition(
        string currentStatus,
        string blockerReason,
        string? blockerCategory = null);

    /// <summary>
    /// Get allowed blocker reasons for a given status
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <returns>List of allowed reason codes</returns>
    IEnumerable<string> GetAllowedReasonsForStatus(string currentStatus);

    /// <summary>
    /// Check if blocker can be set from the given status
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <returns>True if blocker transition is allowed</returns>
    bool CanSetBlockerFromStatus(string currentStatus);
}

