using CephasOps.Application.Common.Services;
using CephasOps.Application.Provisioning;
using CephasOps.Application.Provisioning.DTOs;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CephasOps.Application.Tests.Provisioning;

/// <summary>SaaS scaling: tenant provisioning service tests.</summary>
public class CompanyProvisioningServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "Provisioning_" + Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static CompanyProvisioningService CreateSut(ApplicationDbContext context)
    {
        var hasher = new CompatibilityPasswordHasher();
        var logger = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning)).CreateLogger<CompanyProvisioningService>();
        return new CompanyProvisioningService(context, hasher, logger);
    }

    [Fact]
    public async Task IsCompanyCodeInUseAsync_WhenEmpty_ReturnsFalse()
    {
        await using var context = CreateContext();
        var sut = CreateSut(context);
        (await sut.IsCompanyCodeInUseAsync("")).Should().BeFalse();
        (await sut.IsCompanyCodeInUseAsync("   ")).Should().BeFalse();
    }

    [Fact]
    public async Task IsCompanyCodeInUseAsync_WhenCodeExists_ReturnsTrue()
    {
        await using var context = CreateContext();
        context.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Code = "ACME",
            LegalName = "Acme",
            ShortName = "Acme"
        });
        await context.SaveChangesAsync();
        var sut = CreateSut(context);
        (await sut.IsCompanyCodeInUseAsync("acme")).Should().BeTrue();
        (await sut.IsCompanyCodeInUseAsync("ACME")).Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugInUseAsync_WhenSlugExists_ReturnsTrue()
    {
        await using var context = CreateContext();
        context.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Acme", Slug = "acme" });
        await context.SaveChangesAsync();
        var sut = CreateSut(context);
        (await sut.IsSlugInUseAsync("acme")).Should().BeTrue();
    }

    [Fact]
    public async Task ProvisionAsync_WhenCompanyCodeInUse_ThrowsInvalidOperationException()
    {
        await using var context = CreateContext();
        SeedRolesAndPlan(context);
        context.Companies.Add(new Company { Id = Guid.NewGuid(), Code = "ACME", LegalName = "Acme", ShortName = "Acme" });
        await context.SaveChangesAsync();
        var sut = CreateSut(context);

        var request = new ProvisionTenantRequestDto
        {
            CompanyName = "Acme",
            CompanyCode = "ACME",
            AdminFullName = "Admin",
            AdminEmail = "admin@new.test",
            AdminPassword = "Pass1!"
        };

        await sut.Invoking(s => s.ProvisionAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ACME*");
    }

    [Fact]
    public async Task ProvisionAsync_CreatesTenantCompanyDepartmentsAdminAndSubscription()
    {
        await using var context = CreateContext();
        SeedRolesAndPlan(context);
        var sut = CreateSut(context);

        var request = new ProvisionTenantRequestDto
        {
            CompanyName = "Acme Inc",
            CompanyCode = "ACME",
            Slug = "acme",
            AdminFullName = "Admin User",
            AdminEmail = "admin@acme.test",
            AdminPassword = "Pass1!",
            PlanSlug = "trial",
            TrialDays = 7
        };

        var result = await sut.ProvisionAsync(request);

        result.Should().NotBeNull();
        result.TenantId.Should().NotBe(Guid.Empty);
        result.CompanyId.Should().NotBe(Guid.Empty);
        result.CompanyCode.Should().Be("ACME");
        result.Slug.Should().Be("acme");
        result.AdminUserId.Should().NotBe(Guid.Empty);
        result.AdminEmail.Should().Be("admin@acme.test");
        result.Departments.Should().HaveCount(5);
        result.SubscriptionId.Should().NotBeNull();
        result.PlanSlug.Should().NotBeNullOrEmpty();

        await context.SaveChangesAsync();
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == result.TenantId);
        tenant.Should().NotBeNull();
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == result.CompanyId);
        company.Should().NotBeNull();
        company!.SubscriptionId.Should().Be(result.SubscriptionId);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == result.AdminUserId);
        user.Should().NotBeNull();
        user!.CompanyId.Should().Be(result.CompanyId);
    }

    private static void SeedRolesAndPlan(ApplicationDbContext context)
    {
        var roleId = Guid.NewGuid();
        context.Roles.Add(new Role { Id = roleId, Name = "Admin" });
        var planId = Guid.NewGuid();
        context.BillingPlans.Add(new BillingPlan
        {
            Id = planId,
            Name = "Trial",
            Slug = "trial",
            BillingCycle = BillingCycle.Monthly,
            IsActive = true
        });
    }
}
