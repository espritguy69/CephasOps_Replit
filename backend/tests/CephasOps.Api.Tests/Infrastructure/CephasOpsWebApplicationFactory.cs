using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CephasOps.Infrastructure.Persistence;

namespace CephasOps.Api.Tests.Infrastructure;

/// <summary>
/// Web application factory for API integration tests.
/// Environment "Testing" triggers: in-memory DB in Program; test auth via headers (X-Test-User-Id, etc.);
/// ProductionRoles are forced off here so no schedulers/workers/dispatchers start (quiet test host).
/// </summary>
public class CephasOpsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProductionRoles:RunGuardian"] = "false",
                ["ProductionRoles:RunNotificationWorkers"] = "false",
                ["ProductionRoles:RunJobWorkers"] = "false",
                ["ProductionRoles:RunWatchdog"] = "false",
                ["ProductionRoles:RunStorageLifecycle"] = "false",
                ["ProductionRoles:RunEventDispatcher"] = "false",
                ["ProductionRoles:RunIntegrationWorkers"] = "false",
                ["ProductionRoles:RunSchedulers"] = "false",
                ["ProductionRoles:RunEmailCleanup"] = "false",
                ["ProductionRoles:RunMetricsAggregation"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Use test auth so we can set user/company/roles via headers (X-Test-User-Id, etc.)
            services.AddAuthentication(TestAuthenticationHandler.Scheme)
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.Scheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
                o.DefaultSignInScheme = TestAuthenticationHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
            });
        });
    }
}
