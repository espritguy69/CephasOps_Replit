using CephasOps.Application.Billing.Usage;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CephasOps.Application.Tests.Usage;

public class TenantUsageServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TenantUsage_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RecordIncrementAsync_creates_record_then_increments()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var start = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var service = new TenantUsageService(context, NullLogger<TenantUsageService>.Instance);

        await service.RecordIncrementAsync(tenantId, TenantUsageService.MetricKeys.OrdersCreated, start, end, 1);
        var first = await context.TenantUsageRecords.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == TenantUsageService.MetricKeys.OrdersCreated);
        first.Should().NotBeNull();
        first!.Quantity.Should().Be(1);

        await service.RecordIncrementAsync(tenantId, TenantUsageService.MetricKeys.OrdersCreated, start, end, 2);
        var second = await context.TenantUsageRecords.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == TenantUsageService.MetricKeys.OrdersCreated);
        second!.Quantity.Should().Be(3);
        context.TenantUsageRecords.Count().Should().Be(1);
    }

    [Fact]
    public async Task RecordUsageAsync_resolves_tenant_from_company_and_increments()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t", IsActive = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        context.Companies.Add(new Company { Id = companyId, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C", Vertical = "V", Status = Domain.Companies.Enums.CompanyStatus.Active });
        await context.SaveChangesAsync();

        var service = new TenantUsageService(context, NullLogger<TenantUsageService>.Instance);
        await service.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.InvoicesGenerated, 1);

        var record = await context.TenantUsageRecords.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == TenantUsageService.MetricKeys.InvoicesGenerated);
        record.Should().NotBeNull();
        record!.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task RecordUsageAsync_skips_when_company_has_no_tenant()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, TenantId = null, LegalName = "C", ShortName = "C", Code = "C", Vertical = "V", Status = Domain.Companies.Enums.CompanyStatus.Active });
        await context.SaveChangesAsync();

        var service = new TenantUsageService(context, NullLogger<TenantUsageService>.Instance);
        await service.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.OrdersCreated, 1);

        context.TenantUsageRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task RecalculateUserMetricsForTenantAsync_sets_TotalUsers_and_ActiveUsers()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t", IsActive = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        context.Companies.Add(new Company { Id = companyId, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C", Vertical = "V", Status = Domain.Companies.Enums.CompanyStatus.Active });
        context.Users.Add(new User { Id = Guid.NewGuid(), CompanyId = companyId, Name = "U1", Email = "u1@t.com", IsActive = true });
        context.Users.Add(new User { Id = Guid.NewGuid(), CompanyId = companyId, Name = "U2", Email = "u2@t.com", IsActive = false });
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId;
            await context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }

        var service = new TenantUsageService(context, NullLogger<TenantUsageService>.Instance);
        await service.RecalculateUserMetricsForTenantAsync(tenantId);

        var total = await context.TenantUsageRecords.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == TenantUsageService.MetricKeys.TotalUsers);
        var active = await context.TenantUsageRecords.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.MetricKey == TenantUsageService.MetricKeys.ActiveUsers);
        total.Should().NotBeNull();
        total!.Quantity.Should().Be(2);
        active.Should().NotBeNull();
        active!.Quantity.Should().Be(1);
    }
}
