using CephasOps.Infrastructure;
using CephasOps.Infrastructure.Persistence;

namespace CephasOps.Application.Common;

/// <summary>
/// Defense-in-depth guard for financial operations. Validates company identity presence and consistency
/// across entities participating in a single calculation or write. Use before persisting financial data
/// or combining order + rate + invoice + earnings so mismatched-company states fail fast.
/// Does not replace tenant provider, tenant scope, EF query filters, or save-time tenant guard.
/// </summary>
public static class FinancialIsolationGuard
{
    /// <summary>
    /// Throws if neither a valid tenant context nor an approved platform bypass is active.
    /// Call at the start of finance-sensitive read/write/rebuild paths so operations fail fast when
    /// invoked without tenant scope or explicit platform bypass (e.g. repair job).
    /// </summary>
    /// <param name="operationName">Short name of the operation for the exception message.</param>
    /// <exception cref="InvalidOperationException">When TenantScope.CurrentTenantId is null/empty and platform bypass is not active.</exception>
    public static void RequireTenantOrBypass(string operationName)
    {
        if (TenantSafetyGuard.IsPlatformBypassActive)
            return;
        var tenantId = TenantScope.CurrentTenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            return;
        var message = "Financial operations require a valid tenant context (TenantScope.CurrentTenantId) or an approved platform bypass. Missing or empty tenant context.";
        PlatformGuardLogger.LogViolation("FinancialIsolationGuard", operationName, message);
        throw new InvalidOperationException(
            $"{operationName}: {message}");
    }

    /// <summary>
    /// Throws if <paramref name="companyId"/> is null or empty. Use at the start of financial write/calculation paths.
    /// </summary>
    /// <param name="companyId">Company id (e.g. from request context or entity).</param>
    /// <param name="operationName">Short name of the operation for the exception message.</param>
    /// <exception cref="InvalidOperationException">When companyId is null or Guid.Empty.</exception>
    public static void RequireCompany(Guid? companyId, string operationName)
    {
        if (companyId.HasValue && companyId.Value != Guid.Empty)
            return;
        var message = "Company context is required for this financial operation. CompanyId is missing or empty.";
        PlatformGuardLogger.LogViolation("FinancialIsolationGuard", operationName, message);
        throw new InvalidOperationException(
            $"{operationName}: {message}");
    }

    /// <summary>
    /// Throws if the two company ids differ (or either is null/empty). Use when combining two entities in one financial flow.
    /// </summary>
    /// <param name="companyIdA">First entity's company id.</param>
    /// <param name="companyIdB">Second entity's company id.</param>
    /// <param name="labelA">Short label for first entity (e.g. "Order", "Invoice").</param>
    /// <param name="labelB">Short label for second entity.</param>
    /// <param name="idA">Optional id of first entity for the message (avoid sensitive data).</param>
    /// <param name="idB">Optional id of second entity for the message.</param>
    /// <exception cref="InvalidOperationException">When company ids differ or either is null/empty.</exception>
    public static void RequireSameCompany(
        Guid? companyIdA,
        Guid? companyIdB,
        string labelA,
        string labelB,
        object? idA = null,
        object? idB = null)
    {
        if (!companyIdA.HasValue || companyIdA.Value == Guid.Empty)
        {
            var msg = $"{labelA} company id is missing or empty. {labelA}Id={idA?.ToString() ?? "n/a"}.";
            PlatformGuardLogger.LogViolation("FinancialIsolationGuard", "RequireSameCompany", msg, companyId: companyIdA, entityType: labelA, entityId: idA as Guid?);
            throw new InvalidOperationException(
                $"{labelA} company id is missing or empty. {labelA}Id={idA?.ToString() ?? "n/a"}. " +
                "Financial operations require a valid company context.");
        }
        if (!companyIdB.HasValue || companyIdB.Value == Guid.Empty)
        {
            var msg = $"{labelB} company id is missing or empty. {labelB}Id={idB?.ToString() ?? "n/a"}.";
            PlatformGuardLogger.LogViolation("FinancialIsolationGuard", "RequireSameCompany", msg, companyId: companyIdB, entityType: labelB, entityId: idB as Guid?);
            throw new InvalidOperationException(
                $"{msg} Financial operations require a valid company context.");
        }
        if (companyIdA.Value != companyIdB.Value)
        {
            var idAStr = idA?.ToString() ?? "n/a";
            var idBStr = idB?.ToString() ?? "n/a";
            var msg = $"Company mismatch: {labelA} (Id={idAStr}) and {labelB} (Id={idBStr}) must belong to the same company.";
            PlatformGuardLogger.LogViolation("FinancialIsolationGuard", "RequireSameCompany", msg, companyId: companyIdA, entityType: labelA, entityId: idA as Guid?);
            throw new InvalidOperationException(
                $"Company mismatch: {labelA} (CompanyId={companyIdA.Value}, Id={idAStr}) and " +
                $"{labelB} (CompanyId={companyIdB.Value}, Id={idBStr}) must belong to the same company.");
        }
    }

    /// <summary>
    /// Throws if any item has a company id that is null, empty, or different from <paramref name="expectedCompanyId"/>.
    /// Use when building a single financial result from multiple entities (e.g. invoice lines from orders).
    /// </summary>
    /// <param name="operationName">Short name of the operation for the message.</param>
    /// <param name="expectedCompanyId">The company id all entities must match.</param>
    /// <param name="items">Each item: (label, companyId, optional id).</param>
    /// <exception cref="InvalidOperationException">When any item has missing or mismatched company id.</exception>
    public static void RequireSameCompanySet(
        string operationName,
        Guid expectedCompanyId,
        IEnumerable<(string Label, Guid? CompanyId, object? Id)> items)
    {
        if (expectedCompanyId == Guid.Empty)
        {
            PlatformGuardLogger.LogViolation("FinancialIsolationGuard", operationName, "Expected company id cannot be empty.");
            throw new InvalidOperationException(
                $"{operationName}: Expected company id cannot be empty for this financial operation.");
        }
        var list = items?.ToList() ?? new List<(string, Guid?, object?)>();
        foreach (var (label, companyId, id) in list)
        {
            if (!companyId.HasValue || companyId.Value == Guid.Empty)
            {
                var msg = $"{label} has missing or empty CompanyId. Id={id?.ToString() ?? "n/a"}.";
                PlatformGuardLogger.LogViolation("FinancialIsolationGuard", operationName, msg, companyId: companyId, entityType: label, entityId: id as Guid?);
                throw new InvalidOperationException(
                    $"{operationName}: {label} has missing or empty CompanyId. Id={id?.ToString() ?? "n/a"}. " +
                    "All entities in this operation must belong to the same company.");
            }
            if (companyId.Value != expectedCompanyId)
            {
                var msg = $"{label} (CompanyId={companyId}, Id={id?.ToString() ?? "n/a"}) does not match expected company {expectedCompanyId}.";
                PlatformGuardLogger.LogViolation("FinancialIsolationGuard", operationName, msg, companyId: companyId, entityType: label, entityId: id as Guid?);
                throw new InvalidOperationException(
                    $"{operationName}: Company mismatch. {label} (CompanyId={companyId}, Id={id?.ToString() ?? "n/a"}) " +
                    $"does not match expected company {expectedCompanyId}.");
            }
        }
    }
}
