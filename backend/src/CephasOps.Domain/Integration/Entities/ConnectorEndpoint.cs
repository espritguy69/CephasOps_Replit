namespace CephasOps.Domain.Integration.Entities;

/// <summary>
/// Per-company (or global) endpoint configuration for a connector. Holds URL, auth, retry policy, event filters.
/// </summary>
public class ConnectorEndpoint
{
    public Guid Id { get; set; }

    public Guid ConnectorDefinitionId { get; set; }
    public ConnectorDefinition? ConnectorDefinition { get; set; }

    /// <summary>Null = global; set = company-scoped.</summary>
    public Guid? CompanyId { get; set; }

    /// <summary>Endpoint URL for outbound; path hint for inbound.</summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>HTTP method for outbound: POST, PUT, etc.</summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>Optional: comma-separated event types to send (empty = all allowed by definition).</summary>
    public string? AllowedEventTypes { get; set; }

    /// <summary>Optional: JSON config for signing (e.g. secret key ref, algorithm).</summary>
    public string? SigningConfigJson { get; set; }

    /// <summary>Optional: JSON config for auth (e.g. bearer token ref, header name).</summary>
    public string? AuthConfigJson { get; set; }

    /// <summary>Retry count (0 = no retry).</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsPaused { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
