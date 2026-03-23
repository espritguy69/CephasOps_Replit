using System.Text.Json;
using CephasOps.Application.Audit.DTOs;
using CephasOps.Domain.Audit.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Audit.Services;

/// <summary>
/// Writes and reads audit log entries. Failures on write are logged but do not throw.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        ApplicationDbContext context,
        ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LogAuditAsync(
        Guid? companyId,
        Guid? userId,
        string entityType,
        Guid entityId,
        string action,
        string? fieldChangesJson = null,
        string channel = "Api",
        string? ipAddress = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new AuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                CompanyId = companyId,
                UserId = userId,
                EntityType = entityType ?? string.Empty,
                EntityId = entityId,
                Action = action ?? string.Empty,
                FieldChangesJson = fieldChangesJson,
                Channel = channel ?? "Api",
                IpAddress = ipAddress,
                MetadataJson = metadataJson
            };
            _context.AuditLogs.Add(entry);
            if (companyId.HasValue)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await TenantScopeExecutor.RunWithPlatformBypassAsync(async ct =>
                    await _context.SaveChangesAsync(ct), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to write audit log: EntityType={EntityType}, EntityId={EntityId}, Action={Action}",
                entityType, entityId, action);
        }
    }

    /// <inheritdoc />
    public async Task<List<AuditLogDto>> GetAuditLogsAsync(
        Guid? companyId = null,
        string? entityType = null,
        Guid? entityId = null,
        Guid? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? action = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (companyId.HasValue)
            query = query.Where(a => a.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId);
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);
        if (dateFrom.HasValue)
            query = query.Where(a => a.Timestamp >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(a => a.Timestamp <= dateTo.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        var list = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(Math.Min(limit, 500))
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                CompanyId = a.CompanyId,
                UserId = a.UserId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                FieldChangesJson = a.FieldChangesJson,
                Channel = a.Channel,
                IpAddress = a.IpAddress,
                MetadataJson = a.MetadataJson
            })
            .ToListAsync(cancellationToken);

        return list;
    }

    /// <inheritdoc />
    public async Task<(List<SecurityActivityEntryDto> Items, int TotalCount)> GetSecurityActivityAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Auth");

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (dateFrom.HasValue)
            query = query.Where(a => a.Timestamp >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(a => a.Timestamp <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var joinQuery = from a in query
                        join u in _context.Users on a.UserId equals u.Id into uGroup
                        from u in uGroup.DefaultIfEmpty()
                        orderby a.Timestamp descending
                        select new { a, UserEmail = u != null ? u.Email : null };

        var rows = await joinQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x =>
        {
            var userAgent = (string?)null;
            if (!string.IsNullOrEmpty(x.a.MetadataJson))
            {
                try
                {
                    var doc = JsonDocument.Parse(x.a.MetadataJson);
                    if (doc.RootElement.TryGetProperty("userAgent", out var uaProp))
                        userAgent = uaProp.GetString();
                }
                catch { /* ignore */ }
            }
            return new SecurityActivityEntryDto
            {
                Id = x.a.Id,
                Timestamp = x.a.Timestamp,
                UserId = x.a.UserId,
                UserEmail = x.UserEmail,
                Action = x.a.Action,
                IpAddress = x.a.IpAddress,
                UserAgent = userAgent,
                MetadataJson = x.a.MetadataJson
            };
        }).ToList();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<List<SecurityActivityEntryDto>> GetAuthEventsForDetectionAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        Guid? userId = null,
        int maxCount = 5000,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Auth");

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId);
        if (dateFrom.HasValue)
            query = query.Where(a => a.Timestamp >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(a => a.Timestamp <= dateTo.Value);

        var take = Math.Clamp(maxCount, 1, 10000);

        var joinQuery = from a in query
                        join u in _context.Users on a.UserId equals u.Id into uGroup
                        from u in uGroup.DefaultIfEmpty()
                        orderby a.Timestamp descending
                        select new { a, UserEmail = u != null ? u.Email : null };

        var rows = await joinQuery.Take(take).ToListAsync(cancellationToken);

        return rows.Select(x =>
        {
            var userAgent = (string?)null;
            if (!string.IsNullOrEmpty(x.a.MetadataJson))
            {
                try
                {
                    var doc = JsonDocument.Parse(x.a.MetadataJson);
                    if (doc.RootElement.TryGetProperty("userAgent", out var uaProp))
                        userAgent = uaProp.GetString();
                }
                catch { /* ignore */ }
            }
            return new SecurityActivityEntryDto
            {
                Id = x.a.Id,
                Timestamp = x.a.Timestamp,
                UserId = x.a.UserId,
                UserEmail = x.UserEmail,
                Action = x.a.Action,
                IpAddress = x.a.IpAddress,
                UserAgent = userAgent,
                MetadataJson = x.a.MetadataJson
            };
        }).ToList();
    }
}
