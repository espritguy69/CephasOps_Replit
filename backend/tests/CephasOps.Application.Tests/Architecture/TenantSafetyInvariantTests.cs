using CephasOps.Application.Common;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Infrastructure;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Architecture;

/// <summary>
/// Architecture invariant tests for tenant-safety: guards reject invalid behavior,
/// tenant-scoped writes fail without scope, null-company paths fail closed, and
/// approved runtime paths use the expected tenant-safety model.
/// High-signal suite only; see TenantScopeExecutorTests and FinancialIsolationGuardTests for broader coverage.
/// </summary>
public class TenantSafetyInvariantTests
{
    [Fact]
    public void AssertTenantContext_WhenNoTenantAndNoBypass_Throws()
    {
        EnsureCleanScope();
        var act = () => TenantSafetyGuard.AssertTenantContext();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TenantSafetyGuard*Tenant context is required*");
    }

    [Fact]
    public void AssertTenantContext_WhenTenantSet_DoesNotThrow()
    {
        EnsureCleanScope();
        var companyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyId;
        try
        {
            var act = () => TenantSafetyGuard.AssertTenantContext();
            act.Should().NotThrow();
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public void AssertTenantContext_WhenPlatformBypassActive_DoesNotThrow()
    {
        EnsureCleanScope();
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            var act = () => TenantSafetyGuard.AssertTenantContext();
            act.Should().NotThrow();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutTenantContext_ThrowsForTenantScopedEntity()
    {
        EnsureCleanScope();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Name = "Test",
            IsActive = true
        });

        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TenantSafetyGuard*Cannot save tenant-scoped entity without tenant context*");
    }

    [Fact]
    public async Task SaveChangesAsync_WithTenantContext_SucceedsForTenantScopedEntity()
    {
        EnsureCleanScope();
        var companyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        try
        {
            await using var context = new ApplicationDbContext(options);
            context.Departments.Add(new Department
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Test",
                IsActive = true
            });
            await context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public void RequireTenantOrBypass_WhenNoTenantAndNoBypass_Throws()
    {
        EnsureCleanScope();
        var act = () => FinancialIsolationGuard.RequireTenantOrBypass("TestOp");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestOp*tenant context*or*platform bypass*");
    }

    [Fact]
    public void RequireCompany_WhenNull_Throws()
    {
        var act = () => FinancialIsolationGuard.RequireCompany(null, "TestOp");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Company context is required*");
    }

    [Fact]
    public async Task RunWithTenantScopeAsync_WhenCompanyIdEmpty_ThrowsBeforeEnteringScope()
    {
        EnsureCleanScope();
        var previous = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;
        try
        {
            var act = () => TenantScopeExecutor.RunWithTenantScopeAsync(Guid.Empty, _ => Task.CompletedTask);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithParameterName("companyId")
                .WithMessage("*non-empty*");
            TenantScope.CurrentTenantId.Should().Be(previous);
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task RunWithPlatformBypassAsync_ExitsBypass_SoSaveChangesWithoutScopeStillFailsAfterExit()
    {
        EnsureCleanScope();
        await TenantScopeExecutor.RunWithPlatformBypassAsync(_ => Task.CompletedTask);
        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        context.Departments.Add(new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Name = "Test",
            IsActive = true
        });
        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TenantSafetyGuard*");
    }

    private static void EnsureCleanScope()
    {
        while (TenantSafetyGuard.IsPlatformBypassActive)
            TenantSafetyGuard.ExitPlatformBypass();
        TenantScope.CurrentTenantId = null;
    }
}
