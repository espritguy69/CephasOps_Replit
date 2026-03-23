using CephasOps.Application.Common;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Common;

/// <summary>
/// Unit tests for FinancialIsolationGuard: same-company success, mismatched/missing company failure.
/// </summary>
public class FinancialIsolationGuardTests
{
    [Fact]
    public void RequireCompany_WhenValidGuid_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var act = () => FinancialIsolationGuard.RequireCompany(companyId, "TestOp");
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireCompany_WhenNull_Throws()
    {
        Guid? companyId = null;
        var act = () => FinancialIsolationGuard.RequireCompany(companyId, "TestOp");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestOp*Company context is required*CompanyId is missing or empty*");
    }

    [Fact]
    public void RequireCompany_WhenEmptyGuid_Throws()
    {
        var act = () => FinancialIsolationGuard.RequireCompany(Guid.Empty, "TestOp");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestOp*Company context is required*");
    }

    [Fact]
    public void RequireSameCompany_WhenBothSame_DoesNotThrow()
    {
        var id = Guid.NewGuid();
        var act = () => FinancialIsolationGuard.RequireSameCompany(id, id, "Order", "Invoice", null, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireSameCompany_WhenMismatched_Throws()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var act = () => FinancialIsolationGuard.RequireSameCompany(a, b, "Order", "Invoice", null, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Company mismatch*Order*Invoice*must belong to the same company*");
    }

    [Fact]
    public void RequireSameCompany_WhenFirstMissing_Throws()
    {
        var b = Guid.NewGuid();
        var act = () => FinancialIsolationGuard.RequireSameCompany(null, b, "Order", "Invoice", null, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order*company id is missing or empty*");
    }

    [Fact]
    public void RequireSameCompany_WhenSecondMissing_Throws()
    {
        var a = Guid.NewGuid();
        var act = () => FinancialIsolationGuard.RequireSameCompany(a, null, "Order", "Invoice", null, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invoice*company id is missing or empty*");
    }

    [Fact]
    public void RequireSameCompanySet_WhenAllMatch_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var items = new[] { ("Order", (Guid?)companyId, (object?)Guid.NewGuid()), ("Order", (Guid?)companyId, (object?)Guid.NewGuid()) };
        var act = () => FinancialIsolationGuard.RequireSameCompanySet("TestOp", companyId, items);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireSameCompanySet_WhenExpectedEmpty_Throws()
    {
        var act = () => FinancialIsolationGuard.RequireSameCompanySet("TestOp", Guid.Empty, Array.Empty<(string, Guid?, object?)>());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Expected company id cannot be empty*");
    }

    [Fact]
    public void RequireSameCompanySet_WhenOneItemMismatched_Throws()
    {
        var expected = Guid.NewGuid();
        var other = Guid.NewGuid();
        var items = new[] { ("Order", (Guid?)expected, (object?)Guid.NewGuid()), ("Order", (Guid?)other, (object?)Guid.NewGuid()) };
        var act = () => FinancialIsolationGuard.RequireSameCompanySet("TestOp", expected, items);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Company mismatch*does not match expected company*");
    }

    [Fact]
    public void RequireSameCompanySet_WhenOneItemMissingCompany_Throws()
    {
        var expected = Guid.NewGuid();
        var items = new[] { ("Order", (Guid?)expected, (object?)Guid.NewGuid()), ("Order", (Guid?)null, (object?)Guid.NewGuid()) };
        var act = () => FinancialIsolationGuard.RequireSameCompanySet("TestOp", expected, items);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing or empty CompanyId*");
    }

    [Fact]
    public void RequireTenantOrBypass_WhenTenantSet_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId;
            var act = () => FinancialIsolationGuard.RequireTenantOrBypass("TestOp");
            act.Should().NotThrow();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public void RequireTenantOrBypass_WhenBypassActive_DoesNotThrow()
    {
        try
        {
            TenantSafetyGuard.EnterPlatformBypass();
            var act = () => FinancialIsolationGuard.RequireTenantOrBypass("TestOp");
            act.Should().NotThrow();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    [Fact]
    public void RequireTenantOrBypass_WhenNoTenantAndNoBypass_Throws()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = null;
            var act = () => FinancialIsolationGuard.RequireTenantOrBypass("TestOp");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*TestOp*Financial operations require a valid tenant context*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }
}
