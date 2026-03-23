using CephasOps.Application.Platform.FeatureFlags;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Platform;

public class FeatureFlagServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FeatureFlagService _service;
    private readonly Guid? _previousTenantId;

    public FeatureFlagServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new FeatureFlagService(_context);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFlagNotSet_ReturnsFalse()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        await SeedCompanyAsync(tenantId, companyId);

        var result = await _service.IsEnabledAsync("AdvancedReports", companyId);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFlagEnabled_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        await SeedCompanyAsync(tenantId, companyId);
        _context.TenantFeatureFlags.Add(new TenantFeatureFlag { TenantId = tenantId, FeatureKey = "AdvancedReports", IsEnabled = true });
        await _context.SaveChangesAsync();

        var result = await _service.IsEnabledAsync("AdvancedReports", companyId);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SetFlagAsync_WhenTenantAdminSetsPlatformOnly_Throws()
    {
        var tenantId = Guid.NewGuid();
        await _context.Tenants.AddAsync(new CephasOps.Domain.Tenants.Entities.Tenant { Id = tenantId, Name = "T", Slug = "t", IsActive = true });
        await _context.SaveChangesAsync();

        var act = () => _service.SetFlagAsync(tenantId, "Platform.Observability", true, isPlatformAdmin: false);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*platform-only*");
    }

    [Fact]
    public async Task SetFlagAsync_WhenPlatformAdminSetsPlatformOnly_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        await _context.Tenants.AddAsync(new CephasOps.Domain.Tenants.Entities.Tenant { Id = tenantId, Name = "T", Slug = "t", IsActive = true });
        await _context.SaveChangesAsync();

        await _service.SetFlagAsync(tenantId, "Platform.Observability", true, isPlatformAdmin: true);
        var flags = await _service.GetFlagsForTenantAsync(tenantId);
        flags.Should().ContainSingle(f => f.FeatureKey == "Platform.Observability" && f.IsEnabled);
    }

    private async Task SeedCompanyAsync(Guid tenantId, Guid companyId)
    {
        await _context.Tenants.AddAsync(new CephasOps.Domain.Tenants.Entities.Tenant { Id = tenantId, Name = "T", Slug = "t", IsActive = true });
        await _context.Companies.AddAsync(new CephasOps.Domain.Companies.Entities.Company { Id = companyId, TenantId = tenantId, LegalName = "C", ShortName = "C" });
        await _context.SaveChangesAsync();
    }
}
