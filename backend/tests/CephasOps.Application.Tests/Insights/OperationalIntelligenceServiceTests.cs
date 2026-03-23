using CephasOps.Application.Insights;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace CephasOps.Application.Tests.Insights;

/// <summary>
/// Operational intelligence: tenant isolation (reject empty company), rule output shape, and platform summary.
/// </summary>
[Collection("TenantScopeTests")]
public class OperationalIntelligenceServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);

        var act = () => service.GetSummaryAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetOrdersAtRiskAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);

        var act = () => service.GetOrdersAtRiskAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetInstallersAtRiskAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);

        var act = () => service.GetInstallersAtRiskAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetBuildingsAtRiskAsync_WhenCompanyIdEmpty_Throws()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);

        var act = () => service.GetBuildingsAtRiskAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetSummaryAsync_WhenValidCompanyId_ReturnsSummaryWithCounts()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);
        var companyId = Guid.NewGuid();

        var result = await service.GetSummaryAsync(companyId);

        result.Should().NotBeNull();
        result.OrdersAtRiskCount.Should().Be(0);
        result.InstallersAtRiskCount.Should().Be(0);
        result.BuildingsAtRiskCount.Should().Be(0);
        result.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetOrdersAtRiskAsync_WhenNoMatchingOrders_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);
        var companyId = Guid.NewGuid();

        var result = await service.GetOrdersAtRiskAsync(companyId);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetOrdersAtRiskAsync_WhenStuckOrderExists_FlagsOrderWithStuckReason()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var stuckTime = DateTime.UtcNow.AddHours(-5);

        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            var order = new Order
            {
                Id = orderId,
                CompanyId = companyId,
                PartnerId = Guid.NewGuid(),
                SourceSystem = "Test",
                OrderTypeId = Guid.NewGuid(),
                BuildingId = Guid.NewGuid(),
                AddressLine1 = "A",
                City = "C",
                State = "S",
                Postcode = "P",
                CustomerName = "C",
                CustomerPhone = "P",
                AppointmentDate = DateTime.UtcNow.Date,
                AppointmentWindowFrom = TimeSpan.Zero,
                AppointmentWindowTo = TimeSpan.FromHours(1),
                Status = OrderStatus.Assigned,
                AssignedSiId = siId,
                UpdatedAt = stuckTime,
                CreatedAt = stuckTime.AddHours(-1)
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var options = new OperationalIntelligenceOptions { StuckOrderThresholdHours = 4, MaxResultsPerList = 50 };
        var service = new OperationalIntelligenceService(context, Options.Create(options));

        var result = await service.GetOrdersAtRiskAsync(companyId);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].OrderId.Should().Be(orderId);
        result[0].Reasons.Should().Contain(r => r.RuleCode == "StuckOrder");
        result[0].Severity.Should().Be("Critical");
    }

    [Fact]
    public async Task GetInstallersAtRiskAsync_WhenNoInstallers_ReturnsEmptyList()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);
        var companyId = Guid.NewGuid();

        var result = await service.GetInstallersAtRiskAsync(companyId);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetPlatformSummaryAsync_ReturnsAggregateSummary()
    {
        await using var context = CreateContext();
        var service = new OperationalIntelligenceService(context);

        var result = await service.GetPlatformSummaryAsync();

        result.Should().NotBeNull();
        result.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
