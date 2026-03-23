using CephasOps.Application.Parser.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Email rule service implementation
/// </summary>
public class EmailRuleService : IEmailRuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailRuleService> _logger;

    public EmailRuleService(ApplicationDbContext context, ILogger<EmailRuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EmailRuleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _context.ParserRules
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToDto).ToList();
    }

    public async Task<EmailRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _context.ParserRules
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return rule != null ? MapToDto(rule) : null;
    }

    public async Task<List<EmailRuleDto>> GetActiveRulesAsync(Guid? emailAccountId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ParserRules
            .Where(r => r.IsActive);

        if (emailAccountId.HasValue)
        {
            query = query.Where(r => r.EmailAccountId == null || r.EmailAccountId == emailAccountId.Value);
        }

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

        return rules.Select(MapToDto).ToList();
    }

    public async Task<EmailRuleDto> CreateAsync(CreateEmailRuleDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] ruleParams = {
            id,
            DBNull.Value, // CompanyId - null for now
            (object?)dto.EmailAccountId ?? DBNull.Value,
            (object?)dto.FromAddressPattern ?? DBNull.Value,
            (object?)dto.DomainPattern ?? DBNull.Value,
            (object?)dto.SubjectContains ?? DBNull.Value,
            dto.IsVip,
            (object?)dto.TargetDepartmentId ?? DBNull.Value,
            (object?)dto.TargetUserId ?? DBNull.Value,
            dto.ActionType,
            dto.Priority,
            dto.IsActive,
            (object?)dto.Description ?? DBNull.Value,
            userId,
            DBNull.Value,
            now,
            now
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""ParserRules"" (
                ""Id"", ""CompanyId"", ""EmailAccountId"", ""FromAddressPattern"", ""DomainPattern"", 
                ""SubjectContains"", ""IsVip"", ""TargetDepartmentId"", ""TargetUserId"", 
                ""ActionType"", ""Priority"", ""IsActive"", ""Description"", 
                ""CreatedByUserId"", ""UpdatedByUserId"", ""CreatedAt"", ""UpdatedAt""
            ) VALUES (
                {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}
            )",
            ruleParams);

        _logger.LogInformation("Email rule created: {RuleId}, User: {UserId}", id, userId);

        return await GetByIdAsync(id, cancellationToken) 
            ?? throw new InvalidOperationException("Failed to retrieve created rule");
    }

    public async Task<EmailRuleDto> UpdateAsync(Guid id, UpdateEmailRuleDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Email rule with ID {id} not found");
        }

        var now = DateTime.UtcNow;
        var updates = new List<string> { "\"UpdatedAt\" = {0}", "\"UpdatedByUserId\" = {1}" };
        var parameters = new List<object> { now, userId };
        var paramIndex = 2;

        if (dto.EmailAccountId.HasValue || dto.EmailAccountId == null)
        {
            updates.Add($"\"EmailAccountId\" = {{{paramIndex++}}}");
            parameters.Add((object?)dto.EmailAccountId ?? DBNull.Value);
        }
        if (dto.FromAddressPattern != null)
        {
            updates.Add($"\"FromAddressPattern\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.FromAddressPattern) ? DBNull.Value : dto.FromAddressPattern);
        }
        if (dto.DomainPattern != null)
        {
            updates.Add($"\"DomainPattern\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.DomainPattern) ? DBNull.Value : dto.DomainPattern);
        }
        if (dto.SubjectContains != null)
        {
            updates.Add($"\"SubjectContains\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.SubjectContains) ? DBNull.Value : dto.SubjectContains);
        }
        if (dto.IsVip.HasValue)
        {
            updates.Add($"\"IsVip\" = {{{paramIndex++}}}");
            parameters.Add(dto.IsVip.Value);
        }
        if (dto.TargetDepartmentId.HasValue)
        {
            updates.Add($"\"TargetDepartmentId\" = {{{paramIndex++}}}");
            parameters.Add(dto.TargetDepartmentId.Value);
        }
        if (dto.TargetUserId.HasValue)
        {
            updates.Add($"\"TargetUserId\" = {{{paramIndex++}}}");
            parameters.Add(dto.TargetUserId.Value);
        }
        if (!string.IsNullOrEmpty(dto.ActionType))
        {
            updates.Add($"\"ActionType\" = {{{paramIndex++}}}");
            parameters.Add(dto.ActionType);
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

        parameters.Add(id);
        if (existing.CompanyId.HasValue)
        {
            parameters.Add(existing.CompanyId.Value);
            var sql = $"UPDATE \"ParserRules\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" = {{{paramIndex + 1}}}";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }
        else
        {
            var sql = $"UPDATE \"ParserRules\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" IS NULL";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        _logger.LogInformation("Email rule updated: {RuleId}, User: {UserId}", id, userId);

        return await GetByIdAsync(id, cancellationToken) ?? existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Email rule with ID {id} not found");
        }

        if (existing.CompanyId.HasValue)
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ParserRules"" WHERE ""Id"" = {0} AND ""CompanyId"" = {1}",
                id, existing.CompanyId.Value);
        else
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""ParserRules"" WHERE ""Id"" = {0} AND ""CompanyId"" IS NULL",
                id);

        _logger.LogInformation("Email rule deleted: {RuleId}", id);
    }

    private static EmailRuleDto MapToDto(CephasOps.Domain.Parser.Entities.ParserRule rule)
    {
        return new EmailRuleDto
        {
            Id = rule.Id,
            CompanyId = rule.CompanyId,
            EmailAccountId = rule.EmailAccountId,
            FromAddressPattern = rule.FromAddressPattern,
            DomainPattern = rule.DomainPattern,
            SubjectContains = rule.SubjectContains,
            IsVip = rule.IsVip,
            TargetDepartmentId = rule.TargetDepartmentId,
            TargetUserId = rule.TargetUserId,
            ActionType = rule.ActionType,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            Description = rule.Description,
            CreatedByUserId = rule.CreatedByUserId,
            UpdatedByUserId = rule.UpdatedByUserId,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}

