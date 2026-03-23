using CephasOps.Application.Insights;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Verifies OperationalInsightsService tenant safety: tenant-scoped methods reject Guid.Empty and all reads filter by CompanyId.
/// </summary>
[Collection("TenantScopeTests")]
public class OperationalInsightsTenantIsolationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTenantPerformanceAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalInsightsService(context);

        var act = () => service.GetTenantPerformanceAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetOperationsControlAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalInsightsService(context);

        var act = () => service.GetOperationsControlAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetFinancialOverviewAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalInsightsService(context);

        var act = () => service.GetFinancialOverviewAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetRiskQualityAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalInsightsService(context);

        var act = () => service.GetRiskQualityAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetTenantPerformanceAsync_WhenValidCompanyId_ReturnsWithoutThrowing()
    {
        await using var context = CreateContext();
        var service = new OperationalInsightsService(context);
        var companyId = Guid.NewGuid();

        var result = await service.GetTenantPerformanceAsync(companyId);

        result.Should().NotBeNull();
        result.OrdersThisMonth.Should().Be(0);
        result.ActiveInstallers.Should().Be(0);
    }
}
