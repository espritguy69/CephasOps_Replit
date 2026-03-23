namespace CephasOps.Api.Common.Attributes;

/// <summary>
/// When applied to a controller or action, the tenant guard middleware will not require
/// a valid company/tenant context for this endpoint. Use for auth, health, platform admin,
/// or other tenant-agnostic operations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class AllowNoTenantAttribute : Attribute
{
}
