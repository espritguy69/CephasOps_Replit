using System.Net.Http.Headers;

namespace CephasOps.Api.Tests.Infrastructure;

/// <summary>
/// Builds an HttpClient with test auth headers for a specific tenant (Tenant A or B)
/// for use in tenant boundary tests.
/// </summary>
public static class BoundaryTestClientBuilder
{
    public const string HeaderUserId = "X-Test-User-Id";
    public const string HeaderCompanyId = "X-Test-Company-Id";
    public const string HeaderRoles = "X-Test-Roles";

    /// <summary>
    /// Creates an HttpClient and sets headers for the given tenant user.
    /// </summary>
    /// <param name="client">The client from the factory (e.g. _factory.CreateClient()).</param>
    /// <param name="userId">User GUID for this tenant.</param>
    /// <param name="companyId">Company (tenant) GUID.</param>
    /// <param name="roles">Optional comma-separated roles; default "Admin".</param>
    /// <returns>The same client with headers set.</returns>
    public static HttpClient ForTenant(this HttpClient client, Guid userId, Guid companyId, string roles = "Admin")
    {
        client.DefaultRequestHeaders.Remove(HeaderUserId);
        client.DefaultRequestHeaders.Remove(HeaderCompanyId);
        client.DefaultRequestHeaders.Remove(HeaderRoles);
        client.DefaultRequestHeaders.Add(HeaderUserId, userId.ToString());
        client.DefaultRequestHeaders.Add(HeaderCompanyId, companyId.ToString());
        client.DefaultRequestHeaders.Add(HeaderRoles, roles);
        return client;
    }
}
