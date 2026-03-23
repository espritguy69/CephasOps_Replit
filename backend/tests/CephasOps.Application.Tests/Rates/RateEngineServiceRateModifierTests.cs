using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.Rates.Enums;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Rates;

/// <summary>
/// RateModifier: apply InstallationMethod → SITier → Partner modifiers after base resolution.
/// Backward compatibility: when no modifiers match, payout is unchanged. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class RateEngineServiceRateModifierTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly RateEngineService _rateEngine;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public RateEngineServiceRateModifierTests()
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

    private async Task AddLegacyPayoutAsync(Guid orderTypeId, Guid orderCategoryId, Guid? installationMethodId, string siLevel, decimal amount)
    {
        _context.GponSiJobRates.Add(new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            SiLevel = siLevel,
            PartnerGroupId = null,
            PayoutAmount = amount,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task AddRateModifierAsync(RateModifierType type, Guid? valueId, string? valueString, RateModifierAdjustmentType adjType, decimal adjValue)
    {
        _context.RateModifiers.Add(new RateModifier
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            ModifierType = type,
            ModifierValueId = valueId,
            ModifierValueString = valueString,
            AdjustmentType = adjType,
            AdjustmentValue = adjValue,
            Priority = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// When an InstallationMethod Add modifier matches, base amount is increased.
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenInstallationMethodAddModifierMatches_AppliesAdd()
    {
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        var installationMethodId = Guid.NewGuid();
        await AddLegacyPayoutAsync(orderTypeId, orderCategoryId, installationMethodId, "Junior", 100m);
        await AddRateModifierAsync(RateModifierType.InstallationMethod, installationMethodId, null, RateModifierAdjustmentType.Add, 15m);

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(115m);
        result.PayoutSource.Should().Be("GponSiJobRate");
        result.ResolutionSteps.Should().Contain(s => (s.Contains("Installation Method modifier") || s.Contains("RateModifier (InstallationMethod)")) && s.Contains("115"));
    }

    /// <summary>
    /// When a SITier Multiply modifier matches (e.g. 1.1 for 10%), base amount is multiplied.
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenSITierMultiplyModifierMatches_AppliesMultiply()
    {
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        await AddLegacyPayoutAsync(orderTypeId, orderCategoryId, null, "Senior", 100m);
        await AddRateModifierAsync(RateModifierType.SITier, null, "Senior", RateModifierAdjustmentType.Multiply, 1.1m);

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Senior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(110m);
        result.ResolutionSteps.Should().Contain(s => (s.Contains("SI Tier modifier") || s.Contains("RateModifier (SITier)")) && s.Contains("110"));
    }

    /// <summary>
    /// Multiple modifiers (InstallationMethod then SITier) apply in sequence.
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenMultipleModifiersMatch_AppliesInOrder()
    {
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        var installationMethodId = Guid.NewGuid();
        await AddLegacyPayoutAsync(orderTypeId, orderCategoryId, installationMethodId, "Junior", 100m);
        await AddRateModifierAsync(RateModifierType.InstallationMethod, installationMethodId, null, RateModifierAdjustmentType.Add, 10m);
        await AddRateModifierAsync(RateModifierType.SITier, null, "Junior", RateModifierAdjustmentType.Multiply, 1.2m);

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        // 100 + 10 = 110, then 110 * 1.2 = 132
        result.PayoutAmount.Should().Be(132m);
    }

    /// <summary>
    /// When no modifiers match (e.g. no modifier for this installation method), payout equals legacy base (backward compat).
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenNoModifierMatches_ReturnsUnchangedBaseAmount()
    {
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        await AddLegacyPayoutAsync(orderTypeId, orderCategoryId, null, "Junior", 75m);
        await AddRateModifierAsync(RateModifierType.InstallationMethod, Guid.NewGuid(), null, RateModifierAdjustmentType.Add, 10m);

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(75m);
    }

    /// <summary>
    /// SI Custom override still wins over base + modifiers (custom is checked first).
    /// </summary>
    [Fact]
    public async Task ResolveGponRatesAsync_WhenCustomRateExists_IgnoresModifiers()
    {
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        var siId = Guid.NewGuid();
        await AddLegacyPayoutAsync(orderTypeId, orderCategoryId, null, "Junior", 100m);
        await AddRateModifierAsync(RateModifierType.SITier, null, "Junior", RateModifierAdjustmentType.Add, 50m);
        _context.GponSiCustomRates.Add(new GponSiCustomRate
        {
            Id = Guid.NewGuid(),
            ServiceInstallerId = siId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            CustomPayoutAmount = 90m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _rateEngine.ResolveGponRatesAsync(new GponRateResolutionRequest
        {
            CompanyId = _companyId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            ServiceInstallerId = siId,
            SiLevel = "Junior"
        });

        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(90m);
        result.PayoutSource.Should().Be("GponSiCustomRate");
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _cache?.Dispose();
        _context?.Dispose();
    }
}
