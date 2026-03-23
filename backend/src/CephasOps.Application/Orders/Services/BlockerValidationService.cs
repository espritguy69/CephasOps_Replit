using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Orders.Enums;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Orders.Services;

/// <summary>
/// Service for validating blocker timing rules per ORDER_LIFECYCLE.md sections 3.5.2 and 3.5.3.
/// 
/// Key rules:
/// - Pre-Customer Blocker: status must be Assigned or OnTheWay
/// - Post-Customer Blocker: status must be MetCustomer
/// - Each blocker type has specific allowed reasons
/// </summary>
public class BlockerValidationService : IBlockerValidationService
{
    private readonly ILogger<BlockerValidationService> _logger;

    public BlockerValidationService(ILogger<BlockerValidationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public BlockerValidationResult ValidateBlockerTransition(
        string currentStatus,
        string blockerReason,
        string? blockerCategory = null)
    {
        _logger.LogDebug(
            "Validating blocker transition from status {CurrentStatus} with reason {BlockerReason}",
            currentStatus, blockerReason);

        // Check if blocker can be set from current status
        if (!CanSetBlockerFromStatus(currentStatus))
        {
            return BlockerValidationResult.Failure(
                $"Cannot set blocker from status '{currentStatus}'. " +
                $"Blocker can only be set from statuses: {string.Join(", ", OrderStatus.PreCustomerBlockerAllowedStatuses.Concat(OrderStatus.PostCustomerBlockerAllowedStatuses))}");
        }

        // Determine blocker context
        var isPreCustomer = OrderStatus.IsPreCustomerBlockerContext(currentStatus);
        var blockerContext = isPreCustomer ? "PreCustomer" : "PostCustomer";
        var allowedReasons = GetAllowedReasonsForStatus(currentStatus).ToList();

        // Validate reason against context
        if (!BlockerReason.IsValidReasonForStatus(blockerReason, currentStatus))
        {
            var contextDescription = isPreCustomer
                ? "Pre-Customer blocker (status is Assigned or OnTheWay)"
                : "Post-Customer blocker (status is MetCustomer)";

            _logger.LogWarning(
                "Invalid blocker reason '{BlockerReason}' for {BlockerContext} context (current status: {CurrentStatus})",
                blockerReason, blockerContext, currentStatus);

            return BlockerValidationResult.Failure(
                blockerContext,
                allowedReasons,
                $"Reason '{blockerReason}' is not valid for {contextDescription}.",
                $"Allowed reasons for {blockerContext} blocker: {string.Join(", ", allowedReasons)}");
        }

        // Validate category if provided
        if (!string.IsNullOrEmpty(blockerCategory))
        {
            if (!Enum.TryParse<BlockerCategory>(blockerCategory, ignoreCase: true, out _))
            {
                var validCategories = Enum.GetNames<BlockerCategory>();
                return BlockerValidationResult.Failure(
                    blockerContext,
                    allowedReasons,
                    $"Invalid blocker category '{blockerCategory}'. Valid categories: {string.Join(", ", validCategories)}");
            }
        }

        _logger.LogInformation(
            "Blocker validation passed for {BlockerContext} context with reason {BlockerReason}",
            blockerContext, blockerReason);

        return BlockerValidationResult.Success(blockerContext, allowedReasons);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllowedReasonsForStatus(string currentStatus)
    {
        return BlockerReason.GetAllowedReasonsForStatus(currentStatus);
    }

    /// <inheritdoc />
    public bool CanSetBlockerFromStatus(string currentStatus)
    {
        return OrderStatus.CanSetBlocker(currentStatus);
    }
}

