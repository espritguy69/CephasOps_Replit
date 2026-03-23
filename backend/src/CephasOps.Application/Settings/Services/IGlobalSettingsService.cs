using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Settings;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Global settings service interface
/// </summary>
public interface IGlobalSettingsService : IGlobalSettingsReader
{
    Task<List<GlobalSettingDto>> GetAllAsync(string? module = null, CancellationToken cancellationToken = default);
    Task<GlobalSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    new Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<GlobalSettingDto> CreateAsync(CreateGlobalSettingDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task<GlobalSettingDto> UpdateAsync(string key, UpdateGlobalSettingDto dto, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

