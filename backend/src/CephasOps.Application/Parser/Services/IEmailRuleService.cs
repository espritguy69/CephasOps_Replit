using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for email rule operations
/// </summary>
public interface IEmailRuleService
{
    /// <summary>
    /// Get all email rules, sorted by priority (highest first)
    /// </summary>
    Task<List<EmailRuleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get email rule by ID
    /// </summary>
    Task<EmailRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active rules for evaluation
    /// </summary>
    Task<List<EmailRuleDto>> GetActiveRulesAsync(Guid? emailAccountId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new email rule
    /// </summary>
    Task<EmailRuleDto> CreateAsync(CreateEmailRuleDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing email rule
    /// </summary>
    Task<EmailRuleDto> UpdateAsync(Guid id, UpdateEmailRuleDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an email rule
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

