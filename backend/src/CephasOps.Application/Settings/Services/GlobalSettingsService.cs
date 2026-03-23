using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Global settings service implementation
/// </summary>
public class GlobalSettingsService : IGlobalSettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GlobalSettingsService> _logger;

    public GlobalSettingsService(ApplicationDbContext context, ILogger<GlobalSettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<GlobalSettingDto>> GetAllAsync(string? module = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all global settings, module: {Module}", module);

        var query = _context.GlobalSettings.AsQueryable();

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(s => s.Module == module);
        }

        var settings = await query.OrderBy(s => s.Module).ThenBy(s => s.Key).ToListAsync(cancellationToken);

        return settings.Select(s => new GlobalSettingDto
        {
            Id = s.Id,
            Key = s.Key,
            Value = s.Value,
            ValueType = s.ValueType,
            Description = s.Description,
            Module = s.Module,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList();
    }

    public async Task<GlobalSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting global setting by key: {Key}", key);

        var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting == null) return null;

        return new GlobalSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            ValueType = setting.ValueType,
            Description = setting.Description,
            Module = setting.Module,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt
        };
    }

    public async Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting global setting value as {Type}: {Key}", typeof(T).Name, key);

        var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting == null) return default;

        try
        {
            return setting.ValueType switch
            {
                "Json" => JsonSerializer.Deserialize<T>(setting.Value),
                "Bool" => (T)(object)bool.Parse(setting.Value),
                "Int" => (T)(object)int.Parse(setting.Value),
                "Decimal" => (T)(object)decimal.Parse(setting.Value),
                _ => (T)(object)setting.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing global setting value: {Key}", key);
            return default;
        }
    }

    public async Task<GlobalSettingDto> CreateAsync(CreateGlobalSettingDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating global setting: {Key}", dto.Key);

        var existing = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == dto.Key, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Global setting with key '{dto.Key}' already exists");
        }

        var setting = new GlobalSetting
        {
            Id = Guid.NewGuid(),
            Key = dto.Key,
            Value = dto.Value,
            ValueType = dto.ValueType,
            Description = dto.Description,
            Module = dto.Module,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _context.GlobalSettings.Add(setting);
        await _context.SaveChangesAsync(cancellationToken);

        return new GlobalSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            ValueType = setting.ValueType,
            Description = setting.Description,
            Module = setting.Module,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt
        };
    }

    public async Task<GlobalSettingDto> UpdateAsync(string key, UpdateGlobalSettingDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating global setting: {Key}", key);

        var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting == null)
        {
            throw new KeyNotFoundException($"Global setting with key '{key}' not found");
        }

        setting.Value = dto.Value;
        if (!string.IsNullOrEmpty(dto.Description))
        {
            setting.Description = dto.Description;
        }
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedByUserId = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return new GlobalSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            ValueType = setting.ValueType,
            Description = setting.Description,
            Module = setting.Module,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt
        };
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting global setting: {Key}", key);

        var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting == null)
        {
            throw new KeyNotFoundException($"Global setting with key '{key}' not found");
        }

        _context.GlobalSettings.Remove(setting);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

