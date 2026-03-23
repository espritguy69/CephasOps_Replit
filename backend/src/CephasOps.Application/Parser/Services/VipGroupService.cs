using CephasOps.Application.Parser.DTOs;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// VIP group service implementation
/// </summary>
public class VipGroupService : IVipGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VipGroupService> _logger;

    public VipGroupService(ApplicationDbContext context, ILogger<VipGroupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<VipGroupDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _context.VipGroups
            .OrderByDescending(g => g.Priority)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        var dtos = new List<VipGroupDto>();
        foreach (var group in groups)
        {
            dtos.Add(await MapToDtoWithNamesAsync(group, cancellationToken));
        }
        return dtos;
    }

    public async Task<VipGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _context.VipGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        return group != null ? await MapToDtoWithNamesAsync(group, cancellationToken) : null;
    }

    public async Task<VipGroupDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var group = await _context.VipGroups
            .FirstOrDefaultAsync(g => g.Code == code, cancellationToken);

        return group != null ? await MapToDtoWithNamesAsync(group, cancellationToken) : null;
    }

    public async Task<List<VipGroupDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _context.VipGroups
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.Priority)
            .ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        var dtos = new List<VipGroupDto>();
        foreach (var group in groups)
        {
            dtos.Add(await MapToDtoWithNamesAsync(group, cancellationToken));
        }
        return dtos;
    }

    public async Task<VipGroupDto> CreateAsync(CreateVipGroupDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Group name is required");
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new InvalidOperationException("Group code is required");

        // Check for duplicate code
        var existing = await GetByCodeAsync(dto.Code, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"VIP group with code '{dto.Code}' already exists");

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] groupParams = {
            id,
            DBNull.Value, // CompanyId - null for now
            dto.Name,
            dto.Code.ToUpperInvariant(),
            (object?)dto.Description ?? DBNull.Value,
            (object?)dto.NotifyDepartmentId ?? DBNull.Value,
            (object?)dto.NotifyUserId ?? DBNull.Value,
            (object?)dto.NotifyHodUserId ?? DBNull.Value,
            (object?)dto.NotifyRole ?? DBNull.Value,
            dto.Priority,
            dto.IsActive,
            userId,
            DBNull.Value,
            now,
            now
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""VipGroups"" (
                ""Id"", ""CompanyId"", ""Name"", ""Code"", ""Description"",
                ""NotifyDepartmentId"", ""NotifyUserId"", ""NotifyHodUserId"", ""NotifyRole"",
                ""Priority"", ""IsActive"", ""CreatedByUserId"", ""UpdatedByUserId"", ""CreatedAt"", ""UpdatedAt""
            ) VALUES (
                {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}
            )",
            groupParams);

        _logger.LogInformation("VIP group created: {GroupId}, Code: {Code}, User: {UserId}", id, dto.Code, userId);

        // Create VipEmail records for provided email addresses
        if (dto.EmailAddresses != null && dto.EmailAddresses.Count > 0)
        {
            await SyncGroupEmailsAsync(id, dto.EmailAddresses, userId, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created VIP group");
    }

    public async Task<VipGroupDto> UpdateAsync(Guid id, UpdateVipGroupDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"VIP group with ID {id} not found");

        // Check for duplicate code if changing
        if (!string.IsNullOrEmpty(dto.Code) && !dto.Code.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await GetByCodeAsync(dto.Code, cancellationToken);
            if (duplicate != null)
                throw new InvalidOperationException($"VIP group with code '{dto.Code}' already exists");
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
        if (dto.Description != null)
        {
            updates.Add($"\"Description\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.Description) ? DBNull.Value : dto.Description);
        }
        if (dto.NotifyDepartmentId.HasValue)
        {
            updates.Add($"\"NotifyDepartmentId\" = {{{paramIndex++}}}");
            parameters.Add(dto.NotifyDepartmentId.Value == Guid.Empty ? DBNull.Value : dto.NotifyDepartmentId.Value);
        }
        if (dto.NotifyUserId.HasValue)
        {
            updates.Add($"\"NotifyUserId\" = {{{paramIndex++}}}");
            parameters.Add(dto.NotifyUserId.Value == Guid.Empty ? DBNull.Value : dto.NotifyUserId.Value);
        }
        if (dto.NotifyHodUserId.HasValue)
        {
            updates.Add($"\"NotifyHodUserId\" = {{{paramIndex++}}}");
            parameters.Add(dto.NotifyHodUserId.Value == Guid.Empty ? DBNull.Value : dto.NotifyHodUserId.Value);
        }
        if (dto.NotifyRole != null)
        {
            updates.Add($"\"NotifyRole\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.NotifyRole) ? DBNull.Value : dto.NotifyRole);
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

        parameters.Add(id);
        if (existing.CompanyId.HasValue)
        {
            parameters.Add(existing.CompanyId.Value);
            var sql = $"UPDATE \"VipGroups\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" = {{{paramIndex + 1}}}";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }
        else
        {
            var sql = $"UPDATE \"VipGroups\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" IS NULL";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        _logger.LogInformation("VIP group updated: {GroupId}, User: {UserId}", id, userId);

        // Sync email addresses if provided
        if (dto.EmailAddresses != null)
        {
            await SyncGroupEmailsAsync(id, dto.EmailAddresses, userId, cancellationToken);
        }

        return await GetByIdAsync(id, cancellationToken) ?? existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new KeyNotFoundException($"VIP group with ID {id} not found");

        // Check if any VIP emails are using this group
        var emailCount = await _context.VipEmails.CountAsync(e => e.VipGroupId == id, cancellationToken);
        if (emailCount > 0)
            throw new InvalidOperationException($"Cannot delete VIP group '{existing.Name}' because {emailCount} VIP email(s) are assigned to it");

        if (existing.CompanyId.HasValue)
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""VipGroups"" WHERE ""Id"" = {0} AND ""CompanyId"" = {1}",
                id, existing.CompanyId.Value);
        }
        else
        {
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""VipGroups"" WHERE ""Id"" = {0} AND ""CompanyId"" IS NULL",
                id);
        }

        _logger.LogInformation("VIP group deleted: {GroupId}, Code: {Code}", id, existing.Code);
    }

    private async Task<VipGroupDto> MapToDtoWithNamesAsync(VipGroup group, CancellationToken cancellationToken)
    {
        var dto = new VipGroupDto
        {
            Id = group.Id,
            CompanyId = group.CompanyId,
            Name = group.Name,
            Code = group.Code,
            Description = group.Description,
            NotifyDepartmentId = group.NotifyDepartmentId,
            NotifyUserId = group.NotifyUserId,
            NotifyHodUserId = group.NotifyHodUserId,
            NotifyRole = group.NotifyRole,
            Priority = group.Priority,
            IsActive = group.IsActive,
            CreatedByUserId = group.CreatedByUserId,
            UpdatedByUserId = group.UpdatedByUserId,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };

        // Get department name
        if (group.NotifyDepartmentId.HasValue)
        {
            var dept = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == group.NotifyDepartmentId.Value, cancellationToken);
            dto.DepartmentName = dept?.Name;
        }

        // Get user names
        if (group.NotifyUserId.HasValue)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == group.NotifyUserId.Value, cancellationToken);
            dto.NotifyUserName = user != null
                ? (!string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.Email)
                : null;
        }

        if (group.NotifyHodUserId.HasValue)
        {
            var hod = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == group.NotifyHodUserId.Value, cancellationToken);
            dto.HodUserName = hod != null
                ? (!string.IsNullOrWhiteSpace(hod.Name) ? hod.Name : hod.Email)
                : null;
        }

        // Load email addresses in this group
        var emails = await _context.VipEmails
            .Where(e => e.VipGroupId == group.Id)
            .OrderBy(e => e.EmailAddress)
            .ToListAsync(cancellationToken);

        dto.EmailAddresses = emails.Select(e => new VipGroupEmailDto
        {
            Id = e.Id,
            EmailAddress = e.EmailAddress,
            DisplayName = e.DisplayName,
            IsActive = e.IsActive
        }).ToList();

        return dto;
    }

    /// <summary>
    /// Sync email addresses with a VIP group
    /// Creates new VipEmail records, updates existing ones, and removes emails not in the list
    /// </summary>
    private async Task SyncGroupEmailsAsync(
        Guid groupId,
        List<string> emailAddresses,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // Normalize email addresses
        var normalizedEmails = emailAddresses
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        // Get existing emails in this group
        var existingEmails = await _context.VipEmails
            .Where(e => e.VipGroupId == groupId)
            .ToListAsync(cancellationToken);

        var existingEmailSet = existingEmails
            .Select(e => e.EmailAddress.ToLowerInvariant())
            .ToHashSet();

        // Create new emails
        var emailsToCreate = normalizedEmails
            .Where(e => !existingEmailSet.Contains(e))
            .ToList();

        var now = DateTime.UtcNow;
        foreach (var email in emailsToCreate)
        {
            var emailId = Guid.NewGuid();
            await _context.Database.ExecuteSqlRawAsync(
                @"INSERT INTO ""VipEmails"" (
                    ""Id"", ""CompanyId"", ""EmailAddress"", ""VipGroupId"", ""IsActive"",
                    ""CreatedByUserId"", ""UpdatedByUserId"", ""CreatedAt"", ""UpdatedAt""
                ) VALUES (
                    {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}
                )",
                emailId,
                DBNull.Value, // CompanyId
                email,
                groupId,
                true, // IsActive
                userId,
                DBNull.Value,
                now,
                now
            );
            _logger.LogInformation("Created VIP email: {Email} for group: {GroupId}", email, groupId);
        }

        // Remove emails that are no longer in the list
        var emailsToRemove = existingEmails
            .Where(e => !normalizedEmails.Contains(e.EmailAddress.ToLowerInvariant()))
            .ToList();

        foreach (var email in emailsToRemove)
        {
            if (email.CompanyId.HasValue)
                await _context.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM ""VipEmails"" WHERE ""Id"" = {0} AND ""CompanyId"" = {1}",
                    email.Id, email.CompanyId.Value);
            else
                await _context.Database.ExecuteSqlRawAsync(
                    @"DELETE FROM ""VipEmails"" WHERE ""Id"" = {0} AND ""CompanyId"" IS NULL",
                    email.Id);
            _logger.LogInformation("Removed VIP email: {Email} from group: {GroupId}", email.EmailAddress, groupId);
        }

        // Update existing emails to ensure they're linked to the group (in case they weren't)
        var emailsToUpdate = existingEmails
            .Where(e => normalizedEmails.Contains(e.EmailAddress.ToLowerInvariant()) && e.VipGroupId != groupId)
            .ToList();

        foreach (var email in emailsToUpdate)
        {
            if (email.CompanyId.HasValue)
                await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""VipEmails"" SET ""VipGroupId"" = {0}, ""UpdatedAt"" = {1}, ""UpdatedByUserId"" = {2} WHERE ""Id"" = {3} AND ""CompanyId"" = {4}",
                    groupId, now, userId, email.Id, email.CompanyId.Value);
            else
                await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""VipEmails"" SET ""VipGroupId"" = {0}, ""UpdatedAt"" = {1}, ""UpdatedByUserId"" = {2} WHERE ""Id"" = {3} AND ""CompanyId"" IS NULL",
                    groupId, now, userId, email.Id);
            _logger.LogInformation("Updated VIP email: {Email} to link to group: {GroupId}", email.EmailAddress, groupId);
        }
    }
}

