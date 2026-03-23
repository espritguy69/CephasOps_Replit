using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Commands;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Billing;

/// <summary>
/// Financial isolation: CreateInvoice and BuildInvoiceLines require company and reject mismatched order company. Tenant-scoped (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class BillingServiceFinancialIsolationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BillingService _service;
    private readonly Guid _companyA;
    private readonly Guid _companyB;
    private readonly Guid _orderInCompanyA;
    private readonly Guid _partnerIdA;
    private readonly Guid? _previousTenantId;

    public BillingServiceFinancialIsolationTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
        _orderInCompanyA = Guid.NewGuid();
        _partnerIdA = Guid.NewGuid();
        TenantScope.CurrentTenantId = _companyA;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        SeedData();
        _service = new BillingService(
            _context,
            Mock.Of<ILogger<BillingService>>(),
            new CommandProcessingLogStore(_context, Mock.Of<ILogger<CommandProcessingLogStore>>()));
    }

    private void SeedData()
    {
        var groupId = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();
        var orderCategoryId = Guid.NewGuid();
        _context.Companies.Add(new Company { Id = _companyA, ShortName = "A", LegalName = "Co A", IsActive = true });
        _context.Companies.Add(new Company { Id = _companyB, ShortName = "B", LegalName = "Co B", IsActive = true });
        _context.PartnerGroups.Add(new PartnerGroup { Id = groupId, CompanyId = _companyA, Name = "PG" });
        _context.Partners.Add(new Partner
        {
            Id = _partnerIdA,
            CompanyId = _companyA,
            GroupId = groupId,
            Name = "P",
            PartnerType = "Telco",
            IsActive = true
        });
        _context.OrderTypes.Add(new OrderType { Id = orderTypeId, CompanyId = _companyA, Name = "T", Code = "T", IsActive = true });
        _context.OrderCategories.Add(new OrderCategory { Id = orderCategoryId, CompanyId = _companyA, Name = "FTTH", Code = "FTTH", IsActive = true });
        var buildingId = Guid.NewGuid();
        _context.Buildings.Add(new Domain.Buildings.Entities.Building { Id = buildingId, CompanyId = _companyA, Name = "B1", IsActive = true });
        _context.Orders.Add(new Order
        {
            Id = _orderInCompanyA,
            CompanyId = _companyA,
            PartnerId = _partnerIdA,
            OrderTypeId = orderTypeId,
            OrderCategoryId = orderCategoryId,
            BuildingId = buildingId,
            ServiceId = "S1",
            Status = "Completed",
            SourceSystem = "Manual",
            AddressLine1 = "A",
            City = "KL",
            State = "KL",
            Postcode = "50000"
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenCompanyIdNull_Throws()
    {
        var dto = new CreateInvoiceDto
        {
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            LineItems = new List<CreateInvoiceLineItemDto> { new() { Description = "Line", Quantity = 1, UnitPrice = 100m } }
        };

        var act = () => _service.CreateInvoiceAsync(dto, null, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CreateInvoice*Company context is required*");
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenLineItemOrderFromDifferentCompany_ThrowsBeforeSave()
    {
        // Invoice for company B, but line item references order that belongs to company A
        var dto = new CreateInvoiceDto
        {
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            LineItems = new List<CreateInvoiceLineItemDto>
            {
                new() { OrderId = _orderInCompanyA, Description = "Order line", Quantity = 1, UnitPrice = 100m }
            }
        };

        // TenantScope null so Orders query returns the order (filter allows all when null)
        var act = () => _service.CreateInvoiceAsync(dto, _companyB, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Company mismatch*Order*does not match expected company*");
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenSameCompany_Succeeds()
    {
        var dto = new CreateInvoiceDto
        {
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            LineItems = new List<CreateInvoiceLineItemDto>
            {
                new() { OrderId = _orderInCompanyA, Description = "Order line", Quantity = 1, UnitPrice = 100m }
            }
        };

        TenantScope.CurrentTenantId = _companyA;
        try
        {
            var result = await _service.CreateInvoiceAsync(dto, _companyA, Guid.NewGuid());
            result.Should().NotBeNull();
            result.CompanyId.Should().Be(_companyA);
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task BuildInvoiceLinesFromOrdersAsync_WhenCompanyIdEmpty_Throws()
    {
        var act = () => _service.BuildInvoiceLinesFromOrdersAsync(new[] { _orderInCompanyA }, Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BuildInvoiceLinesFromOrders*Company context is required*");
    }

    [Fact]
    public async Task ResolveInvoiceLineFromOrderAsync_WhenCompanyIdEmpty_Throws()
    {
        var act = () => _service.ResolveInvoiceLineFromOrderAsync(_orderInCompanyA, Guid.Empty);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ResolveInvoiceLineFromOrder*Company context is required*");
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_WhenOtherTenantInvoice_ReturnsNull()
    {
        var invoiceB = Guid.NewGuid();
        _context.Invoices.Add(new Invoice
        {
            Id = invoiceB,
            CompanyId = _companyB,
            InvoiceNumber = "INV-B-001",
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetInvoiceByIdAsync(invoiceB, _companyA, default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoiceCompanyIdAsync_WhenTenantScopeSetAndInvoiceFromOtherTenant_ReturnsNull()
    {
        var invoiceB = Guid.NewGuid();
        _context.Invoices.Add(new Invoice
        {
            Id = invoiceB,
            CompanyId = _companyB,
            InvoiceNumber = "INV-B-002",
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TenantScope.CurrentTenantId = _companyA;
        try
        {
            var companyId = await _service.GetInvoiceCompanyIdAsync(invoiceB, default);
            companyId.Should().BeNull("invoice belongs to company B but scope is company A");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task CreateInvoiceAsync_SameIdempotencyKeyTwice_ReturnsSameInvoice_NoDuplicate()
    {
        var dto = new CreateInvoiceDto
        {
            IdempotencyKey = "idem-inv-001",
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            LineItems = new List<CreateInvoiceLineItemDto>
            {
                new() { OrderId = _orderInCompanyA, Description = "Line", Quantity = 1, UnitPrice = 100m }
            }
        };

        TenantScope.CurrentTenantId = _companyA;
        InvoiceDto first;
        InvoiceDto second;
        try
        {
            first = await _service.CreateInvoiceAsync(dto, _companyA, Guid.NewGuid());
            second = await _service.CreateInvoiceAsync(dto, _companyA, Guid.NewGuid());
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        first.Id.Should().Be(second.Id, "replay with same idempotency key must return existing invoice");
        var count = await _context.Invoices.CountAsync(i => i.CompanyId == _companyA);
        count.Should().Be(1, "only one invoice created; no duplicate from replay");
    }

    [Fact]
    public async Task CreateInvoiceAsync_DifferentIdempotencyKeys_CreatesSeparateInvoices()
    {
        var dto1 = new CreateInvoiceDto
        {
            IdempotencyKey = "inv-key-A",
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            LineItems = new List<CreateInvoiceLineItemDto>
            {
                new() { OrderId = _orderInCompanyA, Description = "Line A", Quantity = 1, UnitPrice = 50m }
            }
        };
        var dto2 = new CreateInvoiceDto
        {
            IdempotencyKey = "inv-key-B",
            PartnerId = _partnerIdA,
            InvoiceDate = DateTime.UtcNow.Date,
            LineItems = new List<CreateInvoiceLineItemDto>
            {
                new() { OrderId = _orderInCompanyA, Description = "Line B", Quantity = 1, UnitPrice = 60m }
            }
        };

        TenantScope.CurrentTenantId = _companyA;
        InvoiceDto first;
        InvoiceDto second;
        try
        {
            first = await _service.CreateInvoiceAsync(dto1, _companyA, Guid.NewGuid());
            second = await _service.CreateInvoiceAsync(dto2, _companyA, Guid.NewGuid());
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        first.Id.Should().NotBe(second.Id);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
