using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Integration;

/// <summary>
/// Tests for TenantScopeExecutor: tenant scope restores, platform bypass exits, nested execution, and exception path restores state.
/// </summary>
[Collection("TenantScopeTests")]
public class TenantScopeExecutorTests
{
    [Fact]
    public async Task RunWithTenantScopeAsync_RestoresPreviousScope_AfterSuccess()
    {
        var previous = Guid.NewGuid();
        var scopeCompanyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await TenantScopeExecutor.RunWithTenantScopeAsync(scopeCompanyId, async (ct) =>
        {
            TenantScope.CurrentTenantId.Should().Be(scopeCompanyId);
            await Task.CompletedTask;
        });

        TenantScope.CurrentTenantId.Should().Be(previous);
    }

    [Fact]
    public async Task RunWithTenantScopeAsync_RestoresPreviousScope_AfterException()
    {
        var previous = Guid.NewGuid();
        var scopeCompanyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TenantScopeExecutor.RunWithTenantScopeAsync(scopeCompanyId, async (ct) =>
            {
                TenantScope.CurrentTenantId.Should().Be(scopeCompanyId);
                throw new InvalidOperationException("test");
            }));

        TenantScope.CurrentTenantId.Should().Be(previous);
    }

    [Fact]
    public async Task RunWithTenantScopeAsync_ReturnsResult()
    {
        var companyId = Guid.NewGuid();
        var result = await TenantScopeExecutor.RunWithTenantScopeAsync(companyId, async (ct) =>
        {
            TenantScope.CurrentTenantId.Should().Be(companyId);
            return await Task.FromResult(42);
        });
        result.Should().Be(42);
    }

    [Fact]
    public async Task RunWithTenantScopeAsync_ThrowsWhenCompanyIdEmpty()
    {
        var previous = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        var act = () => TenantScopeExecutor.RunWithTenantScopeAsync(Guid.Empty, async _ => await Task.CompletedTask);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("companyId")
            .WithMessage("*non-empty*");

        TenantScope.CurrentTenantId.Should().Be(previous); // scope was never set; executor threw before entering scope
    }

    [Fact]
    public async Task RunWithTenantScopeAsync_ThrowsWhenCompanyIdEmpty_Generic()
    {
        var act = () => TenantScopeExecutor.RunWithTenantScopeAsync(Guid.Empty, async _ => await Task.FromResult(1));
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("companyId");
    }

    [Fact]
    public async Task RunWithPlatformBypassAsync_ExitsBypass_AfterSuccess()
    {
        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
        await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
            await Task.CompletedTask;
        });
        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
    }

    [Fact]
    public async Task RunWithPlatformBypassAsync_ExitsBypass_AfterException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
            {
                TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
                throw new InvalidOperationException("test");
            }));
        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
    }

    [Fact]
    public async Task RunWithTenantScopeOrBypassAsync_WhenCompanyIdSet_RunsUnderScopeAndRestores()
    {
        var previous = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, async (ct) =>
        {
            TenantScope.CurrentTenantId.Should().Be(companyId);
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
            await Task.CompletedTask;
        });

        TenantScope.CurrentTenantId.Should().Be(previous);
    }

    [Fact]
    public async Task RunWithTenantScopeOrBypassAsync_WhenCompanyIdNull_RunsUnderBypassAndExits()
    {
        var previous = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync((Guid?)null, async (ct) =>
        {
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
            await Task.CompletedTask;
        });

        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
        TenantScope.CurrentTenantId.Should().Be(previous);
    }

    [Fact]
    public async Task Nested_TenantThenPlatform_RestoresOuterScope()
    {
        var outer = Guid.NewGuid();
        var innerCompany = Guid.NewGuid();
        TenantScope.CurrentTenantId = outer;

        await TenantScopeExecutor.RunWithTenantScopeAsync(innerCompany, async (ct) =>
        {
            TenantScope.CurrentTenantId.Should().Be(innerCompany);
            await TenantScopeExecutor.RunWithPlatformBypassAsync(async (c) =>
            {
                TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
                await Task.CompletedTask;
            });
            TenantScope.CurrentTenantId.Should().Be(innerCompany);
        });

        TenantScope.CurrentTenantId.Should().Be(outer);
    }

    [Fact]
    public async Task Nested_PlatformThenTenant_RestoresBypassThenScope()
    {
        var outer = Guid.NewGuid();
        TenantScope.CurrentTenantId = outer;

        await TenantScopeExecutor.RunWithPlatformBypassAsync(async (ct) =>
        {
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
            var innerCompany = Guid.NewGuid();
            await TenantScopeExecutor.RunWithTenantScopeAsync(innerCompany, async (c) =>
            {
                TenantScope.CurrentTenantId.Should().Be(innerCompany);
                await Task.CompletedTask;
            });
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
        });

        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
        TenantScope.CurrentTenantId.Should().Be(outer);
    }

    [Fact]
    public async Task RunWithTenantScopeOrBypassAsync_WhenCompanyIdEmptyGuid_RunsUnderBypass()
    {
        var previous = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(Guid.Empty, async (ct) =>
        {
            TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
            await Task.CompletedTask;
        });

        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
        TenantScope.CurrentTenantId.Should().Be(previous);
    }

    [Fact]
    public async Task RunWithTenantScopeOrBypassAsync_WhenCompanyIdSet_Exception_RestoresScope()
    {
        var previous = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId, async (ct) =>
            {
                TenantScope.CurrentTenantId.Should().Be(companyId);
                throw new InvalidOperationException("test");
            }));

        TenantScope.CurrentTenantId.Should().Be(previous);
    }

    [Fact]
    public async Task RunWithTenantScopeOrBypassAsync_WhenCompanyIdNull_Exception_ExitsBypass()
    {
        var previous = Guid.NewGuid();
        TenantScope.CurrentTenantId = previous;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TenantScopeExecutor.RunWithTenantScopeOrBypassAsync((Guid?)null, async (ct) =>
            {
                TenantSafetyGuard.IsPlatformBypassActive.Should().BeTrue();
                throw new InvalidOperationException("test");
            }));

        TenantSafetyGuard.IsPlatformBypassActive.Should().BeFalse();
        TenantScope.CurrentTenantId.Should().Be(previous);
    }
}
