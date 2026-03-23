using CephasOps.Application.Companies.DTOs;
using CephasOps.Application.Companies.Services;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Tests that single-company-mode removal is enforced: null/empty companyId never means "all tenants".
/// List methods return empty, get-by-id returns null, mutations throw when no valid company context.
/// </summary>
[Collection("TenantScopeTests")]
public class SingleCompanyModeRemovalTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PartnerService _partnerService;
    private readonly Guid? _previousTenantId;

    public SingleCompanyModeRemovalTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        var logger = new Mock<ILogger<PartnerService>>();
        _partnerService = new PartnerService(_context, logger.Object);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetPartnersAsync_NoCompanyContext_ReturnsEmptyList()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _partnerService.GetPartnersAsync(companyId: null);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetPartnersAsync_GuidEmptyCompanyId_ReturnsEmptyList()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _partnerService.GetPartnersAsync(companyId: Guid.Empty);
            result.Should().NotBeNull().And.BeEmpty();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetPartnerByIdAsync_NoCompanyContext_ReturnsNull()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var result = await _partnerService.GetPartnerByIdAsync(Guid.NewGuid(), companyId: null);
            result.Should().BeNull();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task CreatePartnerAsync_NoCompanyContext_ThrowsCompanyContextRequired()
    {
        TenantScope.CurrentTenantId = null;
        try
        {
            var dto = new CreatePartnerDto
            {
                Name = "Test Partner",
                PartnerType = "Telco",
                IsActive = true
            };
            var act = () => _partnerService.CreatePartnerAsync(dto, companyId: null);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Company context is required*");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetPartnersAsync_ValidCompanyId_ReturnsOnlyThatTenantsData()
    {
        var companyA = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyA;
        try
        {
            _context.Partners.Add(new Partner
            {
                Id = Guid.NewGuid(),
                CompanyId = companyA,
                Name = "Partner A",
                Code = "PA",
                PartnerType = "Telco",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var result = await _partnerService.GetPartnersAsync(companyId: companyA);
        result.Should().HaveCount(1);
        result![0].Name.Should().Be("Partner A");
    }

    [Fact]
    public async Task GetPartnersAsync_NullCompanyId_WithTenantScopeSet_ReturnsScopedData()
    {
        var companyA = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyA;
        try
        {
            _context.Partners.Add(new Partner
            {
                Id = Guid.NewGuid(),
                CompanyId = companyA,
                Name = "Partner A",
                Code = "PA",
                PartnerType = "Telco",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _partnerService.GetPartnersAsync(companyId: null);
            result.Should().HaveCount(1);
            result![0].Name.Should().Be("Partner A");
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }
    }

    [Fact]
    public async Task GetPartnerByIdAsync_ValidCompanyId_SameTenant_ReturnsPartner()
    {
        var companyA = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyA;
        try
        {
            _context.Partners.Add(new Partner
            {
                Id = partnerId,
                CompanyId = companyA,
                Name = "Partner A",
                Code = "PA",
                PartnerType = "Telco",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var result = await _partnerService.GetPartnerByIdAsync(partnerId, companyId: companyA);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Partner A");
    }

    [Fact]
    public async Task GetPartnerByIdAsync_ValidCompanyId_OtherTenant_ReturnsNull()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyA;
        try
        {
            _context.Partners.Add(new Partner
            {
                Id = partnerId,
                CompanyId = companyA,
                Name = "Partner A",
                Code = "PA",
                PartnerType = "Telco",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = _previousTenantId;
        }

        var result = await _partnerService.GetPartnerByIdAsync(partnerId, companyId: companyB);
        result.Should().BeNull();
    }
}
