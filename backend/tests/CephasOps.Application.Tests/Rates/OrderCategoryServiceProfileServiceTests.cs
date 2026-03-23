using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Rates;

[Collection("TenantScopeTests")]
public class OrderCategoryServiceProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderCategoryServiceProfileService _service;
    private readonly Guid _companyId;
    private Guid _orderCategoryId;
    private Guid _serviceProfileId;
    private readonly Guid? _previousTenantId;

    public OrderCategoryServiceProfileServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new OrderCategoryServiceProfileService(_context);
    }

    private async Task SeedReferenceDataAsync()
    {
        var oc = new OrderCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "FTTH",
            Code = "FTTH",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderCategories.Add(oc);
        _orderCategoryId = oc.Id;

        var sp = new ServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Code = "RESIDENTIAL_FIBER",
            Name = "Residential Fiber",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ServiceProfiles.Add(sp);
        _serviceProfileId = sp.Id;
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_AddsMapping()
    {
        await SeedReferenceDataAsync();
        var dto = new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = _serviceProfileId
        };
        var result = await _service.CreateAsync(dto, _companyId);
        result.Id.Should().NotBeEmpty();
        result.OrderCategoryId.Should().Be(_orderCategoryId);
        result.ServiceProfileId.Should().Be(_serviceProfileId);
        result.OrderCategoryName.Should().Be("FTTH");
        result.ServiceProfileCode.Should().Be("RESIDENTIAL_FIBER");

        var list = await _service.ListAsync(_companyId, null);
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMapping()
    {
        await SeedReferenceDataAsync();
        var created = await _service.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = _serviceProfileId
        }, _companyId);

        var get = await _service.GetByIdAsync(created.Id, _companyId);
        get.Should().NotBeNull();
        get!.OrderCategoryId.Should().Be(_orderCategoryId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMapping()
    {
        await SeedReferenceDataAsync();
        var created = await _service.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = _serviceProfileId
        }, _companyId);

        await _service.DeleteAsync(created.Id, _companyId);

        var list = await _service.ListAsync(_companyId, null);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_DuplicateOrderCategory_Throws()
    {
        await SeedReferenceDataAsync();
        await _service.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = _serviceProfileId
        }, _companyId);

        await _service.Invoking(s => s.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = _serviceProfileId
        }, _companyId)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already mapped*");
    }

    [Fact]
    public async Task CreateAsync_InvalidOrderCategoryId_Throws()
    {
        await SeedReferenceDataAsync();
        await _service.Invoking(s => s.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = Guid.NewGuid(),
            ServiceProfileId = _serviceProfileId
        }, _companyId)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Order Category*");
    }

    [Fact]
    public async Task CreateAsync_InvalidServiceProfileId_Throws()
    {
        await SeedReferenceDataAsync();
        await _service.Invoking(s => s.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = Guid.NewGuid()
        }, _companyId)).Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Service Profile*");
    }

    [Fact]
    public async Task ListAsync_FilterByServiceProfileId()
    {
        await SeedReferenceDataAsync();
        var sp2 = new ServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Code = "BUSINESS",
            Name = "Business",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ServiceProfiles.Add(sp2);
        var oc2 = new OrderCategory
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "FTTO",
            Code = "FTTO",
            IsActive = true,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderCategories.Add(oc2);
        await _context.SaveChangesAsync();

        await _service.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = _orderCategoryId,
            ServiceProfileId = _serviceProfileId
        }, _companyId);
        await _service.CreateAsync(new CreateOrderCategoryServiceProfileDto
        {
            OrderCategoryId = oc2.Id,
            ServiceProfileId = sp2.Id
        }, _companyId);

        var list = await _service.ListAsync(_companyId, new OrderCategoryServiceProfileListFilter { ServiceProfileId = _serviceProfileId });
        list.Should().HaveCount(1);
        list[0].ServiceProfileId.Should().Be(_serviceProfileId);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
