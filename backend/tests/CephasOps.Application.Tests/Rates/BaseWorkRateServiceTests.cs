using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Buildings.Entities;
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
/// Unit tests for BaseWorkRateService (Phase 2 — no impact on payout resolution). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class BaseWorkRateServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BaseWorkRateService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public BaseWorkRateServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<BaseWorkRateService>>();
        _service = new BaseWorkRateService(_context, logger.Object);
    }

    private async Task<RateGroup> CreateRateGroupAsync(string code = "INSTALL")
    {
        SetTenantScope();
        var rg = new RateGroup
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = code,
            Code = code,
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.RateGroups.Add(rg);
        await _context.SaveChangesAsync();
        return rg;
    }

    private async Task<OrderCategory> CreateOrderCategoryAsync()
    {
        SetTenantScope();
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
        await _context.SaveChangesAsync();
        return oc;
    }

    private async Task<ServiceProfile> CreateServiceProfileAsync()
    {
        SetTenantScope();
        var sp = new ServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Residential Fiber",
            Code = "RES_FIBER",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ServiceProfiles.Add(sp);
        await _context.SaveChangesAsync();
        return sp;
    }

    private async Task<InstallationMethod> CreateInstallationMethodAsync()
    {
        SetTenantScope();
        var im = new InstallationMethod
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Prelaid",
            Code = "PRELAID",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.InstallationMethods.Add(im);
        await _context.SaveChangesAsync();
        return im;
    }

    private async Task<OrderType> CreateParentOrderTypeAsync()
    {
        SetTenantScope();
        var ot = new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Assurance",
            Code = "ASSURANCE",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderTypes.Add(ot);
        await _context.SaveChangesAsync();
        return ot;
    }

    private async Task<OrderType> CreateSubtypeAsync(Guid parentId)
    {
        SetTenantScope();
        var ot = new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            ParentOrderTypeId = parentId,
            Name = "Standard",
            Code = "STANDARD",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderTypes.Add(ot);
        await _context.SaveChangesAsync();
        return ot;
    }

    [Fact]
    public async Task ListAsync_WhenEmpty_ReturnsEmptyList()
    {
        SetTenantScope();
        var result = await _service.ListAsync(_companyId, null);
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsDto()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        var dto = new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 100m,
            Currency = "MYR",
            Priority = 0,
            IsActive = true
        };
        var created = await _service.CreateAsync(dto, _companyId);
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.RateGroupId.Should().Be(rg.Id);
        created.Amount.Should().Be(100m);
        created.Currency.Should().Be("MYR");
        created.IsActive.Should().BeTrue();

        var list = await _service.ListAsync(_companyId, null);
        list.Should().ContainSingle().Which.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task CreateAsync_WithOptionalDimensions_Persists()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        var oc = await CreateOrderCategoryAsync();
        var im = await CreateInstallationMethodAsync();
        var parent = await CreateParentOrderTypeAsync();
        var subtype = await CreateSubtypeAsync(parent.Id);

        SetTenantScope();
        var created = await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            OrderCategoryId = oc.Id,
            InstallationMethodId = im.Id,
            OrderSubtypeId = subtype.Id,
            Amount = 150m,
            IsActive = true
        }, _companyId);

        created.OrderCategoryId.Should().Be(oc.Id);
        created.InstallationMethodId.Should().Be(im.Id);
        created.OrderSubtypeId.Should().Be(subtype.Id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAndReturnsDto()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        var created = await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 80m,
            IsActive = true
        }, _companyId);

        var updated = await _service.UpdateAsync(created.Id, new UpdateBaseWorkRateDto
        {
            Amount = 120m,
            IsActive = false
        }, _companyId);

        updated.Amount.Should().Be(120m);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        var created = await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 50m,
            IsActive = true
        }, _companyId);

        await _service.DeleteAsync(created.Id, _companyId);

        var list = await _service.ListAsync(_companyId, null);
        list.Should().BeEmpty();
        var get = await _service.GetByIdAsync(created.Id, _companyId);
        get.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_FilterByRateGroupId_ReturnsMatching()
    {
        SetTenantScope();
        var rg1 = await CreateRateGroupAsync("INSTALL");
        var rg2 = await CreateRateGroupAsync("SERVICE");
        SetTenantScope();
        await _service.CreateAsync(new CreateBaseWorkRateDto { RateGroupId = rg1.Id, Amount = 1m, IsActive = true }, _companyId);
        await _service.CreateAsync(new CreateBaseWorkRateDto { RateGroupId = rg2.Id, Amount = 2m, IsActive = true }, _companyId);

        var list = await _service.ListAsync(_companyId, new BaseWorkRateListFilter { RateGroupId = rg1.Id });
        list.Should().ContainSingle().Which.Amount.Should().Be(1m);
    }

    [Fact]
    public async Task ListAsync_FilterByIsActive_ReturnsMatching()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        await _service.CreateAsync(new CreateBaseWorkRateDto { RateGroupId = rg.Id, Amount = 1m, Priority = 0, IsActive = true }, _companyId);
        var second = await _service.CreateAsync(new CreateBaseWorkRateDto { RateGroupId = rg.Id, Amount = 2m, Priority = 1, IsActive = true }, _companyId);
        await _service.UpdateAsync(second.Id, new UpdateBaseWorkRateDto { IsActive = false }, _companyId);

        var activeList = await _service.ListAsync(_companyId, new BaseWorkRateListFilter { IsActive = true });
        activeList.Should().ContainSingle().Which.Amount.Should().Be(1m);
    }

    [Fact]
    public async Task CreateAsync_InvalidRateGroupId_Throws()
    {
        SetTenantScope();
        var act = () => _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = Guid.NewGuid(),
            Amount = 100m,
            IsActive = true
        }, _companyId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_EffectiveToBeforeEffectiveFrom_Throws()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        var act = () => _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 100m,
            EffectiveFrom = DateTime.UtcNow.AddDays(10),
            EffectiveTo = DateTime.UtcNow.AddDays(5),
            IsActive = true
        }, _companyId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_DuplicateActiveSameSpecificityAndPriority_Throws()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 100m,
            Priority = 0,
            IsActive = true
        }, _companyId);

        SetTenantScope();
        var act = () => _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 200m,
            Priority = 0,
            IsActive = true
        }, _companyId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetByIdAsync_OtherCompany_ReturnsNull()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        var created = await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 100m,
            IsActive = true
        }, _companyId);

        var otherCompany = Guid.NewGuid();
        var get = await _service.GetByIdAsync(created.Id, otherCompany);
        get.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_OtherCompany_ThrowsNotFound()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        var created = await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 100m,
            IsActive = true
        }, _companyId);

        var act = () => _service.UpdateAsync(created.Id, new UpdateBaseWorkRateDto { Amount = 999m }, Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Phase 2 constraint: payout resolution must still be unchanged. BaseWorkRates are not used in live resolution yet.
    /// </summary>
    [Fact]
    public async Task PayoutResolution_Unchanged_WithBaseWorkRatesPresent()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        SetTenantScope();
        await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            Amount = 999m,
            IsActive = true
        }, _companyId);

        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var rateEngine = new RateEngineService(_context, cache, Mock.Of<ILogger<RateEngineService>>());
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        var companyId = _companyId;
        var partnerGroupId = Guid.NewGuid();

        _context.Set<GponSiJobRate>().Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PartnerGroupId = partnerGroupId,
            PayoutAmount = 75m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PartnerGroupId = partnerGroupId
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(75m);
        result.PayoutSource.Should().Be("GponSiJobRate");
    }

    [Fact]
    public async Task CreateAsync_WithBothCategoryAndProfile_Throws()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        var oc = await CreateOrderCategoryAsync();
        var sp = await CreateServiceProfileAsync();

        SetTenantScope();
        var act = () => _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            OrderCategoryId = oc.Id,
            ServiceProfileId = sp.Id,
            Amount = 100m,
            IsActive = true
        }, _companyId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Order Category*Service Profile*not both*");
    }

    [Fact]
    public async Task CreateAsync_WithServiceProfileIdOnly_Persists()
    {
        SetTenantScope();
        var rg = await CreateRateGroupAsync();
        var sp = await CreateServiceProfileAsync();

        SetTenantScope();
        var created = await _service.CreateAsync(new CreateBaseWorkRateDto
        {
            RateGroupId = rg.Id,
            ServiceProfileId = sp.Id,
            Amount = 85m,
            Currency = "MYR",
            IsActive = true
        }, _companyId);

        created.Should().NotBeNull();
        created.ServiceProfileId.Should().Be(sp.Id);
        created.ServiceProfileCode.Should().Be(sp.Code);
        created.OrderCategoryId.Should().BeNull();

        var list = await _service.ListAsync(_companyId, new BaseWorkRateListFilter { ServiceProfileId = sp.Id });
        list.Should().ContainSingle().Which.Amount.Should().Be(85m);
    }

    /// <summary>Call at start of each test so TenantScope is set in the test's execution context (ctor value may not flow).</summary>
    private void SetTenantScope() => TenantScope.CurrentTenantId = _companyId;

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
