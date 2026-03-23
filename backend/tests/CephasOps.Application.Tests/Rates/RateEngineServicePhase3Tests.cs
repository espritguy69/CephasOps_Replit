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
/// Phase 3: BaseWorkRate resolution order and fallback. Tenant-scoped (no bypass).
/// Resolution order: 1) GponSiCustomRate, 2) BaseWorkRate, 3) GponSiJobRate (legacy).
/// </summary>
[Collection("TenantScopeTests")]
public class RateEngineServicePhase3Tests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly RateEngineService _rateEngine;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public RateEngineServicePhase3Tests()
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

    private async Task<BaseWorkRate> AddBaseWorkRateAsync(Guid rateGroupId, Guid? orderCategoryId, Guid? installationMethodId, Guid? orderSubtypeId, decimal amount)
    {
        var bwr = new BaseWorkRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            RateGroupId = rateGroupId,
            OrderCategoryId = orderCategoryId,
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
        return bwr;
    }

    /// <summary>
    /// When a BaseWorkRate match exists (via Rate Group mapping + dimensions), payout comes from BaseWorkRate.
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenBaseWorkRateMatches_ReturnsBaseWorkRatePayout()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);

        var orderCategoryId = Guid.NewGuid();
        var installationMethodId = Guid.NewGuid();
        await AddBaseWorkRateAsync(installRg.Id, orderCategoryId, installationMethodId, null, 120m);

        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            SiLevel = "Junior",
            PartnerGroupId = null,
            PayoutAmount = 80m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(120m);
        result.PayoutSource.Should().Be("BaseWorkRate");
    }

    /// <summary>
    /// When no BaseWorkRate matches (no mapping or no BWR row), payout falls back to legacy GponSiJobRate.
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenNoBaseWorkRateMatch_FallsBackToLegacyGponSiJobRate()
    {
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior",
            PartnerGroupId = null,
            PayoutAmount = 75m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(75m);
        result.PayoutSource.Should().Be("GponSiJobRate");
    }

    /// <summary>
    /// GponSiCustomRate overrides BaseWorkRate and legacy: when custom exists, payout is from custom.
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenCustomRateExists_CustomOverridesBaseWorkRateAndLegacy()
    {
        var activation = await AddOrderTypeAsync(null, "Activation", "ACTIVATION");
        var installRg = await AddRateGroupAsync("INSTALL");
        await AddMappingAsync(activation.Id, null, installRg.Id);
        var orderCategoryId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        await AddBaseWorkRateAsync(installRg.Id, orderCategoryId, null, null, 100m);
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
        _context.GponSiCustomRates.Add(new GponSiCustomRate
        {
            Id = Guid.NewGuid(),
            ServiceInstallerId = siId,
            OrderTypeId = activation.Id,
            OrderCategoryId = orderCategoryId,
            CustomPayoutAmount = 95m,
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
        result.PayoutAmount.Should().Be(95m);
        result.PayoutSource.Should().Be("GponSiCustomRate");
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _cache?.Dispose();
        _context?.Dispose();
    }
}
