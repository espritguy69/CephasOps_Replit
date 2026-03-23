using CephasOps.Application.Subscription;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Companies.Enums;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Subscription;

/// <summary>Phase 3: Subscription and tenant access evaluation.</summary>
public class SubscriptionAccessServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "SubscriptionAccess_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_NullCompanyId_Allows()
    {
        await using var context = CreateContext();
        var sut = new SubscriptionAccessService(context);

        var result = await sut.GetAccessForCompanyAsync(null);

        result.Allowed.Should().BeTrue();
        result.DenialReason.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_EmptyGuid_Allows()
    {
        await using var context = CreateContext();
        var sut = new SubscriptionAccessService(context);

        var result = await sut.GetAccessForCompanyAsync(Guid.Empty);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_UnknownCompany_Allows()
    {
        await using var context = CreateContext();
        var sut = new SubscriptionAccessService(context);

        var result = await sut.GetAccessForCompanyAsync(Guid.NewGuid());

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_ActiveCompany_NoTenant_Allows()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_TrialCompany_Allows()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Trial, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_SuspendedCompany_DeniesWithReason()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Suspended, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Be(SubscriptionDenialReasons.TenantSuspended);
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_DisabledCompany_DeniesWithReason()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Disabled, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Be(SubscriptionDenialReasons.TenantDisabled);
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_PendingProvisioning_DeniesWithReason()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.PendingProvisioning, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Be("tenant_pending_provisioning");
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_Archived_DeniesWithReason()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Archived, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Be("tenant_archived");
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_ActiveCompany_WithTenant_NoSubscription_Allows()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_ActiveCompany_SubscriptionCancelled_Denies()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C" });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenantId,
            BillingPlanId = Guid.NewGuid(),
            Status = TenantSubscriptionStatus.Cancelled,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1)
        });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Be(SubscriptionDenialReasons.SubscriptionCancelled);
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_ActiveCompany_SubscriptionPastDue_ReadOnly()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C" });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenantId,
            BillingPlanId = Guid.NewGuid(),
            Status = TenantSubscriptionStatus.PastDue,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1)
        });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.ReadOnlyMode.Should().BeTrue();
        result.DenialReason.Should().Be(SubscriptionDenialReasons.SubscriptionPastDue);
    }

    [Fact]
    public async Task GetAccessForCompanyAsync_ActiveCompany_SubscriptionExpired_Denies()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C" });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenantId,
            BillingPlanId = Guid.NewGuid(),
            Status = TenantSubscriptionStatus.Active,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-2),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.GetAccessForCompanyAsync(companyId);

        result.Allowed.Should().BeFalse();
        result.DenialReason.Should().Be(SubscriptionDenialReasons.SubscriptionExpired);
    }

    [Fact]
    public async Task CanPerformWritesAsync_WhenAllowed_ReturnsTrue()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.CanPerformWritesAsync(companyId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanPerformWritesAsync_WhenDenied_ReturnsFalse()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Suspended, TenantId = null, LegalName = "C", ShortName = "C", Code = "C" });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.CanPerformWritesAsync(companyId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanPerformWritesAsync_WhenReadOnly_ReturnsFalse()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Companies.Add(new Company { Id = companyId, Status = CompanyStatus.Active, TenantId = tenantId, LegalName = "C", ShortName = "C", Code = "C" });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenantId,
            BillingPlanId = Guid.NewGuid(),
            Status = TenantSubscriptionStatus.PastDue,
            StartedAtUtc = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEndUtc = DateTime.UtcNow.AddMonths(1)
        });
        await context.SaveChangesAsync();

        var sut = new SubscriptionAccessService(context);
        var result = await sut.CanPerformWritesAsync(companyId);

        result.Should().BeFalse();
    }
}
