namespace CephasOps.Application.Workflow;

/// <summary>
/// Tenant fairness ordering: round-robin by tenant so no tenant gets more than maxPerTenant items in one pass.
/// Used by BackgroundJobProcessorService to limit jobs per tenant per cycle.
/// </summary>
public static class TenantFairnessOrdering
{
    /// <summary>
    /// Orders items by tenant (grouped by getTenantId) so that each tenant appears at most maxPerTenant times,
    /// in round-robin order. If maxPerTenant &lt;= 0, returns the original list unchanged.
    /// </summary>
    public static List<T> OrderByTenantFairness<T>(IReadOnlyList<T> items, Func<T, Guid?> getTenantId, int maxPerTenant)
    {
        if (items.Count == 0 || maxPerTenant <= 0)
            return items.ToList();

        var grouped = items
            .GroupBy(i => getTenantId(i) ?? Guid.Empty)
            .ToDictionary(g => g.Key, g => g.ToList());
        var result = new List<T>();
        var indices = grouped.Keys.ToDictionary(k => k, _ => 0);
        while (true)
        {
            var added = 0;
            foreach (var kv in grouped)
            {
                var list = kv.Value;
                var idx = indices[kv.Key];
                if (idx >= maxPerTenant) continue;
                if (idx >= list.Count) continue;
                result.Add(list[idx]);
                indices[kv.Key] = idx + 1;
                added++;
            }
            if (added == 0) break;
        }
        return result;
    }
}
