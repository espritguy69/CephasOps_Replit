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
/// Unit tests for RateGroupService (Phase 1 — no impact on payout resolution). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class RateGroupServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RateGroupService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public RateGroupServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<RateGroupService>>();
        _service = new RateGroupService(_context, logger.Object);
    }

    [Fact]
    public async Task ListRateGroupsAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _service.ListRateGroupsAsync(_companyId, null);
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task CreateRateGroupAsync_PersistsAndReturnsDto()
    {
        var dto = new CreateRateGroupDto
        {
            Name = "Installation",
            Code = "INSTALL",
            Description = "Install work",
            IsActive = true,
            DisplayOrder = 1
        };
        var created = await _service.CreateRateGroupAsync(dto, _companyId);
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("Installation");
        created.Code.Should().Be("INSTALL");
        created.IsActive.Should().BeTrue();

        var list = await _service.ListRateGroupsAsync(_companyId, null);
        list.Should().ContainSingle().Which.Code.Should().Be("INSTALL");
    }

    [Fact]
    public async Task UpdateRateGroupAsync_UpdatesAndReturnsDto()
    {
        var created = await _service.CreateRateGroupAsync(new CreateRateGroupDto
        {
            Name = "Install",
            Code = "INSTALL",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);

        var updated = await _service.UpdateRateGroupAsync(created.Id, new UpdateRateGroupDto
        {
            Name = "Installation Work",
            IsActive = false
        }, _companyId);

        updated.Name.Should().Be("Installation Work");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRateGroupAsync_WhenNoMappings_RemovesEntity()
    {
        var created = await _service.CreateRateGroupAsync(new CreateRateGroupDto
        {
            Name = "Temp",
            Code = "TEMP",
            IsActive = true,
            DisplayOrder = 0
        }, _companyId);

        await _service.DeleteRateGroupAsync(created.Id, _companyId);

        var list = await _service.ListRateGroupsAsync(_companyId, null);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignRateGroupToOrderTypeSubtypeAsync_CreatesMapping()
    {
        var orderType = new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Activation",
            Code = "ACTIVATION",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderTypes.Add(orderType);

        var rateGroup = new RateGroup
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "INSTALL",
            Code = "INSTALL",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.RateGroups.Add(rateGroup);
        await _context.SaveChangesAsync();

        var mapping = await _service.AssignRateGroupToOrderTypeSubtypeAsync(new AssignRateGroupToOrderTypeSubtypeDto
        {
            OrderTypeId = orderType.Id,
            OrderSubtypeId = null,
            RateGroupId = rateGroup.Id
        }, _companyId);

        mapping.Should().NotBeNull();
        mapping.OrderTypeId.Should().Be(orderType.Id);
        mapping.RateGroupId.Should().Be(rateGroup.Id);
        mapping.OrderSubtypeId.Should().BeNull();

        var mappings = await _service.ListMappingsAsync(_companyId, null, null);
        mappings.Should().ContainSingle();
    }

    [Fact]
    public async Task UnassignRateGroupMappingAsync_RemovesMapping()
    {
        var orderType = new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "Activation",
            Code = "ACTIVATION",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.OrderTypes.Add(orderType);
        var rateGroup = new RateGroup
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            Name = "INSTALL",
            Code = "INSTALL",
            IsActive = true,
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.RateGroups.Add(rateGroup);
        await _context.SaveChangesAsync();

        var mapping = await _service.AssignRateGroupToOrderTypeSubtypeAsync(new AssignRateGroupToOrderTypeSubtypeDto
        {
            OrderTypeId = orderType.Id,
            RateGroupId = rateGroup.Id
        }, _companyId);

        await _service.UnassignRateGroupMappingAsync(mapping.Id, _companyId);

        var mappings = await _service.ListMappingsAsync(_companyId, null, null);
        mappings.Should().BeEmpty();
    }

    /// <summary>
    /// Phase 1 constraint: rate resolution must be unchanged. This test asserts that
    /// ResolveGponRatesAsync still returns payout from GponSiJobRate when no custom rate exists.
    /// </summary>
    [Fact]
    public async Task RateEngineService_PayoutResolution_Unchanged_ByPhase1()
    {
        var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var rateEngine = new RateEngineService(_context, cache, Mock.Of<ILogger<RateEngineService>>());
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
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

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
