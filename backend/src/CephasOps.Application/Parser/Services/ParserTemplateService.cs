using System.Text;
using System.Text.RegularExpressions;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Parser template service implementation
/// </summary>
public class ParserTemplateService : IParserTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParserTemplateService> _logger;

    public ParserTemplateService(ApplicationDbContext context, ILogger<ParserTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ParserTemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _context.ParserTemplates
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        // Get email account names for templates that have EmailAccountId
        var emailAccountIds = templates
            .Where(t => t.EmailAccountId.HasValue)
            .Select(t => t.EmailAccountId!.Value)
            .Distinct()
            .ToList();

        var emailAccounts = emailAccountIds.Count > 0
            ? await _context.EmailAccounts
                .Where(e => emailAccountIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return templates.Select(t => MapToDto(t, 
            t.EmailAccountId.HasValue && emailAccounts.ContainsKey(t.EmailAccountId.Value)
                ? emailAccounts[t.EmailAccountId.Value]
                : null)).ToList();
    }

    public async Task<ParserTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _context.ParserTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template == null) return null;

        string? emailAccountName = null;
        if (template.EmailAccountId.HasValue)
        {
            var emailAccount = await _context.EmailAccounts.FirstOrDefaultAsync(e => e.Id == template.EmailAccountId.Value, cancellationToken);
            emailAccountName = emailAccount?.Name;
        }

        return MapToDto(template, emailAccountName);
    }

    public async Task<ParserTemplateDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var template = await _context.ParserTemplates
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

        if (template == null) return null;

        string? emailAccountName = null;
        if (template.EmailAccountId.HasValue)
        {
            var emailAccount = await _context.EmailAccounts.FirstOrDefaultAsync(e => e.Id == template.EmailAccountId.Value, cancellationToken);
            emailAccountName = emailAccount?.Name;
        }

        return MapToDto(template, emailAccountName);
    }

    public async Task<List<ParserTemplateDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _context.ParserTemplates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.Priority)
            .ToListAsync(cancellationToken);

        // Get email account names
        var emailAccountIds = templates
            .Where(t => t.EmailAccountId.HasValue)
            .Select(t => t.EmailAccountId!.Value)
            .Distinct()
            .ToList();

        var emailAccounts = emailAccountIds.Count > 0
            ? await _context.EmailAccounts
                .Where(e => emailAccountIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return templates.Select(t => MapToDto(t,
            t.EmailAccountId.HasValue && emailAccounts.ContainsKey(t.EmailAccountId.Value)
                ? emailAccounts[t.EmailAccountId.Value]
                : null)).ToList();
    }

    public async Task<ParserTemplateDto?> FindMatchingTemplateAsync(string fromAddress, string subject, CancellationToken cancellationToken = default)
    {
        return await FindMatchingTemplateAsync(fromAddress, subject, null, null, false, cancellationToken);
    }

    public async Task<ParserTemplateDto?> FindMatchingTemplateAsync(string fromAddress, string subject, Guid? companyId, CancellationToken cancellationToken = default)
    {
        return await FindMatchingTemplateAsync(fromAddress, subject, companyId, null, false, cancellationToken);
    }

    /// <summary>
    /// Find matching template for an email, optionally filtered by mailbox
    /// </summary>
    /// <param name="fromAddress">Sender email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="companyId">Company ID filter</param>
    /// <param name="emailAccountId">Email account/mailbox ID - if provided, only templates assigned to this mailbox (or all mailboxes) are considered</param>
    /// <param name="hasAttachments">Whether the email has attachments - used to prioritize Excel parser templates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<ParserTemplateDto?> FindMatchingTemplateAsync(string fromAddress, string subject, Guid? companyId, Guid? emailAccountId, bool hasAttachments = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ParserTemplates
            .Where(t => t.IsActive && (t.CompanyId == null || t.CompanyId == companyId));

        // Filter by email account: templates with no EmailAccountId (applies to all) OR matching EmailAccountId
        if (emailAccountId.HasValue)
        {
            query = query.Where(t => t.EmailAccountId == null || t.EmailAccountId == emailAccountId.Value);
        }

        var templates = await query
            .OrderByDescending(t => t.Priority)
            .ToListAsync(cancellationToken);

        // Prioritize templates that expect attachments when email has attachments
        // This ensures Excel parser templates are checked before generic templates
        var activeTemplates = templates
            .Select(t => MapToDto(t, null))
            .OrderByDescending(t => 
            {
                // If email has attachments, prioritize templates that expect attachments (Excel parsers)
                if (hasAttachments && !string.IsNullOrEmpty(t.ExpectedAttachmentTypes))
                {
                    // Check if template expects Excel files
                    var expectsExcel = t.ExpectedAttachmentTypes.Contains(".xls", StringComparison.OrdinalIgnoreCase) ||
                                      t.ExpectedAttachmentTypes.Contains(".xlsx", StringComparison.OrdinalIgnoreCase);
                    return expectsExcel ? 1000 : 0; // High priority for Excel templates
                }
                return 0;
            })
            .ThenByDescending(t => t.Priority)
            .ThenByDescending(t => t.SubjectPattern?.Length ?? 0)
            .ToList();

        foreach (var template in activeTemplates)
        {
            bool fromMatches = string.IsNullOrEmpty(template.PartnerPattern) || 
                               MatchesPattern(fromAddress, template.PartnerPattern);
            bool subjectMatches = string.IsNullOrEmpty(template.SubjectPattern) || 
                                  ContainsPattern(subject, template.SubjectPattern);

            if (fromMatches && subjectMatches)
            {
                _logger.LogDebug("Email matched template {TemplateCode}: FROM={From}, Subject={Subject}, Mailbox={MailboxId}", 
                    template.Code, fromAddress, subject, emailAccountId);
                return template;
            }
        }

        return null;
    }

    public async Task<ParserTemplateDto> CreateAsync(CreateParserTemplateDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Template name is required");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Template code is required");

        // Check for duplicate code
        var existing = await GetByCodeAsync(dto.Code, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Template with code '{dto.Code}' already exists");

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] templateParams = {
            id,
            DBNull.Value, // CompanyId - null for now
            dto.Name,
            dto.Code.ToUpperInvariant(),
            (object?)dto.EmailAccountId ?? DBNull.Value,
            (object?)dto.PartnerPattern ?? DBNull.Value,
            (object?)dto.SubjectPattern ?? DBNull.Value,
            (object?)dto.OrderTypeId ?? DBNull.Value,
            (object?)dto.OrderTypeCode ?? DBNull.Value,
            dto.AutoApprove,
            dto.Priority,
            dto.IsActive,
            (object?)dto.Description ?? DBNull.Value,
            (object?)dto.PartnerId ?? DBNull.Value,
            (object?)dto.DefaultDepartmentId ?? DBNull.Value,
            (object?)dto.ExpectedAttachmentTypes ?? DBNull.Value,
            userId,
            DBNull.Value,
            now,
            now
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""ParserTemplates"" (
                ""Id"", ""CompanyId"", ""Name"", ""Code"", ""EmailAccountId"", ""PartnerPattern"", ""SubjectPattern"",
                ""OrderTypeId"", ""OrderTypeCode"", ""AutoApprove"", ""Priority"", ""IsActive"",
                ""Description"", ""PartnerId"", ""DefaultDepartmentId"", ""ExpectedAttachmentTypes"",
                ""CreatedByUserId"", ""UpdatedByUserId"", ""CreatedAt"", ""UpdatedAt""
            ) VALUES (
                {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}
            )",
            templateParams);

        _logger.LogInformation("Parser template created: {TemplateId}, Code: {Code}, User: {UserId}", id, dto.Code, userId);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created template");
    }

    public async Task<ParserTemplateDto> UpdateAsync(Guid id, UpdateParserTemplateDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Parser template with ID {id} not found");

        // Check for duplicate code if changing
        if (!string.IsNullOrEmpty(dto.Code) && !dto.Code.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await GetByCodeAsync(dto.Code, cancellationToken);
            if (duplicate != null)
                throw new InvalidOperationException($"Template with code '{dto.Code}' already exists");
        }

        var now = DateTime.UtcNow;
        var updates = new List<string> { "\"UpdatedAt\" = {0}", "\"UpdatedByUserId\" = {1}" };
        var parameters = new List<object> { now, userId };
        var paramIndex = 2;

        if (!string.IsNullOrEmpty(dto.Name))
        {
            updates.Add($"\"Name\" = {{{paramIndex++}}}");
            parameters.Add(dto.Name);
        }
        if (!string.IsNullOrEmpty(dto.Code))
        {
            updates.Add($"\"Code\" = {{{paramIndex++}}}");
            parameters.Add(dto.Code.ToUpperInvariant());
        }
        if (dto.EmailAccountId.HasValue)
        {
            updates.Add($"\"EmailAccountId\" = {{{paramIndex++}}}");
            parameters.Add(dto.EmailAccountId.Value == Guid.Empty ? DBNull.Value : dto.EmailAccountId.Value);
        }
        if (dto.PartnerPattern != null)
        {
            updates.Add($"\"PartnerPattern\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.PartnerPattern) ? DBNull.Value : dto.PartnerPattern);
        }
        if (dto.SubjectPattern != null)
        {
            updates.Add($"\"SubjectPattern\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.SubjectPattern) ? DBNull.Value : dto.SubjectPattern);
        }
        if (dto.OrderTypeId.HasValue)
        {
            updates.Add($"\"OrderTypeId\" = {{{paramIndex++}}}");
            parameters.Add(dto.OrderTypeId.Value);
        }
        if (dto.OrderTypeCode != null)
        {
            updates.Add($"\"OrderTypeCode\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.OrderTypeCode) ? DBNull.Value : dto.OrderTypeCode);
        }
        if (dto.AutoApprove.HasValue)
        {
            updates.Add($"\"AutoApprove\" = {{{paramIndex++}}}");
            parameters.Add(dto.AutoApprove.Value);
        }
        if (dto.Priority.HasValue)
        {
            updates.Add($"\"Priority\" = {{{paramIndex++}}}");
            parameters.Add(dto.Priority.Value);
        }
        if (dto.IsActive.HasValue)
        {
            updates.Add($"\"IsActive\" = {{{paramIndex++}}}");
            parameters.Add(dto.IsActive.Value);
        }
        if (dto.Description != null)
        {
            updates.Add($"\"Description\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.Description) ? DBNull.Value : dto.Description);
        }
        if (dto.PartnerId.HasValue)
        {
            updates.Add($"\"PartnerId\" = {{{paramIndex++}}}");
            parameters.Add(dto.PartnerId.Value);
        }
        if (dto.DefaultDepartmentId.HasValue)
        {
            updates.Add($"\"DefaultDepartmentId\" = {{{paramIndex++}}}");
            parameters.Add(dto.DefaultDepartmentId.Value);
        }
        if (dto.ExpectedAttachmentTypes != null)
        {
            updates.Add($"\"ExpectedAttachmentTypes\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.ExpectedAttachmentTypes) ? DBNull.Value : dto.ExpectedAttachmentTypes);
        }

        parameters.Add(id);
        if (existing.CompanyId.HasValue)
        {
            parameters.Add(existing.CompanyId.Value);
            var sql = $"UPDATE \"ParserTemplates\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" = {{{paramIndex + 1}}}";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }
        else
        {
            var sql = $"UPDATE \"ParserTemplates\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" IS NULL";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        _logger.LogInformation("Parser template updated: {TemplateId}, User: {UserId}", id, userId);

        return await GetByIdAsync(id, cancellationToken) ?? existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Parser template with ID {id} not found");

        if (existing.CompanyId.HasValue)
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ParserTemplates"" WHERE ""Id"" = {0} AND ""CompanyId"" = {1}",
                id, existing.CompanyId.Value);
        else
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ParserTemplates"" WHERE ""Id"" = {0} AND ""CompanyId"" IS NULL",
                id);

        _logger.LogInformation("Parser template deleted: {TemplateId}, Code: {Code}", id, existing.Code);
    }

    public async Task<ParserTemplateDto> ToggleAutoApproveAsync(Guid id, bool autoApprove, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"Parser template with ID {id} not found");

        var now = DateTime.UtcNow;

        if (existing.CompanyId.HasValue)
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""ParserTemplates"" SET ""AutoApprove"" = {0}, ""UpdatedAt"" = {1}, ""UpdatedByUserId"" = {2} WHERE ""Id"" = {3} AND ""CompanyId"" = {4}",
                autoApprove, now, userId, id, existing.CompanyId.Value);
        else
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""ParserTemplates"" SET ""AutoApprove"" = {0}, ""UpdatedAt"" = {1}, ""UpdatedByUserId"" = {2} WHERE ""Id"" = {3} AND ""CompanyId"" IS NULL",
                autoApprove, now, userId, id);

        _logger.LogInformation("Parser template auto-approve toggled: {TemplateId}, AutoApprove: {AutoApprove}, User: {UserId}", 
            id, autoApprove, userId);

        return await GetByIdAsync(id, cancellationToken) ?? existing;
    }

    /// <summary>
    /// Match a value against a wildcard pattern (supports *, ?, and % for SQL-style compatibility)
    /// </summary>
    private static bool MatchesPattern(string value, string pattern)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            return false;

        // Convert wildcard pattern to regex
        // Support both * (wildcard) and % (SQL-style) for backward compatibility
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\%", ".*")  // Treat % as * for SQL-style patterns
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Check if value contains the pattern (case-insensitive)
    /// </summary>
    private static bool ContainsPattern(string value, string pattern)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            return false;

        var normalizedValue = NormalizePatternValue(value);
        var normalizedPattern = NormalizePatternValue(pattern);

        if (string.IsNullOrEmpty(normalizedValue) || string.IsNullOrEmpty(normalizedPattern))
            return false;

        // Support multiple patterns separated by | (OR logic)
        // This allows patterns like "FTTH|Activation|Service Order"
        var patterns = normalizedPattern.Split('|', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var singlePattern in patterns)
        {
            var trimmedPattern = singlePattern.Trim();
            if (!string.IsNullOrEmpty(trimmedPattern) && 
                normalizedValue.Contains(trimmedPattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }

    private static string NormalizePatternValue(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.Length > 0
            ? builder.ToString()
            : input.Trim().ToLowerInvariant();
    }

    private static ParserTemplateDto MapToDto(ParserTemplate template, string? emailAccountName = null)
    {
        return new ParserTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Name = template.Name,
            Code = template.Code,
            EmailAccountId = template.EmailAccountId,
            EmailAccountName = emailAccountName,
            PartnerPattern = template.PartnerPattern,
            SubjectPattern = template.SubjectPattern,
            OrderTypeId = template.OrderTypeId,
            OrderTypeCode = template.OrderTypeCode,
            AutoApprove = template.AutoApprove,
            Priority = template.Priority,
            IsActive = template.IsActive,
            Description = template.Description,
            PartnerId = template.PartnerId,
            DefaultDepartmentId = template.DefaultDepartmentId,
            ExpectedAttachmentTypes = template.ExpectedAttachmentTypes,
            CreatedByUserId = template.CreatedByUserId,
            UpdatedByUserId = template.UpdatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    public async Task<ParserTemplateTestResultDto> TestTemplateAsync(Guid templateId, ParserTemplateTestDataDto testData, CancellationToken cancellationToken = default)
    {
        var template = await GetByIdAsync(templateId, cancellationToken);
        if (template == null)
        {
            return new ParserTemplateTestResultDto
            {
                Matched = false,
                ErrorMessage = $"Template with ID {templateId} not found"
            };
        }

        var result = new ParserTemplateTestResultDto
        {
            MatchedTemplate = template
        };

        // Check if FROM address matches
        bool fromMatches = string.IsNullOrEmpty(template.PartnerPattern) ||
                          MatchesPattern(testData.FromAddress, template.PartnerPattern);

        // Check if subject matches
        bool subjectMatches = string.IsNullOrEmpty(template.SubjectPattern) ||
                             ContainsPattern(testData.Subject, template.SubjectPattern);

        // Check attachment requirements
        bool attachmentMatches = true;
        if (!string.IsNullOrEmpty(template.ExpectedAttachmentTypes))
        {
            var expectsAttachments = !string.IsNullOrWhiteSpace(template.ExpectedAttachmentTypes);
            attachmentMatches = !expectsAttachments || testData.HasAttachments;
        }

        result.Matched = fromMatches && subjectMatches && attachmentMatches;

        result.MatchDetails = new TemplateMatchDetailsDto
        {
            FromAddressMatched = fromMatches,
            SubjectMatched = subjectMatches,
            FromAddressPattern = template.PartnerPattern,
            SubjectPattern = template.SubjectPattern,
            Priority = template.Priority
        };

        if (!result.Matched)
        {
            var reasons = new List<string>();
            if (!fromMatches) reasons.Add($"FROM address '{testData.FromAddress}' does not match pattern '{template.PartnerPattern}'");
            if (!subjectMatches) reasons.Add($"Subject '{testData.Subject}' does not match pattern '{template.SubjectPattern}'");
            if (!attachmentMatches) reasons.Add($"Template expects attachments but test data has none");
            result.ErrorMessage = string.Join("; ", reasons);
        }
        else
        {
            // If matched, provide sample extracted data structure
            result.ExtractedData = new Dictionary<string, object>
            {
                { "TemplateCode", template.Code },
                { "TemplateName", template.Name },
                { "OrderTypeCode", template.OrderTypeCode ?? "N/A" },
                { "FromAddress", testData.FromAddress },
                { "Subject", testData.Subject },
                { "HasBody", !string.IsNullOrWhiteSpace(testData.Body) },
                { "HasAttachments", testData.HasAttachments },
                { "AttachmentCount", testData.AttachmentFileNames.Count }
            };
        }

        return result;
    }
}

