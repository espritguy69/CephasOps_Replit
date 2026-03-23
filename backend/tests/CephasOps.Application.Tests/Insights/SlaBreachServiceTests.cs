using CephasOps.Application.Insights;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace CephasOps.Application.Tests.Insights;

[Collection("TenantScopeTests")]
public class SlaBreachServiceTests
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
        var service = new SlaBreachService(context);

        var act = () => service.GetSummaryAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetSummaryAsync_WhenValidCompanyId_ReturnsDistribution()
    {
        await using var context = CreateContext();
        var service = new SlaBreachService(context);
        var companyId = Guid.NewGuid();

        var result = await service.GetSummaryAsync(companyId);

        result.Should().NotBeNull();
        result.Distribution.Should().NotBeNull();
        result.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetOrdersAtRiskAsync_WhenNoCompany_Throws()
    {
        await using var context = CreateContext();
        var service = new SlaBreachService(context);

        var act = () => service.GetOrdersAtRiskAsync(Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CompanyId cannot be empty*");
    }

    [Fact]
    public async Task GetOrdersAtRiskAsync_WhenOrderWithKpiDueAtPast_FlagsAsBreached()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var duePast = DateTime.UtcNow.AddMinutes(-60);

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
                KpiDueAt = duePast,
                CreatedAt = duePast.AddHours(-1),
                UpdatedAt = DateTime.UtcNow
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var service = new SlaBreachService(context, Options.Create(new OperationalSlaOptions()));

        var result = await service.GetOrdersAtRiskAsync(companyId);

        result.Should().NotBeNull().And.HaveCount(1);
        result[0].BreachState.Should().Be(SlaBreachState.Breached);
        result[0].MinutesToDueOrOverdue.Should().BeLessThan(0);
    }

    [Fact]
    public async Task GetOrdersAtRiskAsync_WhenOrderWithNoKpiDueAt_NotInAtRiskList()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();

        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
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
                KpiDueAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var service = new SlaBreachService(context);

        var result = await service.GetOrdersAtRiskAsync(companyId);

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetPlatformSummaryAsync_ReturnsSummary()
    {
        await using var context = CreateContext();
        var service = new SlaBreachService(context);

        var result = await service.GetPlatformSummaryAsync();

        result.Should().NotBeNull();
        result.Distribution.Should().NotBeNull();
        result.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
