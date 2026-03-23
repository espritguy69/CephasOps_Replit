using CephasOps.Application.SIApp.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// SI-app workflow tenant isolation: same-tenant allowed, other-tenant denied/not found, missing tenant throws.
/// </summary>
[Collection("TenantScopeTests")]
public class SiAppTenantIsolationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SiAppMaterialService _service;
    private readonly Guid _companyA;
    private readonly Guid _companyB;
    private readonly Guid _siId;
    private readonly Guid? _previousTenantId;

    public SiAppTenantIsolationTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
        _siId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "SiAppTenant_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new SiAppMaterialService(
            _context,
            Mock.Of<CephasOps.Application.RMA.Services.IRMAService>(),
            Mock.Of<CephasOps.Application.Inventory.Services.IStockLedgerService>(),
            Mock.Of<ILogger<SiAppMaterialService>>());
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task MarkDeviceAsFaultyAsync_WhenCompanyIdEmpty_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var act = () => _service.MarkDeviceAsFaultyAsync(
            Guid.NewGuid(),
            "SN1",
            "Faulty",
            Guid.Empty,
            _siId,
            null,
            null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task MarkDeviceAsFaultyAsync_WhenCompanyIdNull_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var act = () => _service.MarkDeviceAsFaultyAsync(
            Guid.NewGuid(),
            "SN1",
            "Faulty",
            null,
            _siId,
            null,
            null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task GetMaterialReturnsAsync_WhenCompanyIdNull_Throws()
    {
        TenantScope.CurrentTenantId = _companyA;
        var act = () => _service.GetMaterialReturnsAsync(_siId, null);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant context missing*CompanyId required*");
    }

    [Fact]
    public async Task MarkDeviceAsFaultyAsync_WhenOrderInSameTenant_DoesNotThrowTenantOrUnauthorized()
    {
        var orderId = Guid.NewGuid();
        TenantScope.CurrentTenantId = _companyA;
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            _context.Orders.Add(new Order
            {
                Id = orderId,
                CompanyId = _companyA,
                AssignedSiId = _siId,
                PartnerId = Guid.NewGuid(),
                OrderTypeId = Guid.NewGuid(),
                SourceSystem = "Manual",
                Status = "Assigned",
                AddressLine1 = "A",
                City = "C",
                State = "S",
                Postcode = "P",
                BuildingId = Guid.NewGuid(),
                ServiceId = "S1",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }

        TenantScope.CurrentTenantId = _companyA;
        var act = () => _service.MarkDeviceAsFaultyAsync(
            orderId,
            "NonExistentSerial",
            "Faulty",
            _companyA,
            _siId,
            null,
            null);
        var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
        ex.Which.Message.Should().Contain("not found", "same-tenant path should reach order/serial validation, not tenant or unauthorized");
    }

    [Fact]
    public async Task MarkDeviceAsFaultyAsync_WhenOrderFromOtherTenant_NotFound()
    {
        var orderIdInCompanyB = Guid.NewGuid();
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            _context.Orders.Add(new Order
            {
                Id = orderIdInCompanyB,
                CompanyId = _companyB,
                AssignedSiId = _siId,
                PartnerId = Guid.NewGuid(),
                OrderTypeId = Guid.NewGuid(),
                SourceSystem = "Manual",
                Status = "Assigned",
                IsDeleted = false
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }

        TenantScope.CurrentTenantId = _companyA;
        var act = () => _service.MarkDeviceAsFaultyAsync(
            orderIdInCompanyB,
            "SN1",
            "Faulty",
            _companyA,
            _siId,
            null,
            null);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Order*not found*");
    }
}
