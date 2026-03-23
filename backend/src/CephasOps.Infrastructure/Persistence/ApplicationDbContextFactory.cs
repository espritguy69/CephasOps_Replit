using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext.
/// Used by EF Core tools (migrations, bundle) to create the DbContext.
/// Connection string is resolved in order: (1) --connection from bundle args, (2) env ConnectionStrings__DefaultConnection, (3) appsettings.json.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Design-time/migrations: allow SaveChanges without tenant context (TenantSafetyGuard)
        TenantSafetyGuard.EnterPlatformBypass();

        var connectionString = GetConnectionString(args);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. Set ConnectionStrings__DefaultConnection, " +
                "pass --connection \"...\" to the migrations bundle, or ensure appsettings.json is available.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.ConfigureWarnings(w =>
        {
            w.Ignore(RelationalEventId.PendingModelChangesWarning);
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
        });
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string? GetConnectionString(string[] args)
    {
        // 1) Migration bundle: --connection "..." is passed in args
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--connection" && !string.IsNullOrWhiteSpace(args[i + 1]))
                return args[i + 1].Trim();
        }

        // 2) Environment variable (works for bundle and tools)
        var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(envConnection))
            return envConnection.Trim();

        // 3) appsettings.json: use a base path that works for backend\src\CephasOps.Api and for bundle run from backend\scripts
        var basePath = ResolveAppSettingsBasePath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Resolves the directory containing appsettings.json. Supports (1) running from Api project dir,
    /// (2) running from Infrastructure or backend/scripts (backend\src\CephasOps.Api), (3) legacy backend\CephasOps.Api.
    /// </summary>
    private static string ResolveAppSettingsBasePath()
    {
        var cwd = Directory.GetCurrentDirectory();

        var candidates = new[]
        {
            cwd,
            Path.Combine(cwd, "..", "src", "CephasOps.Api"),
            Path.Combine(cwd, "..", "CephasOps.Api"),
        };

        foreach (var dir in candidates)
        {
            var full = Path.GetFullPath(dir);
            if (File.Exists(Path.Combine(full, "appsettings.json")))
                return full;
        }

        return Path.GetFullPath(Path.Combine(cwd, "..", "src", "CephasOps.Api"));
    }
}

