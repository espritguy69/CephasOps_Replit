using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service interface for parser template operations
/// </summary>
public interface IParserTemplateService
{
    /// <summary>
    /// Get all parser templates, sorted by priority (highest first)
    /// </summary>
    Task<List<ParserTemplateDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parser template by ID
    /// </summary>
    Task<ParserTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get parser template by code
    /// </summary>
    Task<ParserTemplateDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active templates for evaluation
    /// </summary>
    Task<List<ParserTemplateDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Find matching template for an email
    /// </summary>
    /// <param name="fromAddress">Email FROM address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching template or null</returns>
    Task<ParserTemplateDto?> FindMatchingTemplateAsync(string fromAddress, string subject, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find matching template for an email (company-scoped)
    /// </summary>
    /// <param name="fromAddress">Email FROM address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching template or null</returns>
    Task<ParserTemplateDto?> FindMatchingTemplateAsync(string fromAddress, string subject, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find matching template for an email (company and mailbox scoped)
    /// </summary>
    /// <param name="fromAddress">Email FROM address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="emailAccountId">Email account/mailbox ID - only templates assigned to this mailbox (or all) are considered</param>
    /// <param name="hasAttachments">Whether the email has attachments - used to prioritize Excel parser templates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching template or null</returns>
    Task<ParserTemplateDto?> FindMatchingTemplateAsync(string fromAddress, string subject, Guid? companyId, Guid? emailAccountId, bool hasAttachments = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new parser template
    /// </summary>
    Task<ParserTemplateDto> CreateAsync(CreateParserTemplateDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing parser template
    /// </summary>
    Task<ParserTemplateDto> UpdateAsync(Guid id, UpdateParserTemplateDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a parser template
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggle auto-approve setting for a template
    /// </summary>
    Task<ParserTemplateDto> ToggleAutoApproveAsync(Guid id, bool autoApprove, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test a parser template with sample email data
    /// </summary>
    /// <param name="templateId">Template ID to test</param>
    /// <param name="testData">Sample email data (from address, subject, body, attachments)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test result with matching status and extracted data</returns>
    Task<ParserTemplateTestResultDto> TestTemplateAsync(Guid templateId, ParserTemplateTestDataDto testData, CancellationToken cancellationToken = default);
}

