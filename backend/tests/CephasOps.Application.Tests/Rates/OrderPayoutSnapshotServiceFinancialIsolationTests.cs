using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Rates;

/// <summary>
/// Financial isolation: GetPayoutWithSnapshotOrLiveAsync rejects snapshot from different company when companyId is provided. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class OrderPayoutSnapshotServiceFinancialIsolationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderPayoutSnapshotService _service;
    private readonly Guid _companyA;
    private readonly Guid _companyB;
    private readonly Guid _orderId;
    private readonly Guid? _previousTenantId;

    public OrderPayoutSnapshotServiceFinancialIsolationTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
        _orderId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        SeedData();
        var profitabilityMock = new Mock<IOrderProfitabilityService>();
        profitabilityMock
            .Setup(x => x.GetOrderPayoutBreakdownAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GponRateResolutionResult?)null);
        _service = new OrderPayoutSnapshotService(
            _context,
            profitabilityMock.Object,
            Mock.Of<ILogger<OrderPayoutSnapshotService>>());
    }

    private void SeedData()
    {
        TenantScope.CurrentTenantId = _companyA;
        try
        {
            _context.Orders.Add(new Order
            {
                Id = _orderId,
                CompanyId = _companyA,
                ServiceId = "S1",
                Status = "Completed",
                SourceSystem = "Manual",
                AddressLine1 = "A",
                City = "KL",
                State = "KL",
                Postcode = "50000"
            });
            _context.SaveChanges();

            var snapshot = new OrderPayoutSnapshot
            {
                Id = Guid.NewGuid(),
                OrderId = _orderId,
                CompanyId = _companyA,
                FinalPayout = 100m,
                Currency = "MYR",
                PayoutPath = "BaseWorkRate",
                CalculatedAt = DateTime.UtcNow,
                Provenance = "NormalFlow"
            };
            _context.OrderPayoutSnapshots.Add(snapshot);
            _context.SaveChanges();
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task GetPayoutWithSnapshotOrLiveAsync_WhenSnapshotExistsAndSameCompany_ReturnsSnapshot()
    {
        var result = await _service.GetPayoutWithSnapshotOrLiveAsync(_orderId, _companyA, referenceDate: null);

        result.Source.Should().Be("Snapshot");
        result.Result.Should().NotBeNull();
        result.Result!.Success.Should().BeTrue();
        result.Result.PayoutAmount.Should().Be(100m);
    }

    [Fact]
    public async Task GetPayoutWithSnapshotOrLiveAsync_WhenSnapshotExistsAndCompanyMismatch_Throws()
    {
        var act = () => _service.GetPayoutWithSnapshotOrLiveAsync(_orderId, _companyB, referenceDate: null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Company mismatch*Snapshot*Request*must belong to the same company*");
    }

    [Fact]
    public async Task GetSnapshotByOrderIdAsync_WhenScopeIsOtherTenant_ReturnsNull()
    {
        TenantScope.CurrentTenantId = _companyB;
        try
        {
            var result = await _service.GetSnapshotByOrderIdAsync(_orderId);
            result.Should().BeNull();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetPayoutWithSnapshotOrLiveAsync_WhenNoCompanyContext_ReturnsFailClosedResponse()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _service.GetPayoutWithSnapshotOrLiveAsync(_orderId, null, referenceDate: null);
            result.Source.Should().Be("None");
            result.Result.Should().NotBeNull();
            result.Result!.Success.Should().BeFalse();
            result.Result.ErrorMessage.Should().Contain("Company context is required");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
