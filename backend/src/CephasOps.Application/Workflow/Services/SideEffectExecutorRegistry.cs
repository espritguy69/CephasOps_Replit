using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Interfaces;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.Services;

/// <summary>
/// Registry for side effect executors
/// Loads executors dynamically from settings and executes them
/// No hardcoding - everything comes from database
/// </summary>
public class SideEffectExecutorRegistry
{
    private readonly Dictionary<string, ISideEffectExecutor> _executors;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SideEffectExecutorRegistry> _logger;

    public SideEffectExecutorRegistry(
        IEnumerable<ISideEffectExecutor> executors,
        ApplicationDbContext context,
        ILogger<SideEffectExecutorRegistry> logger)
    {
        _context = context;
        _logger = logger;
        
        // Register all executors by their Key
        _executors = executors.ToDictionary(e => e.Key, e => e);
        
        _logger.LogInformation(
            "SideEffectExecutorRegistry initialized with {Count} executors: {Keys}",
            _executors.Count, string.Join(", ", _executors.Keys));
    }

    /// <summary>
    /// Execute a side effect by loading its definition from settings
    /// </summary>
    public async Task ExecuteAsync(
        Guid companyId,
        string sideEffectKey,
        string entityType,
        Guid entityId,
        WorkflowTransitionDto transition,
        Dictionary<string, object>? payload,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing side effect: {Key} for {EntityType}/{EntityId}",
            sideEffectKey, entityType, entityId);

        // Load side effect definition from settings
        var definition = await _context.Set<SideEffectDefinition>()
            .FirstOrDefaultAsync(sed => 
                sed.CompanyId == companyId 
                && sed.Key == sideEffectKey
                && sed.EntityType == entityType
                && sed.IsActive
                && !sed.IsDeleted,
                cancellationToken);

        if (definition == null)
        {
            _logger.LogWarning(
                "Side effect definition not found: {Key} for entity type {EntityType} in company {CompanyId}",
                sideEffectKey, entityType, companyId);
            return;
        }

        // Get executor from registry
        if (!_executors.TryGetValue(sideEffectKey, out var executor))
        {
            _logger.LogError(
                "Side effect executor not registered: {Key}. Available executors: {Available}",
                sideEffectKey, string.Join(", ", _executors.Keys));
            throw new InvalidOperationException(
                $"Side effect executor '{sideEffectKey}' is not registered. " +
                $"Please ensure the executor is registered in dependency injection. " +
                $"Available executors: {string.Join(", ", _executors.Keys)}");
        }

        // Verify entity type matches
        if (executor.EntityType != entityType)
        {
            _logger.LogError(
                "Executor entity type mismatch: Executor '{Key}' is for '{ExecutorEntityType}' but called for '{EntityType}'",
                sideEffectKey, executor.EntityType, entityType);
            throw new InvalidOperationException(
                $"Executor '{sideEffectKey}' is for entity type '{executor.EntityType}' but called for '{entityType}'");
        }

        // Parse config from JSON
        Dictionary<string, object>? config = null;
        if (!string.IsNullOrEmpty(definition.ExecutorConfigJson))
        {
            try
            {
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    definition.ExecutorConfigJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse executor config JSON for {Key}", sideEffectKey);
                throw new InvalidOperationException(
                    $"Invalid executor configuration JSON for side effect '{sideEffectKey}': {ex.Message}");
            }
        }

        // Execute side effect
        try
        {
            await executor.ExecuteAsync(entityId, transition, payload, config, cancellationToken);
            _logger.LogDebug(
                "Side effect executed successfully: {Key} for {EntityType}/{EntityId}",
                sideEffectKey, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error executing side effect {Key} for {EntityType}/{EntityId}",
                sideEffectKey, entityType, entityId);
            throw;
        }
    }
}

