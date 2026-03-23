using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Billing;

/// <summary>
/// Tests for BillingRatecardService, including ServiceCategory validation (must match OrderCategory.Code). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class BillingRatecardServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BillingRatecardService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public BillingRatecardServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        SeedCompanyAndOrderCategory();
        _service = new BillingRatecardService(_context, Mock.Of<ILogger<BillingRatecardService>>());
    }

    private void SeedCompanyAndOrderCategory()
    {
        _context.Companies.Add(new Domain.Companies.Entities.Company
        {
            Id = _companyId,
            ShortName = "Test",
            LegalName = "Test Co",
            IsActive = true
        });
        _context.OrderCategories.Add(new OrderCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "FTTH",
            Code = "FTTH",
            IsActive = true
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateBillingRatecard_WithUnknownServiceCategory_ThrowsArgumentException()
    {
        var dto = new CreateBillingRatecardDto
        {
            OrderTypeId = Guid.NewGuid(),
            ServiceCategory = "INVALID_CODE",
            Amount = 100m,
            IsActive = true
        };

        var act = () => _service.CreateBillingRatecardAsync(dto, _companyId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ServiceCategory*INVALID_CODE*OrderCategory*Code*");
    }

    [Fact]
    public async Task CreateBillingRatecard_WithValidServiceCategory_Succeeds()
    {
        var orderTypeId = Guid.NewGuid();
        _context.OrderTypes.Add(new OrderType
        {
            Id = orderTypeId,
            CompanyId = _companyId,
            Name = "Activation",
            Code = "ACT",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var dto = new CreateBillingRatecardDto
        {
            OrderTypeId = orderTypeId,
            ServiceCategory = "FTTH",
            Amount = 100m,
            IsActive = true
        };

        var result = await _service.CreateBillingRatecardAsync(dto, _companyId);

        result.Should().NotBeNull();
        result.ServiceCategory.Should().Be("FTTH");
        result.Amount.Should().Be(100m);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
