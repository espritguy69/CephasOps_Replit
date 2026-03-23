using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Common;

/// <summary>
/// Tests for OrderPricingContextResolver: direct order type, subtype to parent code, optional fields, null/missing. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class OrderPricingContextResolverTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderPricingContextResolver _resolver;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public OrderPricingContextResolverTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _resolver = new OrderPricingContextResolver(_context);
    }

    [Fact]
    public async Task ResolveFromOrderAsync_OrderNotFound_ReturnsNull()
    {
        var result = await _resolver.ResolveFromOrderAsync(Guid.NewGuid(), _companyId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveFromOrderAsync_OrderInOtherCompany_ReturnsNull()
    {
        var (_, orderId) = await SeedOrderAsync(partnerGroupId: null, departmentId: null, orderCategoryId: null, installationMethodId: null);
        var otherCompanyId = Guid.NewGuid();
        var result = await _resolver.ResolveFromOrderAsync(orderId, otherCompanyId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveFromOrderAsync_DirectOrderType_ReturnsOwnCodeAsScopeAndNullParentCode()
    {
        var parentOrderTypeId = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();
        var companyId = _companyId;
        var orderId = await SeedOrderWithOrderTypeAsync(companyId, orderTypeId, parentOrderTypeId: null, orderTypeCode: "ACTIVATION", parentCode: null);

        var result = await _resolver.ResolveFromOrderAsync(orderId, companyId);

        result.Should().NotBeNull();
        result!.OrderTypeCode.Should().Be("ACTIVATION");
        result.ParentOrderTypeCode.Should().BeNull();
        result.OrderTypeId.Should().Be(orderTypeId);
    }

    [Fact]
    public async Task ResolveFromOrderAsync_SubtypeOrderType_ReturnsParentCodeForScopeAndParentCodeSet()
    {
        var parentOrderTypeId = Guid.NewGuid();
        var subtypeOrderTypeId = Guid.NewGuid();
        var companyId = _companyId;
        var orderId = await SeedOrderWithOrderTypeAsync(companyId, subtypeOrderTypeId, parentOrderTypeId, orderTypeCode: "MODIFICATION_OUTDOOR", parentCode: "MODIFICATION");

        var result = await _resolver.ResolveFromOrderAsync(orderId, companyId);

        result.Should().NotBeNull();
        result!.OrderTypeCode.Should().Be("MODIFICATION");
        result.ParentOrderTypeCode.Should().Be("MODIFICATION");
        result.OrderTypeId.Should().Be(subtypeOrderTypeId);
    }

    [Fact]
    public async Task ResolveFromOrderAsync_OrderCategoryAndInstallationMethodPresent_PopulatesContext()
    {
        var orderCategoryId = Guid.NewGuid();
        var installationMethodId = Guid.NewGuid();
        var (_, orderId) = await SeedOrderAsync(partnerGroupId: null, departmentId: null, orderCategoryId, installationMethodId);

        var result = await _resolver.ResolveFromOrderAsync(orderId, _companyId);

        result.Should().NotBeNull();
        result!.OrderCategoryId.Should().Be(orderCategoryId);
        result.InstallationMethodId.Should().Be(installationMethodId);
    }

    [Fact]
    public async Task ResolveFromOrderAsync_NullOptionalValues_ReturnsContextWithNulls()
    {
        var (_, orderId) = await SeedOrderAsync(partnerGroupId: null, departmentId: null, orderCategoryId: null, installationMethodId: null);

        var result = await _resolver.ResolveFromOrderAsync(orderId, _companyId);

        result.Should().NotBeNull();
        result!.DepartmentId.Should().BeNull();
        result.OrderCategoryId.Should().BeNull();
        result.InstallationMethodId.Should().BeNull();
        result.PartnerGroupId.Should().BeNull();
    }

    [Fact]
    public async Task ResolveFromOrderAsync_PartnerWithGroupId_PopulatesPartnerGroupId()
    {
        var partnerGroupId = Guid.NewGuid();
        var (_, orderId) = await SeedOrderAsync(partnerGroupId, departmentId: null, orderCategoryId: null, installationMethodId: null);

        var result = await _resolver.ResolveFromOrderAsync(orderId, _companyId);

        result.Should().NotBeNull();
        result!.PartnerGroupId.Should().Be(partnerGroupId);
    }

    [Fact]
    public async Task ResolveFromOrderAsync_OrderTypeMissing_ReturnsContextWithNullOrderTypeCodes()
    {
        var (_, orderId) = await SeedOrderAsync(partnerGroupId: null, departmentId: null, orderCategoryId: null, installationMethodId: null);
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        order.Should().NotBeNull();
        var missingOrderTypeId = Guid.NewGuid();
        order!.OrderTypeId = missingOrderTypeId;
        await _context.SaveChangesAsync();

        var result = await _resolver.ResolveFromOrderAsync(orderId, _companyId);

        result.Should().NotBeNull();
        result!.OrderTypeId.Should().Be(missingOrderTypeId);
        result.OrderTypeCode.Should().BeNull();
        result.ParentOrderTypeCode.Should().BeNull();
    }

    private async Task<(Guid OrderId, Guid OrderIdOut)> SeedOrderAsync(Guid? partnerGroupId, Guid? departmentId, Guid? orderCategoryId, Guid? installationMethodId)
    {
        var companyId = _companyId;
        var partnerId = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();

        _context.Companies.Add(new Company { Id = companyId, LegalName = "Test Co", ShortName = "Test" });
        if (partnerGroupId.HasValue)
            _context.PartnerGroups.Add(new PartnerGroup { Id = partnerGroupId.Value, CompanyId = companyId, Name = "PG" });
        _context.Partners.Add(new Partner
        {
            Id = partnerId,
            CompanyId = companyId,
            Name = "P",
            GroupId = partnerGroupId,
            PartnerType = "Telco",
            IsActive = true
        });
        _context.OrderTypes.Add(new OrderType
        {
            Id = orderTypeId,
            CompanyId = companyId,
            Name = "Activation",
            Code = "ACTIVATION",
            IsActive = true
        });
        await _context.SaveChangesAsync();

        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Building { Id = buildingId, CompanyId = companyId, Name = "B", AddressLine1 = "A", City = "C", State = "S", Postcode = "P" });
        var orderId = Guid.NewGuid();
        _context.Orders.Add(new Order
        {
            Id = orderId,
            CompanyId = companyId,
            PartnerId = partnerId,
            BuildingId = buildingId,
            DepartmentId = departmentId,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            InstallationMethodId = installationMethodId,
            SourceSystem = "Test",
            Status = "Pending",
            AddressLine1 = "A",
            City = "C",
            State = "S",
            Postcode = "P"
        });
        await _context.SaveChangesAsync();
        return (orderId, orderId);
    }

    private async Task<Guid> SeedOrderWithOrderTypeAsync(Guid companyId, Guid orderTypeId, Guid? parentOrderTypeId, string orderTypeCode, string? parentCode)
    {
        _context.Companies.Add(new Company { Id = companyId, LegalName = "Test Co", ShortName = "Test" });
        if (parentOrderTypeId.HasValue && parentCode != null)
        {
            _context.OrderTypes.Add(new OrderType
            {
                Id = parentOrderTypeId.Value,
                CompanyId = companyId,
                Name = parentCode,
                Code = parentCode,
                IsActive = true
            });
        }
        _context.OrderTypes.Add(new OrderType
        {
            Id = orderTypeId,
            CompanyId = companyId,
            ParentOrderTypeId = parentOrderTypeId,
            Name = orderTypeCode,
            Code = orderTypeCode,
            IsActive = true
        });
        var partnerId = Guid.NewGuid();
        _context.Partners.Add(new Partner { Id = partnerId, CompanyId = companyId, Name = "P", PartnerType = "Telco", IsActive = true });
        await _context.SaveChangesAsync();

        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Building { Id = buildingId, CompanyId = companyId, Name = "B", AddressLine1 = "A", City = "C", State = "S", Postcode = "P" });
        var orderId = Guid.NewGuid();
        _context.Orders.Add(new Order
        {
            Id = orderId,
            CompanyId = companyId,
            PartnerId = partnerId,
            BuildingId = buildingId,
            OrderTypeId = orderTypeId,
            SourceSystem = "Test",
            Status = "Pending",
            AddressLine1 = "A",
            City = "C",
            State = "S",
            Postcode = "P"
        });
        await _context.SaveChangesAsync();
        return orderId;
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
