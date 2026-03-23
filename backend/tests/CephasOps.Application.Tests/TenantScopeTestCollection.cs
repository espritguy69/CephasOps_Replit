using Xunit;

// Disable parallelization for the entire assembly so TenantScope (AsyncLocal) is not overwritten across test classes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CephasOps.Application.Tests;

/// <summary>
/// Runs tenant-scope–dependent tests without parallelization so TenantScope/AsyncLocal is not overwritten by other tests.
/// </summary>
[CollectionDefinition("TenantScopeTests", DisableParallelization = true)]
public class TenantScopeTestCollectionDefinition
{
}
