using Xunit;

namespace CephasOps.Application.Tests.Auth;

/// <summary>
/// Runs AuthService tests without parallelization so TenantScope static state is not overwritten by other test classes.
/// </summary>
[CollectionDefinition("AuthServiceTests", DisableParallelization = true)]
public class AuthServiceTestCollectionDefinition
{
}
