using CephasOps.Application.Billing.Services;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Tenant isolation for BillingRatecardService: no Guid.Empty "all tenants";
/// missing context fails closed; cross-tenant get returns null.
/// </summary>
[Collection("TenantScopeTests")]
public class BillingRatecardTenantIsolationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BillingRatecardService _service;
    private readonly Guid? _previousTenantId;

    public BillingRatecardTenantIsolationTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new BillingRatecardService(_context, Mock.Of<ILogger<BillingRatecardService>>());
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetBillingRatecardsAsync_GuidEmptyAndNoTenantScope_ReturnsEmpty()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _service.GetBillingRatecardsAsync(Guid.Empty, cancellationToken: default);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetBillingRatecardByIdAsync_OtherTenantRatecard_ReturnsNull()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var ratecardId = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyB;
        try
        {
            _context.BillingRatecards.Add(new BillingRatecard
            {
                Id = ratecardId,
                CompanyId = companyB,
                Amount = 100m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var result = await _service.GetBillingRatecardByIdAsync(ratecardId, companyA);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateBillingRatecardAsync_GuidEmptyAndNoTenantScope_Throws()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var dto = new CephasOps.Application.Billing.DTOs.CreateBillingRatecardDto
            {
                Amount = 100m,
                IsActive = true
            };
            var act = () => _service.CreateBillingRatecardAsync(dto, Guid.Empty);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*tenant context*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetBillingRatecardsAsync_ValidCompanyId_ReturnsOnlyThatTenantsRatecards()
    {
        var companyA = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyA;
        try
        {
            _context.BillingRatecards.Add(new BillingRatecard
            {
                Id = Guid.NewGuid(),
                CompanyId = companyA,
                Amount = 50m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var result = await _service.GetBillingRatecardsAsync(companyA);
        result.Should().HaveCount(1);
        result![0].CompanyId.Should().Be(companyA);
    }
}
