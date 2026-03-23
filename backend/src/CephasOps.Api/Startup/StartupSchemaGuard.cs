using System.Data;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CephasOps.Api.Startup;

/// <summary>
/// Fail-fast startup check that critical schema objects exist. Prevents the app from starting
/// when __EFMigrationsHistory says migrations are applied but required tables are missing
/// (e.g. after partial apply or drift). See backend/docs/operations/STARTUP_SCHEMA_GUARD.md.
/// </summary>
public static class StartupSchemaGuard
{
    /// <summary>
    /// Table names that must exist in public schema. These were involved in past drift incidents.
    /// </summary>
    public static readonly IReadOnlyList<string> RequiredTables = new[]
    {
        "ConnectorDefinitions",
        "ConnectorEndpoints",
        "ExternalIdempotencyRecords",
        "OutboundIntegrationAttempts"
    };

    /// <summary>
    /// Verifies that all required tables exist. Throws if any are missing.
    /// </summary>
    public static async Task EnsureCriticalTablesExistAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name IN (
                'ConnectorDefinitions', 'ConnectorEndpoints',
                'ExternalIdempotencyRecords', 'OutboundIntegrationAttempts')
            """;

        var existing = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                existing.Add(reader.GetString(0));
        }

        var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);
        var missing = RequiredTables.Where(t => !existingSet.Contains(t)).ToList();

        if (missing.Count == 0)
        {
            logger.LogInformation("Startup schema guard: all {Count} critical tables present.", RequiredTables.Count);
            return;
        }

        logger.LogError(
            "Startup schema guard failed: missing critical table(s): {Missing}. " +
            "Migration history may show migrations as applied but schema is incomplete. " +
            "Run backend/scripts/check-migration-state.sql and apply full migration or remediation. " +
            "See backend/docs/operations/EF_MIGRATION_SCHEMA_GUARD.md and STARTUP_SCHEMA_GUARD.md.",
            string.Join(", ", missing));

        throw new InvalidOperationException(
            $"Critical schema objects missing: {string.Join(", ", missing)}. " +
            "Application cannot start. Verify schema with check-migration-state.sql and apply full migration or remediation. " +
            "See backend/docs/operations/STARTUP_SCHEMA_GUARD.md.");
    }
}
