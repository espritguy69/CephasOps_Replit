using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Rates;

[Collection("TenantScopeTests")]
public class ServiceProfileServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ServiceProfileService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public ServiceProfileServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new ServiceProfileService(_context);
    }

    [Fact]
    public async Task CreateAsync_AddsProfile()
    {
        var dto = new CreateServiceProfileDto
        {
            Code = "RESIDENTIAL_FIBER",
            Name = "Residential Fiber",
            Description = "FTTH, FTTR",
            IsActive = true,
            DisplayOrder = 1
        };
        var result = await _service.CreateAsync(dto, _companyId);
        result.Id.Should().NotBeEmpty();
        result.Code.Should().Be("RESIDENTIAL_FIBER");
        result.Name.Should().Be("Residential Fiber");
        result.IsActive.Should().BeTrue();
        result.DisplayOrder.Should().Be(1);

        var list = await _service.ListAsync(_companyId, null);
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProfile()
    {
        var created = await _service.CreateAsync(new CreateServiceProfileDto
        {
            Code = "BUSINESS_FIBER",
            Name = "Business Fiber",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);

        var get = await _service.GetByIdAsync(created.Id, _companyId);
        get.Should().NotBeNull();
        get!.Code.Should().Be("BUSINESS_FIBER");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesProfile()
    {
        var created = await _service.CreateAsync(new CreateServiceProfileDto
        {
            Code = "MAINT",
            Name = "Maintenance",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);

        var updated = await _service.UpdateAsync(created.Id, new UpdateServiceProfileDto
        {
            Name = "Maintenance & Assurance",
            IsActive = false
        }, _companyId);

        updated.Name.Should().Be("Maintenance & Assurance");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        var created = await _service.CreateAsync(new CreateServiceProfileDto
        {
            Code = "DELME",
            Name = "To Delete",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);

        await _service.DeleteAsync(created.Id, _companyId);

        var list = await _service.ListAsync(_companyId, null);
        list.Should().BeEmpty();
        var get = await _service.GetByIdAsync(created.Id, _companyId);
        get.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_Throws()
    {
        await _service.CreateAsync(new CreateServiceProfileDto
        {
            Code = "DUP",
            Name = "First",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);

        await _service.Invoking(s => s.CreateAsync(new CreateServiceProfileDto
        {
            Code = "DUP",
            Name = "Second",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DUP*");
    }

    [Fact]
    public async Task ListAsync_FiltersByCompany()
    {
        var otherCompanyId = Guid.NewGuid();
        await _service.CreateAsync(new CreateServiceProfileDto
        {
            Code = "MINE",
            Name = "Mine",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);
        await _service.CreateAsync(new CreateServiceProfileDto
        {
            Code = "THEIRS",
            Name = "Theirs",
            IsActive = true,
            DisplayOrder = 0
        }, otherCompanyId);

        var list = await _service.ListAsync(_companyId, null);
        list.Should().HaveCount(1);
        list[0].Code.Should().Be("MINE");
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
