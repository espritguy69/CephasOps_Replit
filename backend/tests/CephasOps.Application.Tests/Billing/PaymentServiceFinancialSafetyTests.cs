using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Commands;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Billing;

/// <summary>
/// Tenant financial safety: PaymentService requires company for create; cross-tenant read returns null.
/// </summary>
[Collection("TenantScopeTests")]
public class PaymentServiceFinancialSafetyTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PaymentService _service;
    private readonly Guid _companyA;
    private readonly Guid _companyB;
    private readonly Guid _paymentInA;
    private readonly Guid? _previousTenantId;

    public PaymentServiceFinancialSafetyTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
        _paymentInA = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Payments.Add(new Payment
        {
            Id = _paymentInA,
            CompanyId = _companyA,
            PaymentNumber = "REC-202601-0001",
            PaymentType = PaymentType.Income,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 100m,
            Currency = "MYR",
            PayerPayeeName = "Payer",
            IsVoided = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
        _service = new PaymentService(
            _context,
            Mock.Of<ISupplierInvoiceService>(),
            new CommandProcessingLogStore(_context, Mock.Of<ILogger<CommandProcessingLogStore>>()),
            Mock.Of<ILogger<PaymentService>>());
    }

    [Fact]
    public async Task CreatePaymentAsync_WhenCompanyIdNull_Throws()
    {
        var dto = new CreatePaymentDto
        {
            PaymentType = PaymentType.Income,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 50m,
            Currency = "MYR",
            PayerPayeeName = "Test"
        };

        var act = () => _service.CreatePaymentAsync(dto, null, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CreatePayment*Company context is required*");
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WhenOtherTenantPayment_ReturnsNull()
    {
        var result = await _service.GetPaymentByIdAsync(_paymentInA, _companyB, default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WhenSameTenant_ReturnsPayment()
    {
        var result = await _service.GetPaymentByIdAsync(_paymentInA, _companyA, default);
        result.Should().NotBeNull();
        result!.Id.Should().Be(_paymentInA);
        result.CompanyId.Should().Be(_companyA);
    }

    [Fact]
    public async Task CreatePaymentAsync_SameIdempotencyKeyTwice_ReturnsSamePayment_NoDuplicate()
    {
        var dto = new CreatePaymentDto
        {
            IdempotencyKey = "idem-pay-001",
            PaymentType = PaymentType.Income,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 75m,
            Currency = "MYR",
            PayerPayeeName = "Payer"
        };

        var first = await _service.CreatePaymentAsync(dto, _companyA, Guid.NewGuid());
        var second = await _service.CreatePaymentAsync(dto, _companyA, Guid.NewGuid());

        first.Id.Should().Be(second.Id, "replay with same idempotency key must return existing payment");
        var count = await _context.Payments.CountAsync(p => p.CompanyId == _companyA);
        count.Should().Be(2, "original seed payment + one new; no duplicate from replay");
    }

    [Fact]
    public async Task CreatePaymentAsync_DifferentIdempotencyKeys_CreatesSeparatePayments()
    {
        var dto1 = new CreatePaymentDto
        {
            IdempotencyKey = "key-A",
            PaymentType = PaymentType.Income,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 10m,
            Currency = "MYR",
            PayerPayeeName = "P"
        };
        var dto2 = new CreatePaymentDto
        {
            IdempotencyKey = "key-B",
            PaymentType = PaymentType.Income,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 10m,
            Currency = "MYR",
            PayerPayeeName = "P"
        };

        var first = await _service.CreatePaymentAsync(dto1, _companyA, Guid.NewGuid());
        var second = await _service.CreatePaymentAsync(dto2, _companyA, Guid.NewGuid());

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public async Task CreatePaymentAsync_SameKeyDifferentCompany_CreatesSeparatePayments_TenantScoped()
    {
        _context.Companies.Add(new Company { Id = _companyB, ShortName = "B", LegalName = "Co B", IsActive = true });
        await _context.SaveChangesAsync();

        var dto = new CreatePaymentDto
        {
            IdempotencyKey = "shared-key",
            PaymentType = PaymentType.Income,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 50m,
            Currency = "MYR",
            PayerPayeeName = "P"
        };

        var payA = await _service.CreatePaymentAsync(dto, _companyA, Guid.NewGuid());
        var payB = await _service.CreatePaymentAsync(dto, _companyB, Guid.NewGuid());

        payA.Id.Should().NotBe(payB.Id);
        payA.CompanyId.Should().Be(_companyA);
        payB.CompanyId.Should().Be(_companyB);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
    }
}
