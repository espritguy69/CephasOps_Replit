namespace CephasOps.Domain.Settings;

/// <summary>
/// Minimal interface for reading global settings values
/// Used by Infrastructure providers to avoid circular dependency
/// </summary>
public interface IGlobalSettingsReader
{
    /// <summary>
    /// Get a setting value by key
    /// </summary>
    Task<T?> GetValueAsync<T>(string key, CancellationToken cancellationToken = default);
}

