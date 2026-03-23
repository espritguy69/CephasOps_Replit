using CephasOps.Domain.Authorization;
using Xunit;

namespace CephasOps.Application.Tests.Authorization;

public class PermissionCatalogTests
{
    [Fact]
    public void All_ContainsExpectedPermissions()
    {
        Assert.Contains(PermissionCatalog.AdminUsersView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.AdminSecurityView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.PayoutHealthView, PermissionCatalog.All);
    }

    [Fact]
    public void All_ContainsPhase4ModulePermissions()
    {
        Assert.Contains(PermissionCatalog.OrdersView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.OrdersEdit, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.ReportsView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.ReportsExport, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.InventoryView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.InventoryEdit, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.JobsView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.JobsRun, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.JobsAdmin, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.SettingsView, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.SettingsEdit, PermissionCatalog.All);
    }

    [Theory]
    [InlineData("admin.users.view", true)]
    [InlineData("orders.view", true)]
    [InlineData("reports.export", true)]
    [InlineData("jobs.view", true)]
    [InlineData("settings.edit", true)]
    [InlineData("invalid.permission", false)]
    [InlineData("", false)]
    public void IsValid_ReturnsExpected(string name, bool expected)
    {
        Assert.Equal(expected, PermissionCatalog.IsValid(name));
    }

    [Fact]
    public void IsValid_Null_ReturnsFalse()
    {
        Assert.False(PermissionCatalog.IsValid(null!));
    }

    [Fact]
    public void FilterValid_KeepsOnlyValidNames()
    {
        var input = new[] { PermissionCatalog.AdminUsersView, "invalid", PermissionCatalog.OrdersView, "" };
        var result = PermissionCatalog.FilterValid(input);
        Assert.Equal(2, result.Count);
        Assert.Contains(PermissionCatalog.AdminUsersView, result);
        Assert.Contains(PermissionCatalog.OrdersView, result);
    }

    [Fact]
    public void All_ContainsRbacV3FieldLevelPermissions()
    {
        Assert.Contains(PermissionCatalog.OrdersViewPrice, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.PayrollViewPayout, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.PayrollEditPayout, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.RatesViewAmounts, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.InventoryViewCost, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.InventoryEditCost, PermissionCatalog.All);
        Assert.Contains(PermissionCatalog.ReportsViewFinancial, PermissionCatalog.All);
    }

    [Theory]
    [InlineData("orders.view.price", true)]
    [InlineData("payroll.view.payout", true)]
    [InlineData("inventory.view.cost", true)]
    [InlineData("reports.view.financial", true)]
    public void IsValid_AcceptsFieldLevelPermissions(string name, bool expected)
    {
        Assert.Equal(expected, PermissionCatalog.IsValid(name));
    }
}
