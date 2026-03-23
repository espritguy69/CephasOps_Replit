using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CephasOps.Application.Platform.Guardian;

/// <summary>Platform Guardian: compares current configuration to expected baseline and classifies drift.</summary>
public class PlatformDriftDetectionService : IPlatformDriftDetectionService
{
    private readonly IConfiguration _configuration;
    private readonly PlatformDriftDetectionOptions _options;

    public PlatformDriftDetectionService(IConfiguration configuration, IOptions<PlatformDriftDetectionOptions>? options = null)
    {
        _configuration = configuration;
        _options = options?.Value ?? new PlatformDriftDetectionOptions();
    }

    public Task<PlatformDriftResultDto> DetectAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Task.FromResult(new PlatformDriftResultDto { GeneratedAtUtc = DateTime.UtcNow });

        var items = new List<PlatformDriftItemDto>();

        // JobOrchestration:Worker
        var batchSize = _configuration.GetValue("JobOrchestration:Worker:BatchSize", 10);
        if (batchSize > 50) items.Add(NewItem("JobOrchestration:Worker", "BatchSize", "<=50", batchSize.ToString(), "Warning", "High batch size may increase contention"));
        var tenantFairness = _configuration.GetValue("JobOrchestration:Worker:TenantJobFairnessEnabled", true);
        if (!tenantFairness) items.Add(NewItem("JobOrchestration:Worker", "TenantJobFairnessEnabled", "true", "false", "Warning", "Tenant job fairness disabled"));
        var leaseSeconds = _configuration.GetValue("JobOrchestration:Worker:LeaseSeconds", 300);
        if (leaseSeconds < 60) items.Add(NewItem("JobOrchestration:Worker", "LeaseSeconds", ">=60", leaseSeconds.ToString(), "Critical", "Very short lease may cause excessive stuck resets"));

        // SaaS:TenantRateLimit
        var rateLimitEnabled = _configuration.GetValue("SaaS:TenantRateLimit:Enabled", true);
        if (!rateLimitEnabled) items.Add(NewItem("SaaS:TenantRateLimit", "Enabled", "true", "false", "Warning", "Per-tenant rate limiting disabled"));
        var perMin = _configuration.GetValue("SaaS:TenantRateLimit:RequestsPerMinute", 100);
        if (perMin > 2000) items.Add(NewItem("SaaS:TenantRateLimit", "RequestsPerMinute", "<=2000", perMin.ToString(), "Informational", "Very high per-minute limit"));

        // SaaS:StorageLifecycle
        var lifecycleEnabled = _configuration.GetValue("SaaS:StorageLifecycle:Enabled", true);
        var archiveDays = _configuration.GetValue("SaaS:StorageLifecycle:ArchiveAfterDays", 365);
        if (lifecycleEnabled && archiveDays > 0 && archiveDays < 30) items.Add(NewItem("SaaS:StorageLifecycle", "ArchiveAfterDays", ">=30 or 0", archiveDays.ToString(), "Warning", "Short archive window"));

        // PlatformGuardian
        var guardianEnabled = _configuration.GetValue("PlatformGuardian:Enabled", true);
        var intervalMin = _configuration.GetValue("PlatformGuardian:RunIntervalMinutes", 60);
        if (guardianEnabled && intervalMin < 5) items.Add(NewItem("PlatformGuardian", "RunIntervalMinutes", ">=5", intervalMin.ToString(), "Warning", "Very frequent guardian runs may add load"));

        var result = new PlatformDriftResultDto
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Items = items,
            InformationalCount = items.Count(i => i.Classification == "Informational"),
            WarningCount = items.Count(i => i.Classification == "Warning"),
            CriticalCount = items.Count(i => i.Classification == "Critical")
        };

        return Task.FromResult(result);
    }

    private static PlatformDriftItemDto NewItem(string section, string key, string expected, string actual, string classification, string message) =>
        new() { Section = section, Key = key, Expected = expected, Actual = actual, Classification = classification, Message = message };
}
