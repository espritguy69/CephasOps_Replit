using CephasOps.Api.Tests.Infrastructure;
using Xunit;

namespace CephasOps.Api.Tests.Integration;

/// <summary>
/// Collection for inventory integration tests that share the in-memory DB.
/// Runs sequentially to avoid one test clearing data another test needs (e.g. DepartmentMemberships).
/// </summary>
[CollectionDefinition("InventoryIntegration")]
public class InventoryIntegrationCollection : ICollectionFixture<CephasOpsWebApplicationFactory>
{
}
