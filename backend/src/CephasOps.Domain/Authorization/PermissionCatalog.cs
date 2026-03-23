namespace CephasOps.Domain.Authorization;

/// <summary>
/// Central catalog of permission names (module.action). Used for validation, seeding, and authorization.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>All known permission names. Used to validate role-permission assignments.</summary>
    public static IReadOnlyList<string> All => AllOrdered;

    /// <summary>Permissions grouped by module for UI display.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByModule => ByModuleDict;

    // ---------- Admin ----------
    public const string AdminUsersView = "admin.users.view";
    public const string AdminUsersEdit = "admin.users.edit";
    public const string AdminUsersResetPassword = "admin.users.reset-password";
    public const string AdminSecurityView = "admin.security.view";
    public const string AdminSecuritySessionsRevoke = "admin.security.sessions.revoke";
    public const string AdminRolesView = "admin.roles.view";
    public const string AdminRolesEdit = "admin.roles.edit";
    /// <summary>Phase 11: List/view tenants.</summary>
    public const string AdminTenantsView = "admin.tenants.view";
    /// <summary>Phase 11: Create/update tenants and assign companies.</summary>
    public const string AdminTenantsEdit = "admin.tenants.edit";
    /// <summary>Phase 12: View billing plans and tenant subscriptions.</summary>
    public const string AdminBillingPlansView = "admin.billing.plans.view";
    /// <summary>Phase 12: Manage billing plans and tenant subscriptions.</summary>
    public const string AdminBillingPlansEdit = "admin.billing.plans.edit";

    // ---------- Payout ----------
    public const string PayoutHealthView = "payout.health.view";
    public const string PayoutRepairRun = "payout.repair.run";
    public const string PayoutAnomaliesReview = "payout.anomalies.review";

    // ---------- Rates ----------
    public const string RatesView = "rates.view";
    public const string RatesEdit = "rates.edit";
    /// <summary>RBAC v3: View rate amounts (revenue, payout, base work rate).</summary>
    public const string RatesViewAmounts = "rates.view.amounts";

    // ---------- Payroll ----------
    public const string PayrollView = "payroll.view";
    public const string PayrollRun = "payroll.run";
    /// <summary>RBAC v3: View payroll amounts (run total, line pay, job earnings, SI rate plan amounts).</summary>
    public const string PayrollViewPayout = "payroll.view.payout";
    /// <summary>RBAC v3: Edit payroll payout amounts and SI rate plans.</summary>
    public const string PayrollEditPayout = "payroll.edit.payout";

    // ---------- Orders ----------
    public const string OrdersView = "orders.view";
    public const string OrdersEdit = "orders.edit";
    /// <summary>RBAC v3: View order revenue, payout, profit (RevenueAmount, PayoutAmount, ProfitAmount).</summary>
    public const string OrdersViewPrice = "orders.view.price";

    // ---------- Settings (broad) ----------
    public const string SettingsView = "settings.view";
    public const string SettingsEdit = "settings.edit";

    // ---------- Reports ----------
    public const string ReportsExport = "reports.export";
    /// <summary>RBAC v3: View financial columns in report run/export.</summary>
    public const string ReportsViewFinancial = "reports.view.financial";

    // ---------- Background jobs ----------
    public const string JobsView = "jobs.view";
    public const string JobsRun = "jobs.run";
    public const string JobsAdmin = "jobs.admin";

    // ---------- Other modules ----------
    public const string InventoryView = "inventory.view";
    public const string InventoryEdit = "inventory.edit";
    /// <summary>RBAC v3: View material default cost.</summary>
    public const string InventoryViewCost = "inventory.view.cost";
    /// <summary>RBAC v3: Edit material default cost.</summary>
    public const string InventoryEditCost = "inventory.edit.cost";
    public const string BuildingsView = "buildings.view";
    public const string BillingView = "billing.view";
    public const string PnlView = "pnl.view";
    public const string ReportsView = "reports.view";
    public const string SchedulerView = "scheduler.view";
    public const string AssetsView = "assets.view";
    public const string AccountingView = "accounting.view";
    public const string EmailView = "email.view";
    public const string DocumentsView = "documents.view";
    public const string FilesView = "files.view";
    public const string WorkflowView = "workflow.view";
    public const string KpiView = "kpi.view";
    public const string AdminView = "admin.view";

    private static readonly string[] AllOrdered =
    {
        AdminView,
        AdminUsersView,
        AdminUsersEdit,
        AdminUsersResetPassword,
        AdminSecurityView,
        AdminSecuritySessionsRevoke,
        AdminRolesView,
        AdminRolesEdit,
        AdminTenantsView,
        AdminTenantsEdit,
        AdminBillingPlansView,
        AdminBillingPlansEdit,
        PayoutHealthView,
        PayoutRepairRun,
        PayoutAnomaliesReview,
        RatesView,
        RatesEdit,
        RatesViewAmounts,
        PayrollView,
        PayrollRun,
        PayrollViewPayout,
        PayrollEditPayout,
        OrdersView,
        OrdersEdit,
        OrdersViewPrice,
        SettingsView,
        SettingsEdit,
        ReportsExport,
        ReportsViewFinancial,
        JobsView,
        JobsRun,
        JobsAdmin,
        InventoryView,
        InventoryEdit,
        InventoryViewCost,
        InventoryEditCost,
        BuildingsView,
        BillingView,
        PnlView,
        ReportsView,
        SchedulerView,
        AssetsView,
        AccountingView,
        EmailView,
        DocumentsView,
        FilesView,
        WorkflowView,
        KpiView,
    };

    private static readonly Dictionary<string, IReadOnlyList<string>> ByModuleDict = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Admin"] = new[] { AdminView, AdminUsersView, AdminUsersEdit, AdminUsersResetPassword, AdminSecurityView, AdminSecuritySessionsRevoke, AdminRolesView, AdminRolesEdit, AdminTenantsView, AdminTenantsEdit, AdminBillingPlansView, AdminBillingPlansEdit },
        ["Payout"] = new[] { PayoutHealthView, PayoutRepairRun, PayoutAnomaliesReview },
        ["Rates"] = new[] { RatesView, RatesEdit, RatesViewAmounts },
        ["Payroll"] = new[] { PayrollView, PayrollRun, PayrollViewPayout, PayrollEditPayout },
        ["Orders"] = new[] { OrdersView, OrdersEdit, OrdersViewPrice },
        ["Settings"] = new[] { SettingsView, SettingsEdit },
        ["Jobs"] = new[] { JobsView, JobsRun, JobsAdmin },
        ["Inventory"] = new[] { InventoryView, InventoryEdit, InventoryViewCost, InventoryEditCost },
        ["Buildings"] = new[] { BuildingsView },
        ["Billing"] = new[] { BillingView },
        ["P&L"] = new[] { PnlView },
        ["Reports"] = new[] { ReportsView, ReportsExport, ReportsViewFinancial },
        ["Scheduler"] = new[] { SchedulerView },
        ["Assets"] = new[] { AssetsView },
        ["Accounting"] = new[] { AccountingView },
        ["Email"] = new[] { EmailView },
        ["Documents"] = new[] { DocumentsView },
        ["Files"] = new[] { FilesView },
        ["Workflow"] = new[] { WorkflowView },
        ["KPI"] = new[] { KpiView },
    };

    /// <summary>Returns true if the permission name is in the catalog.</summary>
    public static bool IsValid(string permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName)) return false;
        return AllOrdered.Contains(permissionName, StringComparer.Ordinal);
    }

    /// <summary>Returns all permission names that are valid; invalid names are skipped.</summary>
    public static IReadOnlyList<string> FilterValid(IEnumerable<string> permissionNames)
    {
        var set = new HashSet<string>(AllOrdered, StringComparer.Ordinal);
        return permissionNames.Where(p => !string.IsNullOrWhiteSpace(p) && set.Contains(p)).Distinct(StringComparer.Ordinal).ToList();
    }
}
