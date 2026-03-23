using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Rates;

/// <summary>
/// Service Profile resolution: exact OrderCategoryId beats ServiceProfileId; profile fallback when no category row; legacy unchanged. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class RateEngineServiceServiceProfileResolutionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly RateEngineService _rateEngine;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public RateEngineServiceServiceProfileResolutionTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _rateEngine = new RateEngineService(_context, _cache, Mock.Of<ILogger<RateEngineService>>());
    }

    private async Task<OrderType> AddOrderTypeAsync(Guid? parentId, string name, string code)
    {
        var ot = new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            ParentOrderTypeId = parentId,
            Name = name,
            Code = code,
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderTypes.Add(ot);
        await _context.SaveChangesAsync();
        return ot;
    }

    private async Task<RateGroup> AddRateGroupAsync(string code)
    {
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

    private async Task AddMappingAsync(Guid orderTypeId, Guid? orderSubtypeId, Guid rateGroupId)
    {
        _context.OrderTypeSubtypeRateGroups.Add(new OrderTypeSubtypeRateGroup
        {
            Id = Guid.NewGuid(),
            OrderTypeId = orderTypeId,
            OrderSubtypeId = orderSubtypeId,
            RateGroupId = rateGroupId,
            CompanyId = _companyId
        });
        await _context.SaveChangesAsync();
    }

    private async Task<OrderCategory> AddOrderCategoryAsync(string code)
    {
        var oc = new OrderCategory
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
        _context.OrderCategories.Add(oc);
        await _context.SaveChangesAsync();
        return oc;
    }

    private async Task<ServiceProfile> AddServiceProfileAsync(string code)
    {
        var sp = new ServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Code = code,
            Name = code,
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.ServiceProfiles.Add(sp);
        await _context.SaveChangesAsync();
        return sp;
    }

    private async Task AddCategoryProfileMappingAsync(Guid orderCategoryId, Guid serviceProfileId)
    {
        _context.OrderCategoryServiceProfiles.Add(new OrderCategoryServiceProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderCategoryId = orderCategoryId,
            ServiceProfileId = serviceProfileId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task AddBaseWorkRateAsync(Guid rateGroupId, Guid? orderCategoryId, Guid? serviceProfileId, Guid? installationMethodId, Guid? orderSubtypeId, decimal amount)
    {
        var bwr = new BaseWorkRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            RateGroupId = rateGroupId,
            OrderCategoryId = orderCategoryId,
            ServiceProfileId = serviceProfileId,
            InstallationMethodId = installationMethodId,
            OrderSubtypeId = orderSubtypeId,
            Amount = amount,
            Currency = "MYR",
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.BaseWorkRates.Add(bwr);
        await _context.SaveChangesAsync();
    }

    /// <summary>Exact OrderCategoryId match beats ServiceProfileId match.</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_ExactCategoryBeatsProfileMatch()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = (await AddOrderCategoryAsync("FTTH")).Id;
        var profile = await AddServiceProfileAsync("RESIDENTIAL_FIBER");
        await AddCategoryProfileMappingAsync(orderCategoryId, profile.Id);

        await AddBaseWorkRateAsync(installRg.Id, orderCategoryId, null, null, null, 100m);
        await AddBaseWorkRateAsync(installRg.Id, null, profile.Id, null, null, 90m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PayoutAmount = 70m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(100m);
        result.PayoutSource.Should().Be("BaseWorkRate");
    }

    /// <summary>When no exact category row, profile match is used.</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_ProfileMatchWhenNoExactCategoryRow()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = (await AddOrderCategoryAsync("FTTH")).Id;
        var profile = await AddServiceProfileAsync("RESIDENTIAL_FIBER");
        await AddCategoryProfileMappingAsync(orderCategoryId, profile.Id);

        await AddBaseWorkRateAsync(installRg.Id, null, profile.Id, null, null, 95m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PayoutAmount = 70m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(95m);
        result.PayoutSource.Should().Be("BaseWorkRate");
    }

    /// <summary>No profile mapping: profile-based BWR is not used for that category; falls back to legacy.</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_NoProfileMapping_FallsBackToLegacy()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = (await AddOrderCategoryAsync("FTTO")).Id;
        var profile = await AddServiceProfileAsync("RESIDENTIAL_FIBER");
        await AddBaseWorkRateAsync(installRg.Id, null, profile.Id, null, null, 95m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PayoutAmount = 88m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(88m);
        result.PayoutSource.Should().Be("GponSiJobRate");
    }

    /// <summary>Old setup with only category-based BWR (no ServiceProfile): unchanged behaviour.</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_OldSetupCategoryOnly_UnchangedPayout()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = Guid.NewGuid();
        await AddBaseWorkRateAsync(installRg.Id, orderCategoryId, null, null, null, 110m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Senior",
            PayoutAmount = 75m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Senior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(110m);
        result.PayoutSource.Should().Be("BaseWorkRate");
    }

    /// <summary>Custom SI rate still wins over BaseWorkRate (category or profile).</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_CustomRateStillWinsOverProfileBasedBaseWorkRate()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = (await AddOrderCategoryAsync("FTTH")).Id;
        var profile = await AddServiceProfileAsync("RESIDENTIAL_FIBER");
        await AddCategoryProfileMappingAsync(orderCategoryId, profile.Id);
        var siId = Guid.NewGuid();

        await AddBaseWorkRateAsync(installRg.Id, null, profile.Id, null, null, 90m);
        _context.GponSiCustomRates.Add(new GponSiCustomRate
        {
            Id = Guid.NewGuid(),
            ServiceInstallerId = siId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            CustomPayoutAmount = 85m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PayoutAmount = 70m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            ServiceInstallerId = siId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(85m);
        result.PayoutSource.Should().Be("GponSiCustomRate");
    }

    /// <summary>ResolutionMatchLevel is ServiceProfile when BWR matched by profile (no exact category row).</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_ProfileMatch_SetsResolutionMatchLevelServiceProfile()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = (await AddOrderCategoryAsync("FTTH")).Id;
        var profile = await AddServiceProfileAsync("RESIDENTIAL_FIBER");
        await AddCategoryProfileMappingAsync(orderCategoryId, profile.Id);
        await AddBaseWorkRateAsync(installRg.Id, null, profile.Id, null, null, 92m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PayoutAmount = 70m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.ResolutionMatchLevel.Should().Be("ServiceProfile");
        result.MatchedRuleDetails.Should().NotBeNull();
        result.MatchedRuleDetails!.ServiceProfileId.Should().Be(profile.Id);
    }

    /// <summary>Same request twice returns same result (cache correctness); cache key includes ServiceProfileId.</summary>
    [Fact]
    public async Task ResolveGponRatesAsync_SameContextTwice_ConsistentCachedResult()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = (await AddOrderCategoryAsync("FTTH")).Id;
        var profile = await AddServiceProfileAsync("RESIDENTIAL_FIBER");
        await AddCategoryProfileMappingAsync(orderCategoryId, profile.Id);
        await AddBaseWorkRateAsync(installRg.Id, null, profile.Id, null, null, 99m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PayoutAmount = 70m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var request = new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        };

        var result1 = await _rateEngine.ResolveGponRatesAsync(request);
        var result2 = await _rateEngine.ResolveGponRatesAsync(request);

        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result1.PayoutAmount.Should().Be(99m);
        result2.PayoutAmount.Should().Be(result1.PayoutAmount);
        result2.PayoutSource.Should().Be(result1.PayoutSource);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _cache?.Dispose();
        _context?.Dispose();
    }
}
