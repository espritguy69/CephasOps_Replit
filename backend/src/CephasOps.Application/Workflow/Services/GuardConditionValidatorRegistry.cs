using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Registry for guard condition validators
/// Loads validators dynamically from settings and executes them
/// No hardcoding - everything comes from database
/// </summary>
public class GuardConditionValidatorRegistry
{
    private readonly Dictionary<string, IGuardConditionValidator> _validators;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GuardConditionValidatorRegistry> _logger;

    public GuardConditionValidatorRegistry(
        IEnumerable<IGuardConditionValidator> validators,
        ApplicationDbContext context,
        ILogger<GuardConditionValidatorRegistry> logger)
    {
        _context = context;
        _logger = logger;
        
        // Register all validators by their Key
        _validators = validators.ToDictionary(v => v.Key, v => v);
        
        _logger.LogInformation(
            "GuardConditionValidatorRegistry initialized with {Count} validators: {Keys}",
            _validators.Count, string.Join(", ", _validators.Keys));
    }

    /// <summary>
    /// Validate a guard condition by loading its definition from settings
    /// </summary>
    public async Task<bool> ValidateAsync(
        Guid companyId,
        string guardConditionKey,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Validating guard condition: {Key} for {EntityType}/{EntityId}",
            guardConditionKey, entityType, entityId);

        // Load guard condition definition from settings
        var definition = await _context.Set<GuardConditionDefinition>()
            .FirstOrDefaultAsync(gcd => 
                gcd.CompanyId == companyId 
                && gcd.Key == guardConditionKey
                && gcd.EntityType == entityType
                && gcd.IsActive
                && !gcd.IsDeleted,
                cancellationToken);

        if (definition == null)
        {
            _logger.LogWarning(
                "Guard condition definition not found: {Key} for entity type {EntityType} in company {CompanyId}",
                guardConditionKey, entityType, companyId);
            return false;
        }

        // Get validator from registry
        if (!_validators.TryGetValue(guardConditionKey, out var validator))
        {
            _logger.LogError(
                "Guard condition validator not registered: {Key}. Available validators: {Available}",
                guardConditionKey, string.Join(", ", _validators.Keys));
            throw new InvalidOperationException(
                $"Guard condition validator '{guardConditionKey}' is not registered. " +
                $"Please ensure the validator is registered in dependency injection. " +
                $"Available validators: {string.Join(", ", _validators.Keys)}");
        }

        // Verify entity type matches
        if (validator.EntityType != entityType)
        {
            _logger.LogError(
                "Validator entity type mismatch: Validator '{Key}' is for '{ValidatorEntityType}' but called for '{EntityType}'",
                guardConditionKey, validator.EntityType, entityType);
            throw new InvalidOperationException(
                $"Validator '{guardConditionKey}' is for entity type '{validator.EntityType}' but called for '{entityType}'");
        }

        // Parse config from JSON
        Dictionary<string, object>? config = null;
        if (!string.IsNullOrEmpty(definition.ValidatorConfigJson))
        {
            try
            {
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    definition.ValidatorConfigJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse validator config JSON for {Key}", guardConditionKey);
                throw new InvalidOperationException(
                    $"Invalid validator configuration JSON for guard condition '{guardConditionKey}': {ex.Message}");
            }
        }

        // Execute validator
        try
        {
            var result = await validator.ValidateAsync(entityId, config, cancellationToken);
            _logger.LogDebug(
                "Guard condition validation result: {Key} = {Result} for {EntityType}/{EntityId}",
                guardConditionKey, result, entityType, entityId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error executing validator {Key} for {EntityType}/{EntityId}",
                guardConditionKey, entityType, entityId);
            throw;
        }
    }
}

