using CephasOps.Application.Billing.Subscription.DTOs;
using CephasOps.Application.Platform;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Platform;

/// <summary>SaaS operations hardening: platform admin subscription GET/PATCH tests.</summary>
public class PlatformAdminSubscriptionTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PlatformSub_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetTenantSubscriptionAsync_WhenNoSubscription_ReturnsNull()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        await context.SaveChangesAsync();

        var sut = new PlatformAdminService(context);
        var result = await sut.GetTenantSubscriptionAsync(tenantId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTenantSubscriptionAsync_WhenSubscriptionExists_ReturnsDto()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        context.BillingPlans.Add(new BillingPlan { Id = planId, Name = "Trial", Slug = "trial", BillingCycle = BillingCycle.Monthly, IsActive = true });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingPlanId = planId,
            Status = TenantSubscriptionStatus.Trialing,
            StartedAtUtc = DateTime.UtcNow,
            TrialEndsAtUtc = DateTime.UtcNow.AddDays(14)
        });
        await context.SaveChangesAsync();

        var sut = new PlatformAdminService(context);
        var result = await sut.GetTenantSubscriptionAsync(tenantId);
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);
        result.PlanSlug.Should().Be("trial");
        result.Status.Should().Be(TenantSubscriptionStatus.Trialing);
    }

    [Fact]
    public async Task UpdateTenantSubscriptionAsync_WhenSubscriptionExists_UpdatesAndReturnsDto()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        context.BillingPlans.Add(new BillingPlan { Id = planId, Name = "Trial", Slug = "trial", BillingCycle = BillingCycle.Monthly, IsActive = true });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingPlanId = planId,
            Status = TenantSubscriptionStatus.Trialing,
            StartedAtUtc = DateTime.UtcNow,
            SeatLimit = null,
            StorageLimitBytes = null
        });
        await context.SaveChangesAsync();

        var sut = new PlatformAdminService(context);
        var request = new PlatformTenantSubscriptionUpdateRequest
        {
            Status = "Active",
            SeatLimit = 10,
            StorageLimitBytes = 1_000_000
        };
        var result = await sut.UpdateTenantSubscriptionAsync(tenantId, request);
        result.Should().NotBeNull();
        result!.Status.Should().Be(TenantSubscriptionStatus.Active);
        result.SeatLimit.Should().Be(10);
        result.StorageLimitBytes.Should().Be(1_000_000);
    }

    [Fact]
    public async Task UpdateTenantSubscriptionAsync_WhenInvalidPlanSlug_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        context.BillingPlans.Add(new BillingPlan { Id = planId, Name = "Trial", Slug = "trial", BillingCycle = BillingCycle.Monthly, IsActive = true });
        context.TenantSubscriptions.Add(new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BillingPlanId = planId,
            Status = TenantSubscriptionStatus.Active,
            StartedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var sut = new PlatformAdminService(context);
        var request = new PlatformTenantSubscriptionUpdateRequest { PlanSlug = "nonexistent" };
        await sut.Invoking(s => s.UpdateTenantSubscriptionAsync(tenantId, request))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*nonexistent*");
    }
}
