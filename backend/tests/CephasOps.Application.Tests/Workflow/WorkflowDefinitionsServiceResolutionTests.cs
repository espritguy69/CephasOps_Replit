using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Workflow;

/// <summary>
/// Tests for workflow resolution priority: Partner → Department → OrderType → General.
/// See docs/WORKFLOW_RESOLUTION_RULES.md. Tenant-scoped entities require TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class WorkflowDefinitionsServiceResolutionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly WorkflowDefinitionsService _service;
    private readonly Guid _companyId;
    private readonly Guid _partnerA;
    private readonly Guid _deptX;
    private readonly Guid? _previousTenantId;
    private const string OrderEntityType = "Order";

    public WorkflowDefinitionsServiceResolutionTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _partnerA = Guid.NewGuid();
        _deptX = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "WorkflowResolution_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _currentUser = new Mock<ICurrentUserService>();
        _currentUser.Setup(x => x.CompanyId).Returns(_companyId);
        var logger = new Mock<ILogger<WorkflowDefinitionsService>>();
        _service = new WorkflowDefinitionsService(_context, logger.Object, _currentUser.Object);
    }

    private void SetTenantScope() => TenantScope.CurrentTenantId = _companyId;

    [Fact]
    public async Task GetEffective_PartnerSpecific_Overrides_General()
    {
        SetTenantScope();
        await SeedGeneralAndPartnerWorkflowAsync();
        SetTenantScope();
        var effective = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: _partnerA, departmentId: null, orderTypeCode: null);
        effective.Should().NotBeNull();
        effective!.Name.Should().Be("Partner A Order Workflow");
        effective.PartnerId.Should().Be(_partnerA);
    }

    [Fact]
    public async Task GetEffective_DepartmentSpecific_Overrides_OrderType_And_General()
    {
        SetTenantScope();
        await SeedGeneralDepartmentAndOrderTypeWorkflowsAsync();
        SetTenantScope();
        var effective = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: _deptX, orderTypeCode: "MODIFICATION");
        effective.Should().NotBeNull();
        effective!.Name.Should().Be("Dept X Order Workflow");
        effective.DepartmentId.Should().Be(_deptX);
    }

    [Fact]
    public async Task GetEffective_OrderTypeSpecific_Overrides_General()
    {
        SetTenantScope();
        await SeedGeneralAndOrderTypeWorkflowAsync();
        SetTenantScope();
        var effective = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: null, orderTypeCode: "MODIFICATION");
        effective.Should().NotBeNull();
        effective!.Name.Should().Be("Modification Order Workflow");
        effective.OrderTypeCode.Should().Be("MODIFICATION");
    }

    [Fact]
    public async Task GetEffective_General_Fallback_When_No_Specific_Match()
    {
        SetTenantScope();
        await SeedGeneralWorkflowOnlyAsync();
        SetTenantScope();
        var effective = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: _partnerA, departmentId: _deptX, orderTypeCode: "ACTIVATION");
        effective.Should().NotBeNull();
        effective!.Name.Should().Be("General Order Workflow");
        effective.PartnerId.Should().BeNull();
        effective.DepartmentId.Should().BeNull();
        effective.OrderTypeCode.Should().BeNull();
    }

    [Fact]
    public async Task GetEffective_Returns_Null_When_No_Active_Workflow()
    {
        SetTenantScope();
        var effective = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: null, orderTypeCode: null);
        effective.Should().BeNull();
    }

    [Fact]
    public async Task GetEffective_Multiple_Active_General_Throws()
    {
        SetTenantScope();
        await SeedTwoGeneralWorkflowsAsync();
        SetTenantScope();
        var act = () => _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: null, orderTypeCode: null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Multiple active*general*");
    }

    [Fact]
    public async Task GetEffective_Empty_Or_Whitespace_OrderTypeCode_Returns_General()
    {
        SetTenantScope();
        await SeedGeneralAndOrderTypeWorkflowAsync();
        SetTenantScope();
        var effectiveEmpty = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: null, orderTypeCode: "");
        SetTenantScope();
        var effectiveWhitespace = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: null, orderTypeCode: "  ");
        effectiveEmpty.Should().NotBeNull();
        effectiveEmpty!.Name.Should().Be("General Order Workflow");
        effectiveWhitespace.Should().NotBeNull();
        effectiveWhitespace!.Name.Should().Be("General Order Workflow");
    }

    [Fact]
    public async Task GetEffective_Trimmed_OrderTypeCode_Matches_OrderType_Workflow()
    {
        SetTenantScope();
        await SeedGeneralAndOrderTypeWorkflowAsync();
        SetTenantScope();
        var effective = await _service.GetEffectiveWorkflowDefinitionAsync(
            _companyId, OrderEntityType, partnerId: null, departmentId: null, orderTypeCode: "  MODIFICATION  ");
        effective.Should().NotBeNull();
        effective!.Name.Should().Be("Modification Order Workflow");
        effective.OrderTypeCode.Should().Be("MODIFICATION");
    }

    [Fact]
    public async Task CreateWorkflowDefinition_Throws_When_Duplicate_Active_Scope()
    {
        SetTenantScope();
        await SeedGeneralWorkflowOnlyAsync();
        SetTenantScope();
        var createDto = new CephasOps.Application.Workflow.DTOs.CreateWorkflowDefinitionDto
        {
            Name = "Another General",
            EntityType = OrderEntityType,
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null
        };
        var act = () => _service.CreateWorkflowDefinitionAsync(_companyId, createDto, Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*scope*");
    }

    private async Task SeedGeneralAndPartnerWorkflowAsync()
    {
        SetTenantScope();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "General Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "Partner A Order Workflow",
            IsActive = true,
            PartnerId = _partnerA,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedGeneralDepartmentAndOrderTypeWorkflowsAsync()
    {
        SetTenantScope();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "General Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "Modification Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = "MODIFICATION",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "Dept X Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = _deptX,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedGeneralAndOrderTypeWorkflowAsync()
    {
        SetTenantScope();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "General Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "Modification Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = "MODIFICATION",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedGeneralWorkflowOnlyAsync()
    {
        SetTenantScope();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "General Order Workflow",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedTwoGeneralWorkflowsAsync()
    {
        SetTenantScope();
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "General One",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.WorkflowDefinitions.Add(new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            CompanyId = _companyId,
            EntityType = OrderEntityType,
            Name = "General Two",
            IsActive = true,
            PartnerId = null,
            DepartmentId = null,
            OrderTypeCode = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
