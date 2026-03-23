using CephasOps.Application.Parser.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// VIP email service implementation
/// </summary>
public class VipEmailService : IVipEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VipEmailService> _logger;

    public VipEmailService(ApplicationDbContext context, ILogger<VipEmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<VipEmailDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var vipEmails = await _context.VipEmails
            .OrderBy(v => v.EmailAddress)
            .ToListAsync(cancellationToken);

        return vipEmails.Select(MapToDto).ToList();
    }

    public async Task<VipEmailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var vipEmail = await _context.VipEmails
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return vipEmail != null ? MapToDto(vipEmail) : null;
    }

    public async Task<VipEmailDto?> GetByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return null;

        var normalizedEmail = emailAddress.Trim().ToLowerInvariant();

        var vipEmail = await _context.VipEmails
            .FirstOrDefaultAsync(v => v.EmailAddress.ToLower() == normalizedEmail, cancellationToken);

        return vipEmail != null ? MapToDto(vipEmail) : null;
    }

    public async Task<List<VipEmailDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var vipEmails = await _context.VipEmails
            .Where(v => v.IsActive)
            .OrderBy(v => v.EmailAddress)
            .ToListAsync(cancellationToken);

        return vipEmails.Select(MapToDto).ToList();
    }

    public async Task<VipEmailDto> CreateAsync(CreateVipEmailDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        // Check for duplicate email address
        var existing = await GetByEmailAddressAsync(dto.EmailAddress, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"VIP email entry for '{dto.EmailAddress}' already exists");
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        object[] vipParams = {
            id,
            DBNull.Value, // CompanyId - null for now
            dto.EmailAddress.Trim().ToLowerInvariant(),
            (object?)dto.DisplayName ?? DBNull.Value,
            (object?)dto.Description ?? DBNull.Value,
            (object?)dto.VipGroupId ?? DBNull.Value,
            (object?)dto.NotifyUserId ?? DBNull.Value,
            (object?)dto.NotifyRole ?? DBNull.Value,
            (object?)dto.DepartmentId ?? DBNull.Value,
            dto.IsActive,
            userId,
            DBNull.Value,
            now,
            now
        };
        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""VipEmails"" (
                ""Id"", ""CompanyId"", ""EmailAddress"", ""DisplayName"", ""Description"",
                ""VipGroupId"", ""NotifyUserId"", ""NotifyRole"", ""DepartmentId"", ""IsActive"",
                ""CreatedByUserId"", ""UpdatedByUserId"", ""CreatedAt"", ""UpdatedAt""
            ) VALUES (
                {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}
            )",
            vipParams);

        _logger.LogInformation("VIP email created: {VipEmailId}, Email: {Email}, User: {UserId}", id, dto.EmailAddress, userId);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created VIP email");
    }

    public async Task<VipEmailDto> UpdateAsync(Guid id, UpdateVipEmailDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"VIP email with ID {id} not found");
        }

        // Check for duplicate if email address is being changed
        if (!string.IsNullOrEmpty(dto.EmailAddress) && 
            !dto.EmailAddress.Equals(existing.EmailAddress, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await GetByEmailAddressAsync(dto.EmailAddress, cancellationToken);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"VIP email entry for '{dto.EmailAddress}' already exists");
            }
        }

        var now = DateTime.UtcNow;
        var updates = new List<string> { "\"UpdatedAt\" = {0}", "\"UpdatedByUserId\" = {1}" };
        var parameters = new List<object> { now, userId };
        var paramIndex = 2;

        if (!string.IsNullOrEmpty(dto.EmailAddress))
        {
            updates.Add($"\"EmailAddress\" = {{{paramIndex++}}}");
            parameters.Add(dto.EmailAddress.Trim().ToLowerInvariant());
        }
        if (dto.DisplayName != null)
        {
            updates.Add($"\"DisplayName\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.DisplayName) ? DBNull.Value : dto.DisplayName);
        }
        if (dto.Description != null)
        {
            updates.Add($"\"Description\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.Description) ? DBNull.Value : dto.Description);
        }
        if (dto.VipGroupId.HasValue)
        {
            updates.Add($"\"VipGroupId\" = {{{paramIndex++}}}");
            parameters.Add(dto.VipGroupId.Value == Guid.Empty ? DBNull.Value : dto.VipGroupId.Value);
        }
        if (dto.NotifyUserId.HasValue)
        {
            updates.Add($"\"NotifyUserId\" = {{{paramIndex++}}}");
            parameters.Add(dto.NotifyUserId.Value == Guid.Empty ? DBNull.Value : dto.NotifyUserId.Value);
        }
        if (dto.NotifyRole != null)
        {
            updates.Add($"\"NotifyRole\" = {{{paramIndex++}}}");
            parameters.Add(string.IsNullOrEmpty(dto.NotifyRole) ? DBNull.Value : dto.NotifyRole);
        }
        if (dto.DepartmentId.HasValue)
        {
            updates.Add($"\"DepartmentId\" = {{{paramIndex++}}}");
            parameters.Add(dto.DepartmentId.Value == Guid.Empty ? DBNull.Value : dto.DepartmentId.Value);
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
            var sql = $"UPDATE \"VipEmails\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" = {{{paramIndex + 1}}}";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }
        else
        {
            var sql = $"UPDATE \"VipEmails\" SET {string.Join(", ", updates)} WHERE \"Id\" = {{{paramIndex}}} AND \"CompanyId\" IS NULL";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
        }

        _logger.LogInformation("VIP email updated: {VipEmailId}, User: {UserId}", id, userId);

        return await GetByIdAsync(id, cancellationToken) ?? existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"VIP email with ID {id} not found");
        }

        if (existing.CompanyId.HasValue)
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""VipEmails"" WHERE ""Id"" = {0} AND ""CompanyId"" = {1}",
                id, existing.CompanyId.Value);
        else
            await _context.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""VipEmails"" WHERE ""Id"" = {0} AND ""CompanyId"" IS NULL",
                id);

        _logger.LogInformation("VIP email deleted: {VipEmailId}, Email: {Email}", id, existing.EmailAddress);
    }

    public async Task<List<VipEmailDto>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var vipEmails = await _context.VipEmails
            .Where(v => v.VipGroupId == groupId)
            .OrderBy(v => v.EmailAddress)
            .ToListAsync(cancellationToken);

        var dtos = new List<VipEmailDto>();
        foreach (var email in vipEmails)
        {
            dtos.Add(await MapToDtoWithNamesAsync(email, cancellationToken));
        }
        return dtos;
    }

    private async Task<VipEmailDto> MapToDtoWithNamesAsync(CephasOps.Domain.Parser.Entities.VipEmail vipEmail, CancellationToken cancellationToken)
    {
        var dto = new VipEmailDto
        {
            Id = vipEmail.Id,
            CompanyId = vipEmail.CompanyId,
            EmailAddress = vipEmail.EmailAddress,
            DisplayName = vipEmail.DisplayName,
            Description = vipEmail.Description,
            VipGroupId = vipEmail.VipGroupId,
            NotifyUserId = vipEmail.NotifyUserId,
            NotifyRole = vipEmail.NotifyRole,
            DepartmentId = vipEmail.DepartmentId,
            IsActive = vipEmail.IsActive,
            CreatedByUserId = vipEmail.CreatedByUserId,
            UpdatedByUserId = vipEmail.UpdatedByUserId,
            CreatedAt = vipEmail.CreatedAt,
            UpdatedAt = vipEmail.UpdatedAt
        };

        // Get VIP group name
        if (vipEmail.VipGroupId.HasValue)
        {
            var group = await _context.VipGroups
                .FirstOrDefaultAsync(g => g.Id == vipEmail.VipGroupId.Value, cancellationToken);
            dto.VipGroupName = group?.Name;
        }

        // Get user name
        if (vipEmail.NotifyUserId.HasValue)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == vipEmail.NotifyUserId.Value, cancellationToken);
            dto.NotifyUserName = user != null
                ? (!string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.Email)
                : null;
        }

        // Get department name
        if (vipEmail.DepartmentId.HasValue)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == vipEmail.DepartmentId.Value, cancellationToken);
            dto.DepartmentName = department?.Name;
        }

        return dto;
    }

    private static VipEmailDto MapToDto(CephasOps.Domain.Parser.Entities.VipEmail vipEmail)
    {
        return new VipEmailDto
        {
            Id = vipEmail.Id,
            CompanyId = vipEmail.CompanyId,
            EmailAddress = vipEmail.EmailAddress,
            DisplayName = vipEmail.DisplayName,
            Description = vipEmail.Description,
            VipGroupId = vipEmail.VipGroupId,
            NotifyUserId = vipEmail.NotifyUserId,
            NotifyRole = vipEmail.NotifyRole,
            DepartmentId = vipEmail.DepartmentId,
            IsActive = vipEmail.IsActive,
            CreatedByUserId = vipEmail.CreatedByUserId,
            UpdatedByUserId = vipEmail.UpdatedByUserId,
            CreatedAt = vipEmail.CreatedAt,
            UpdatedAt = vipEmail.UpdatedAt
        };
    }
}

