namespace CephasOps.Api.Production;

/// <summary>Validates critical production configuration at startup. Only runs when ASPNETCORE_ENVIRONMENT=Production.</summary>
public static class ProductionStartupValidator
{
    private const int MinimumSecretKeyLength = 16;

    /// <summary>Validates required production config. Throws if invalid and environment is Production.</summary>
    public static void Validate(IConfiguration configuration)
    {
        var env = configuration["ASPNETCORE_ENVIRONMENT"];
        if (string.IsNullOrEmpty(env) || !string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
            return;

        var errors = new List<string>();

        // Database
        var conn = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrWhiteSpace(conn))
            errors.Add("ConnectionStrings:DefaultConnection is required in Production.");

        // JWT
        var jwtSecret = configuration["Jwt:SecretKey"] ?? configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
            errors.Add("Jwt:SecretKey or Jwt:Key is required in Production.");
        else if (jwtSecret.Length < MinimumSecretKeyLength)
            errors.Add($"Jwt secret must be at least {MinimumSecretKeyLength} characters in Production.");

        // Redis: when set, must be non-empty (connectivity validated by health check / startup connectivity run)
        var redis = configuration["ConnectionStrings:Redis"];
        if (redis != null && string.IsNullOrWhiteSpace(redis))
            errors.Add("ConnectionStrings:Redis cannot be empty when set.");

        // Rate-limit: if section present, limits must be positive
        var rateLimitSection = configuration.GetSection("SaaS:TenantRateLimit");
        if (rateLimitSection.Exists())
        {
            var perMin = rateLimitSection.GetValue("RequestsPerMinute", -1);
            var perHour = rateLimitSection.GetValue("RequestsPerHour", -1);
            if (perMin >= 0 && perMin == 0) errors.Add("SaaS:TenantRateLimit:RequestsPerMinute must be > 0 when set.");
            if (perHour >= 0 && perHour == 0) errors.Add("SaaS:TenantRateLimit:RequestsPerHour must be > 0 when set.");
        }

        // Guardian: section expected in production (warning only - we don't fail, but document in ENVIRONMENT_VALIDATION.md)
        // Worker roles: section expected (ProductionRoles) - same, document only

        if (errors.Count > 0)
            throw new InvalidOperationException("Production startup validation failed: " + string.Join(" ", errors));
    }
}
