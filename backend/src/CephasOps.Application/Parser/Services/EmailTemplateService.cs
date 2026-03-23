using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Email template service implementation
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(
        ApplicationDbContext context,
        ILogger<EmailTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EmailTemplateDto>> GetAllAsync(string? direction = null, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<EmailTemplateDto>();

        var query = _context.EmailTemplates
            .Where(t => t.CompanyId == effectiveCompanyId.Value || t.CompanyId == null);

        if (!string.IsNullOrWhiteSpace(direction))
        {
            query = query.Where(t => t.Direction == direction);
        }

        var templates = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Name)
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

        // Get department names
        var departmentIds = templates
            .Where(t => t.DepartmentId.HasValue)
            .Select(t => t.DepartmentId!.Value)
            .Distinct()
            .ToList();

        var departments = departmentIds.Count > 0
            ? await _context.Departments
                .Where(d => departmentIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken)
            : new Dictionary<Guid, string>();

        return templates.Select(t => MapToDto(t,
            t.EmailAccountId.HasValue && emailAccounts.ContainsKey(t.EmailAccountId.Value)
                ? emailAccounts[t.EmailAccountId.Value]
                : null,
            t.DepartmentId.HasValue && departments.ContainsKey(t.DepartmentId.Value)
                ? departments[t.DepartmentId.Value]
                : null)).ToList();
    }

    public async Task<EmailTemplateDto?> GetByIdAsync(Guid id, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template == null)
            return null;

        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (template.CompanyId.HasValue && template.CompanyId.Value != Guid.Empty)
        {
            if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value != template.CompanyId.Value)
                return null;
        }
        else
        {
            if (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
            {
            }
        }

        string? emailAccountName = null;
        if (template.EmailAccountId.HasValue)
        {
            var account = await _context.EmailAccounts
                .FirstOrDefaultAsync(e => e.Id == template.EmailAccountId.Value, cancellationToken);
            emailAccountName = account?.Name;
        }

        string? departmentName = null;
        if (template.DepartmentId.HasValue)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == template.DepartmentId.Value, cancellationToken);
            departmentName = department?.Name;
        }

        return MapToDto(template, emailAccountName, departmentName);
    }

    public async Task<EmailTemplateDto?> GetByCodeAsync(string code, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var codeUpper = code.ToUpperInvariant();
        if (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
        {
            var tenantTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Code == codeUpper && t.CompanyId == effectiveCompanyId.Value, cancellationToken);
            if (tenantTemplate != null)
                return await GetByIdAsync(tenantTemplate.Id, companyId, cancellationToken);
        }
        var platformTemplate = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Code == codeUpper && t.CompanyId == null, cancellationToken);
        if (platformTemplate == null)
            return null;
        return await GetByIdAsync(platformTemplate.Id, companyId, cancellationToken);
    }

    public async Task<List<EmailTemplateDto>> GetActiveByEntityTypeAsync(string entityType, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            return new List<EmailTemplateDto>();

        var templates = await _context.EmailTemplates
            .Where(t => (t.CompanyId == effectiveCompanyId.Value || t.CompanyId == null) &&
                        t.IsActive &&
                        (t.RelatedEntityType == null || t.RelatedEntityType == entityType))
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.Select(t => MapToDto(t, null, null)).ToList();
    }

    public async Task<EmailTemplateDto> CreateAsync(CreateEmailTemplateDto dto, Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value == Guid.Empty)
            throw new InvalidOperationException("Company context is required to create an email template.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Template name is required");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Template code is required");
        if (string.IsNullOrWhiteSpace(dto.SubjectTemplate))
            throw new InvalidOperationException("Subject template is required");
        if (string.IsNullOrWhiteSpace(dto.BodyTemplate))
            throw new InvalidOperationException("Body template is required");

        var codeUpper = dto.Code.ToUpperInvariant();
        var duplicate = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.CompanyId == effectiveCompanyId.Value && t.Code == codeUpper, cancellationToken);
        if (duplicate != null)
            throw new InvalidOperationException($"Template with code '{dto.Code}' already exists");

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] templateParams = {
            id,
            effectiveCompanyId.Value,
            dto.Name,
            dto.Code.ToUpperInvariant(),
            (object?)dto.EmailAccountId ?? DBNull.Value,
            dto.SubjectTemplate,
            dto.BodyTemplate,
            (object?)dto.DepartmentId ?? DBNull.Value,
            (object?)dto.RelatedEntityType ?? DBNull.Value,
            dto.Priority,
            dto.IsActive,
            dto.AutoProcessReplies,
            (object?)dto.ReplyPattern ?? DBNull.Value,
            (object?)dto.Description ?? DBNull.Value,
            dto.Direction ?? "Outgoing",
            userId,
            DBNull.Value,
            now,
            now
        };

        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""EmailTemplates"" (
                ""Id"", ""CompanyId"", ""Name"", ""Code"", ""EmailAccountId"", ""SubjectTemplate"", ""BodyTemplate"",
                ""DepartmentId"", ""RelatedEntityType"", ""Priority"", ""IsActive"", ""AutoProcessReplies"",
                ""ReplyPattern"", ""Description"", ""Direction"", ""CreatedByUserId"", ""UpdatedByUserId"", ""CreatedAt"", ""UpdatedAt""
            ) VALUES (
                {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}
            )",
            templateParams);

        _logger.LogInformation("Email template created: {TemplateId}, Code: {Code}, User: {UserId}", id, dto.Code, userId);

        return await GetByIdAsync(id, companyId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created template");
    }

    public async Task<EmailTemplateDto> UpdateAsync(Guid id, UpdateEmailTemplateDto dto, Guid userId, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var template = await _context.EmailTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template == null)
            throw new KeyNotFoundException($"Email template with ID {id} not found");
        if (template.CompanyId.HasValue && template.CompanyId.Value != Guid.Empty)
        {
            if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value != template.CompanyId.Value)
                throw new KeyNotFoundException($"Email template with ID {id} not found");
        }
        else
        {
            if (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
                throw new KeyNotFoundException($"Email template with ID {id} not found");
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
        if (!string.IsNullOrEmpty(dto.SubjectTemplate))
        {
            updates.Add($"\"SubjectTemplate\" = {{{paramIndex++}}}");
            parameters.Add(dto.SubjectTemplate);
        }
        if (!string.IsNullOrEmpty(dto.BodyTemplate))
        {
            updates.Add($"\"BodyTemplate\" = {{{paramIndex++}}}");
            parameters.Add(dto.BodyTemplate);
        }
        if (dto.EmailAccountId.HasValue)
        {
            updates.Add($"\"EmailAccountId\" = {{{paramIndex++}}}");
            parameters.Add(dto.EmailAccountId.Value == Guid.Empty ? DBNull.Value : dto.EmailAccountId.Value);
        }
        if (dto.DepartmentId.HasValue)
        {
            updates.Add($"\"DepartmentId\" = {{{paramIndex++}}}");
            parameters.Add(dto.DepartmentId.Value == Guid.Empty ? DBNull.Value : dto.DepartmentId.Value);
        }
        if (dto.RelatedEntityType != null)
        {
            updates.Add($"\"RelatedEntityType\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.RelatedEntityType) ? DBNull.Value : dto.RelatedEntityType);
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
        if (dto.AutoProcessReplies.HasValue)
        {
            updates.Add($"\"AutoProcessReplies\" = {{{paramIndex++}}}");
            parameters.Add(dto.AutoProcessReplies.Value);
        }
        if (dto.ReplyPattern != null)
        {
            updates.Add($"\"ReplyPattern\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.ReplyPattern) ? DBNull.Value : dto.ReplyPattern);
        }
        if (dto.Description != null)
        {
            updates.Add($"\"Description\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.Description) ? DBNull.Value : dto.Description);
        }
        if (dto.Direction != null)
        {
            updates.Add($"\"Direction\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.Direction) ? "Outgoing" : dto.Direction);
        }

        parameters.Add(id);
        var updateSql = string.Join(", ", updates) + " WHERE \"Id\" = {" + paramIndex + "}";

        // Using parameterized query - safe from SQL injection
        // EF Core warning is false positive since we're using placeholders {0}, {1}, etc. with parameters
#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync(
            $@"UPDATE ""EmailTemplates"" SET {updateSql}",
            parameters.ToArray());
#pragma warning restore EF1002

        _logger.LogInformation("Email template updated: {TemplateId}, User: {UserId}", id, userId);

        return await GetByIdAsync(id, companyId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated template");
    }

    public async Task DeleteAsync(Guid id, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template == null)
            throw new KeyNotFoundException($"Email template with ID {id} not found");
        if (template.CompanyId.HasValue && template.CompanyId.Value != Guid.Empty)
        {
            if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value != template.CompanyId.Value)
                throw new KeyNotFoundException($"Email template with ID {id} not found");
        }
        else
        {
            if (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
                throw new KeyNotFoundException($"Email template with ID {id} not found");
        }

        _context.EmailTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email template deleted: {TemplateId}", id);
    }

    public async Task<(string Subject, string Body)> RenderTemplateAsync(Guid templateId, Dictionary<string, string> placeholders, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var effectiveCompanyId = companyId ?? CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId;
        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
            throw new KeyNotFoundException($"Email template with ID {templateId} not found");
        if (template.CompanyId.HasValue && template.CompanyId.Value != Guid.Empty)
        {
            if (!effectiveCompanyId.HasValue || effectiveCompanyId.Value != template.CompanyId.Value)
                throw new KeyNotFoundException($"Email template with ID {templateId} not found");
        }
        else
        {
            if (effectiveCompanyId.HasValue && effectiveCompanyId.Value != Guid.Empty)
            {
            }
        }

        var subject = ReplacePlaceholders(template.SubjectTemplate, placeholders);
        var body = ReplacePlaceholders(template.BodyTemplate, placeholders);

        return (subject, body);
    }

    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        if (placeholders == null || placeholders.Count == 0)
            return template;

        var result = template;
        foreach (var placeholder in placeholders)
        {
            result = result.Replace($"{{{placeholder.Key}}}", placeholder.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static EmailTemplateDto MapToDto(EmailTemplate template, string? emailAccountName, string? departmentName)
    {
        return new EmailTemplateDto
        {
            Id = template.Id,
            CompanyId = template.CompanyId,
            Name = template.Name,
            Code = template.Code,
            EmailAccountId = template.EmailAccountId,
            EmailAccountName = emailAccountName,
            SubjectTemplate = template.SubjectTemplate,
            BodyTemplate = template.BodyTemplate,
            DepartmentId = template.DepartmentId,
            DepartmentName = departmentName,
            RelatedEntityType = template.RelatedEntityType,
            Priority = template.Priority,
            IsActive = template.IsActive,
            AutoProcessReplies = template.AutoProcessReplies,
            ReplyPattern = template.ReplyPattern,
            Description = template.Description,
            Direction = template.Direction,
            CreatedByUserId = template.CreatedByUserId,
            UpdatedByUserId = template.UpdatedByUserId,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}

