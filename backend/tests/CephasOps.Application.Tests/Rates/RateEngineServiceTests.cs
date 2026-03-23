using CephasOps.Application.Rates.DTOs;
using CephasOps.Application.Rates.Services;
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
/// Unit tests for RateEngineService rate resolution logic. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class RateEngineServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<RateEngineService>> _mockLogger;
    private readonly RateEngineService _service;
    private readonly Guid _companyId;
    private readonly Guid _orderTypeId;
    private readonly Guid _orderCategoryId;
    private readonly Guid _partnerGroupId;
    private readonly Guid? _previousTenantId;

    public RateEngineServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _orderTypeId = Guid.NewGuid();
        _orderCategoryId = Guid.NewGuid();
        _partnerGroupId = Guid.NewGuid(); // Not nullable in entity
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<RateEngineService>>();
        _service = new RateEngineService(_dbContext, _cache, _mockLogger.Object);
    }

    #region Revenue Rate Resolution Tests

    [Fact]
    public async Task ResolveGponRatesAsync_WithPartnerId_ReturnsPartnerSpecificRate()
    {
        // Arrange
        var partnerId = Guid.NewGuid();
        var partnerRate = await CreateTestGponPartnerJobRateAsync(partnerId: partnerId, revenueAmount: 100m);
        await CreateTestGponPartnerJobRateAsync(partnerId: null, revenueAmount: 50m); // Default rate

        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            PartnerId = partnerId,
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RevenueAmount.Should().Be(100m);
        result.RevenueSource.Should().Be("GponPartnerJobRate");
        result.ResolutionSteps.Should().Contain(s => s.Contains("Revenue matched"));
    }

    [Fact]
    public async Task ResolveGponRatesAsync_NoPartnerId_FallsBackToDefaultRate()
    {
        // Arrange
        var defaultRate = await CreateTestGponPartnerJobRateAsync(partnerId: null, revenueAmount: 50m);

        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            PartnerId = null,
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RevenueAmount.Should().Be(50m);
    }

    [Fact]
    public async Task ResolveGponRatesAsync_NoRateFound_ReturnsNoRevenue()
    {
        // Arrange
        var request = new GponRateResolutionRequest
        {
            OrderTypeId = Guid.NewGuid(), // Non-existent
            OrderCategoryId = Guid.NewGuid(),
            PartnerId = null,
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RevenueAmount.Should().BeNull();
        result.ResolutionSteps.Should().Contain(s => s.Contains("no matching partner rate found"));
    }

    [Fact]
    public async Task ResolveGponRatesAsync_WithOrderCategoryId_ResolvesCorrectRate()
    {
        // Rate calculator and payroll use OrderCategoryId (not installationTypeId) for resolution.
        var partnerId = Guid.NewGuid();
        await CreateTestGponPartnerJobRateAsync(partnerId: partnerId, revenueAmount: 75m);
        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            PartnerId = partnerId,
            PartnerGroupId = _partnerGroupId
        };

        var result = await _service.ResolveGponRatesAsync(request);

        result.Success.Should().BeTrue();
        result.RevenueAmount.Should().Be(75m);
    }

    #endregion

    #region Payout Rate Resolution Tests

    [Fact]
    public async Task ResolveGponRatesAsync_WithCustomRate_ReturnsCustomPayout()
    {
        // Arrange
        var siId = Guid.NewGuid();
        var customRate = await CreateTestGponSiCustomRateAsync(siId, customPayoutAmount: 80m);
        await CreateTestGponSiJobRateAsync("Senior", payoutAmount: 60m); // Default rate

        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            ServiceInstallerId = siId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(80m);
        result.PayoutSource.Should().Be("GponSiCustomRate");
        result.ResolutionSteps.Should().Contain(s => s.Contains("Custom override payout") || s.Contains("Checked Custom SI rate → matched"));
    }

    [Fact]
    public async Task ResolveGponRatesAsync_NoCustomRate_FallsBackToDefaultPayout()
    {
        // Arrange
        var siId = Guid.NewGuid();
        var defaultRate = await CreateTestGponSiJobRateAsync("Senior", payoutAmount: 60m);

        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            ServiceInstallerId = siId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().Be(60m);
        result.PayoutSource.Should().Be("GponSiJobRate");
        result.ResolutionSteps.Should().Contain(s => s.Contains("Fallback: matched Legacy SI rate") || s.Contains("GponSiJobRate"));
    }

    [Fact]
    public async Task ResolveGponRatesAsync_NoSiLevel_ReturnsNoPayout()
    {
        // Arrange
        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            ServiceInstallerId = null,
            SiLevel = null, // No SI level
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.PayoutAmount.Should().BeNull();
        result.ResolutionSteps.Should().Contain(s => s.Contains("No SI Level provided") || s.Contains("legacy lookup skipped"));
    }

    #endregion

    #region Rate Priority Tests

    [Fact]
    public async Task ResolveGponRatesAsync_CustomRateTakesPriorityOverDefault()
    {
        // Arrange
        var siId = Guid.NewGuid();
        var customRate = await CreateTestGponSiCustomRateAsync(siId, customPayoutAmount: 100m);
        var defaultRate = await CreateTestGponSiJobRateAsync("Senior", payoutAmount: 50m);

        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            ServiceInstallerId = siId,
            SiLevel = "Senior",
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.PayoutAmount.Should().Be(100m); // Custom rate, not default
        result.PayoutSource.Should().Be("GponSiCustomRate");
    }

    [Fact]
    public async Task ResolveGponRatesAsync_PartnerSpecificTakesPriorityOverDefault()
    {
        // Arrange
        var partnerId = Guid.NewGuid();
        var partnerRate = await CreateTestGponPartnerJobRateAsync(partnerId: partnerId, revenueAmount: 150m);
        var defaultRate = await CreateTestGponPartnerJobRateAsync(partnerId: null, revenueAmount: 100m);

        var request = new GponRateResolutionRequest
        {
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            PartnerId = partnerId,
            PartnerGroupId = _partnerGroupId
        };

        // Act
        var result = await _service.ResolveGponRatesAsync(request);

        // Assert
        result.RevenueAmount.Should().Be(150m); // Partner-specific rate
    }

    #endregion

    #region GetGponRevenueRate Tests

    [Fact]
    public async Task GetGponRevenueRateAsync_ValidRequest_ReturnsRate()
    {
        // Arrange
        var rate = await CreateTestGponPartnerJobRateAsync(revenueAmount: 100m);

        // Act
        var result = await _service.GetGponRevenueRateAsync(
            _orderTypeId,
            _orderCategoryId,
            null,
            _partnerGroupId);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public async Task GetGponRevenueRateAsync_NoRateFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetGponRevenueRateAsync(
            Guid.NewGuid(), // Non-existent
            Guid.NewGuid(),
            null,
            _partnerGroupId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetGponPayoutRate Tests

    [Fact]
    public async Task GetGponPayoutRateAsync_WithCustomRate_ReturnsCustomRate()
    {
        // Arrange
        var siId = Guid.NewGuid();
        await CreateTestGponSiCustomRateAsync(siId, customPayoutAmount: 80m);

        // Act
        var result = await _service.GetGponPayoutRateAsync(
            _orderTypeId,
            _orderCategoryId,
            null,
            siId,
            "Senior",
            _partnerGroupId);

        // Assert
        result.Should().Be(80m);
    }

    [Fact]
    public async Task GetGponPayoutRateAsync_NoCustomRate_ReturnsDefaultRate()
    {
        // Arrange
        await CreateTestGponSiJobRateAsync("Senior", payoutAmount: 60m);

        // Act
        var result = await _service.GetGponPayoutRateAsync(
            _orderTypeId,
            _orderCategoryId,
            null,
            null, // No SI ID
            "Senior",
            _partnerGroupId);

        // Assert
        result.Should().Be(60m);
    }

    [Fact]
    public async Task GetGponPayoutRateAsync_NoRateFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetGponPayoutRateAsync(
            Guid.NewGuid(), // Non-existent
            Guid.NewGuid(),
            null,
            null,
            "Senior",
            _partnerGroupId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private async Task<GponPartnerJobRate> CreateTestGponPartnerJobRateAsync(
        Guid? partnerId = null,
        decimal revenueAmount = 100m)
    {
        var rate = new GponPartnerJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            PartnerId = partnerId,
            PartnerGroupId = _partnerGroupId,
            RevenueAmount = revenueAmount,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(365),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Set<GponPartnerJobRate>().Add(rate);
        await _dbContext.SaveChangesAsync();
        return rate;
    }

    private async Task<GponSiJobRate> CreateTestGponSiJobRateAsync(
        string siLevel,
        decimal payoutAmount = 60m)
    {
        var rate = new GponSiJobRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            SiLevel = siLevel,
            PartnerGroupId = _partnerGroupId,
            PayoutAmount = payoutAmount,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(365),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Set<GponSiJobRate>().Add(rate);
        await _dbContext.SaveChangesAsync();
        return rate;
    }

    private async Task<GponSiCustomRate> CreateTestGponSiCustomRateAsync(
        Guid serviceInstallerId,
        decimal customPayoutAmount = 80m)
    {
        var rate = new GponSiCustomRate
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            ServiceInstallerId = serviceInstallerId,
            OrderTypeId = _orderTypeId,
            OrderCategoryId = _orderCategoryId,
            PartnerGroupId = _partnerGroupId,
            CustomPayoutAmount = customPayoutAmount,
            IsActive = true,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(365),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Set<GponSiCustomRate>().Add(rate);
        await _dbContext.SaveChangesAsync();
        return rate;
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}

