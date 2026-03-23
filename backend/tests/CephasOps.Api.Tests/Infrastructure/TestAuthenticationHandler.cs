using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CephasOps.Api.Tests.Infrastructure;

/// <summary>
/// Test authentication handler that builds a principal from request headers.
/// Used by integration tests to simulate an authenticated user (e.g. user in Dept A only).
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme = "Test";

    private const string TestUserIdHeader = "X-Test-User-Id";
    private const string TestCompanyIdHeader = "X-Test-Company-Id";
    private const string TestRolesHeader = "X-Test-Roles";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[TestUserIdHeader].FirstOrDefault();
        var companyId = Request.Headers[TestCompanyIdHeader].FirstOrDefault();
        var roles = Request.Headers[TestRolesHeader].FirstOrDefault() ?? "Member";

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out _))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        if (!string.IsNullOrEmpty(companyId) && Guid.TryParse(companyId, out var parsedCompanyId) && parsedCompanyId != Guid.Empty)
        {
            claims.Add(new Claim("companyId", companyId));
            claims.Add(new Claim("company_id", companyId));
        }

        foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
