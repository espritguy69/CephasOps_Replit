using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Orders;

/// <summary>
/// Tests for OrderTypeService parent/subtype hierarchy: parent list returns only top-level,
/// subtype list returns only direct children, duplicate parent/subtype creation blocked.
/// Tenant-scoped entities require TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class OrderTypeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrderTypeService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public OrderTypeServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<OrderTypeService>>().Object;
        _service = new OrderTypeService(_context, logger);
    }

    [Fact]
    public async Task GetOrderTypesAsync_ParentsOnly_ReturnsOnlyParentOrderTypeIdNull()
    {
        var parentId = Guid.NewGuid();
        var subtypeId = Guid.NewGuid();
        await SeedOrderType(_context, _companyId, parentId, null, "MODIFICATION", "Modification", 1);
        await SeedOrderType(_context, _companyId, subtypeId, parentId, "INDOOR", "Indoor", 1);

        var result = await _service.GetOrderTypesAsync(_companyId, departmentId: null, isActive: null, parentsOnly: true);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(parentId);
        result[0].ParentOrderTypeId.Should().BeNull();
        result[0].Code.Should().Be("MODIFICATION");
    }

    [Fact]
    public async Task GetOrderTypesAsync_ParentsOnly_DedupesByCompanyIdAndCode()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        await SeedOrderType(_context, _companyId, id1, null, "ACTIVATION", "Activation", 1);
        await SeedOrderType(_context, _companyId, id2, null, "ACTIVATION", "Activation", 2);

        var result = await _service.GetOrderTypesAsync(_companyId, departmentId: null, isActive: null, parentsOnly: true);

        result.Should().HaveCount(1);
        result[0].Code.Should().Be("ACTIVATION");
    }

    [Fact]
    public async Task GetSubtypesAsync_ReturnsOnlyDirectChildrenOfParent()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        await SeedOrderType(_context, _companyId, parentId, null, "ASSURANCE", "Assurance", 1);
        await SeedOrderType(_context, _companyId, childId, parentId, "STANDARD", "Standard", 1);

        var result = await _service.GetSubtypesAsync(parentId, _companyId);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(childId);
        result[0].ParentOrderTypeId.Should().Be(parentId);
        result[0].Code.Should().Be("STANDARD");
    }

    [Fact]
    public async Task GetSubtypesAsync_WhenParentIdIsSubtype_ReturnsEmpty()
    {
        var parentId = Guid.NewGuid();
        var subtypeId = Guid.NewGuid();
        await SeedOrderType(_context, _companyId, parentId, null, "MODIFICATION", "Modification", 1);
        await SeedOrderType(_context, _companyId, subtypeId, parentId, "INDOOR", "Indoor", 1);

        var result = await _service.GetSubtypesAsync(subtypeId, _companyId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateOrderTypeAsync_DuplicateParentCode_Throws()
    {
        await _service.CreateOrderTypeAsync(new CreateOrderTypeDto
        {
            Name = "Activation",
            Code = "ACTIVATION",
            ParentOrderTypeId = null,
            DisplayOrder = 1,
            IsActive = true
        }, _companyId);

        var act = () => _service.CreateOrderTypeAsync(new CreateOrderTypeDto
        {
            Name = "Activation 2",
            Code = "ACTIVATION",
            ParentOrderTypeId = null,
            DisplayOrder = 2,
            IsActive = true
        }, _companyId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*parent order type with code \"ACTIVATION\" already exists*");
    }

    [Fact]
    public async Task CreateOrderTypeAsync_DuplicateSubtypeCodeUnderSameParent_Throws()
    {
        var parentId = Guid.NewGuid();
        await SeedOrderType(_context, _companyId, parentId, null, "MODIFICATION", "Modification", 1);
        await _service.CreateOrderTypeAsync(new CreateOrderTypeDto
        {
            Name = "Indoor",
            Code = "INDOOR",
            ParentOrderTypeId = parentId,
            DisplayOrder = 1,
            IsActive = true
        }, _companyId);

        var act = () => _service.CreateOrderTypeAsync(new CreateOrderTypeDto
        {
            Name = "Indoor Another",
            Code = "INDOOR",
            ParentOrderTypeId = parentId,
            DisplayOrder = 2,
            IsActive = true
        }, _companyId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*subtype with code \"INDOOR\" already exists under this parent*");
    }

    [Fact]
    public async Task CreateOrderTypeAsync_SubtypeWithInvalidParent_Throws()
    {
        var act = () => _service.CreateOrderTypeAsync(new CreateOrderTypeDto
        {
            Name = "Indoor",
            Code = "INDOOR",
            ParentOrderTypeId = Guid.NewGuid(),
            DisplayOrder = 1,
            IsActive = true
        }, _companyId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Parent order type not found or is not a parent*");
    }

    private static async Task SeedOrderType(ApplicationDbContext context, Guid companyId, Guid id, Guid? parentId, string code, string name, int displayOrder)
    {
        var entity = new OrderType
        {
            Id = id,
            CompanyId = companyId,
            ParentOrderTypeId = parentId,
            Code = code,
            Name = name,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Set<OrderType>().Add(entity);
        await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
