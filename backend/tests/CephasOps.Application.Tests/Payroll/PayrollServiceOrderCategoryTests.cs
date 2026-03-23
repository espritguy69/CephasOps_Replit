using CephasOps.Application.Payroll.DTOs;
using CephasOps.Application.Payroll.Services;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Payroll.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Payroll;

/// <summary>
/// Tests that payroll rejects orders without OrderCategoryId (no silent zero payout). Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class PayrollServiceOrderCategoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PayrollService _service;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public PayrollServiceOrderCategoryTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        SeedData();
        var rateEngine = new Mock<IRateEngineService>();
        var kpiProfile = new Mock<IKpiProfileService>();
        _service = new PayrollService(
            _context,
            rateEngine.Object,
            kpiProfile.Object,
            Mock.Of<ILogger<PayrollService>>());
    }

    private void SeedData()
    {
        _context.Companies.Add(new Company
        {
            Id = _companyId,
            ShortName = "Test",
            LegalName = "Test Co",
            IsActive = true
        });
        var periodId = Guid.NewGuid();
        _context.PayrollPeriods.Add(new PayrollPeriod
        {
            Id = periodId,
            CompanyId = _companyId,
            Period = "2025-01",
            Status = "Open",
            PeriodStart = new DateTime(2025, 1, 1),
            PeriodEnd = new DateTime(2025, 1, 31),
            CreatedByUserId = Guid.NewGuid()
        });
        var partnerId = Guid.NewGuid();
        _context.Partners.Add(new Partner
        {
            Id = partnerId,
            CompanyId = _companyId,
            Name = "Partner A",
            PartnerType = "Telco",
            IsActive = true
        });
        var orderTypeId = Guid.NewGuid();
        _context.OrderTypes.Add(new OrderType
        {
            Id = orderTypeId,
            CompanyId = _companyId,
            Name = "Activation",
            Code = "ACT",
            IsActive = true
        });
        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Domain.Buildings.Entities.Building
        {
            Id = buildingId,
            CompanyId = _companyId,
            Name = "B1",
            IsActive = true
        });
        var siId = Guid.NewGuid();
        _context.ServiceInstallers.Add(new ServiceInstaller
        {
            Id = siId,
            CompanyId = _companyId,
            Name = "SI One",
            SiLevel = Domain.ServiceInstallers.Enums.InstallerLevel.Junior,
            IsActive = true
        });
        var orderId = Guid.NewGuid();
        var periodStart = new DateTime(2025, 1, 5);
        var periodEnd = new DateTime(2025, 1, 25);
        _context.Orders.Add(new Order
        {
            Id = orderId,
            CompanyId = _companyId,
            PartnerId = partnerId,
            OrderTypeId = orderTypeId,
            BuildingId = buildingId,
            OrderCategoryId = null,
            InstallationMethodId = null,
            ServiceId = "SVC001",
            Status = "Completed",
            AssignedSiId = siId,
            AppointmentDate = new DateTime(2025, 1, 15),
            PayrollPeriodId = null,
            SourceSystem = "Manual",
            AddressLine1 = "A1",
            City = "KL",
            State = "KL",
            Postcode = "50000"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreatePayrollRun_WhenOrderHasNoOrderCategoryId_ThrowsInvalidOperationException()
    {
        var period = await _context.PayrollPeriods.FirstAsync(p => p.CompanyId == _companyId);
        var dto = new CreatePayrollRunDto
        {
            PayrollPeriodId = period.Id,
            PeriodStart = new DateTime(2025, 1, 5),
            PeriodEnd = new DateTime(2025, 1, 25)
        };

        var act = () => _service.CreatePayrollRunAsync(dto, _companyId, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Order category must be set before payroll calculation*");
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
