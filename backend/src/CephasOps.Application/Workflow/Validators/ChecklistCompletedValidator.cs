using CephasOps.Application.Orders.Services;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Workflow.Validators;

/// <summary>
/// Validator for checking if all required checklist items are completed for an order status
/// Configurable via GuardConditionDefinition.ValidatorConfigJson
/// </summary>
public class ChecklistCompletedValidator : IGuardConditionValidator
{
    public string Key => "checklistCompleted";
    public string EntityType => "Order";

    private readonly ApplicationDbContext _context;
    private readonly IOrderStatusChecklistService _checklistService;
    private readonly ILogger<ChecklistCompletedValidator> _logger;

    public ChecklistCompletedValidator(
        ApplicationDbContext context,
        IOrderStatusChecklistService checklistService,
        ILogger<ChecklistCompletedValidator> logger)
    {
        _context = context;
        _checklistService = checklistService;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        Guid entityId,
        Dictionary<string, object>? config,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == entityId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for checklist validation", entityId);
            return false;
        }

        // Get the status code to check checklist for
        // If config specifies a statusCode, use that; otherwise use current status
        string statusCode = config?.GetValueOrDefault("statusCode")?.ToString() ?? order.Status;

        if (string.IsNullOrEmpty(statusCode))
        {
            _logger.LogWarning("No status code specified for checklist validation on order {OrderId}", entityId);
            return false;
        }

        try
        {
            // Use the checklist service to validate completion
            bool isValid = await _checklistService.ValidateChecklistCompletionAsync(
                entityId,
                statusCode,
                cancellationToken);

            if (!isValid)
            {
                var errors = await _checklistService.GetChecklistValidationErrorsAsync(
                    entityId,
                    statusCode,
                    cancellationToken);

                _logger.LogWarning(
                    "Order {OrderId} checklist validation failed for status {StatusCode}. Errors: {Errors}",
                    entityId,
                    statusCode,
                    string.Join("; ", errors));
            }
            else
            {
                _logger.LogDebug(
                    "Order {OrderId} checklist validation passed for status {StatusCode}",
                    entityId,
                    statusCode);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating checklist for order {OrderId}, status {StatusCode}", entityId, statusCode);
            return false;
        }
    }
}

