using CephasOps.Application.Workflow;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Metrics;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.TenantIsolation;

/// <summary>
/// Minimal tests for tenant operational observability: metrics record without throw, fairness ordering caps per tenant.
/// </summary>
public class TenantOperationalObservabilityTests
{
    [Fact]
    public void TenantOperationalMetrics_RecordRequest_DoesNotThrow()
    {
        var tenantId = Guid.NewGuid();
        TenantOperationalMetrics.RecordRequest(tenantId, true);
        TenantOperationalMetrics.RecordRequest(tenantId, false);
        TenantOperationalMetrics.RecordRequest(null, true);
    }

    [Fact]
    public void TenantOperationalMetrics_RecordJob_DoesNotThrow()
    {
        var tenantId = Guid.NewGuid();
        TenantOperationalMetrics.RecordJobExecuted(tenantId);
        TenantOperationalMetrics.RecordJobFailure(tenantId);
        TenantOperationalMetrics.RecordJobExecuted(null);
    }

    [Fact]
    public void TenantOperationalMetrics_RecordNotificationAndIntegration_DoesNotThrow()
    {
        var tenantId = Guid.NewGuid();
        TenantOperationalMetrics.RecordNotificationSent(tenantId, true);
        TenantOperationalMetrics.RecordNotificationSent(tenantId, false);
        TenantOperationalMetrics.RecordIntegrationDelivery(tenantId, true);
        TenantOperationalMetrics.RecordIntegrationDelivery(null, false);
    }

    [Fact]
    public void TenantFairnessOrdering_MaxPerTenant_LimitsAndRoundRobins()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var items = new[]
        {
            CreateJob(tenantA),
            CreateJob(tenantA),
            CreateJob(tenantA),
            CreateJob(tenantB),
            CreateJob(tenantB),
        };
        var ordered = TenantFairnessOrdering.OrderByTenantFairness(items, j => j.CompanyId, maxPerTenant: 2);
        ordered.Should().HaveCount(4); // at most 2 per tenant: 2 from A + 2 from B
        var aCount = ordered.Count(j => j.CompanyId == tenantA);
        var bCount = ordered.Count(j => j.CompanyId == tenantB);
        aCount.Should().Be(2);
        bCount.Should().Be(2);
        // Round-robin: first A, then B, then A, then B
        ordered[0].CompanyId.Should().Be(tenantA);
        ordered[1].CompanyId.Should().Be(tenantB);
        ordered[2].CompanyId.Should().Be(tenantA);
        ordered[3].CompanyId.Should().Be(tenantB);
    }

    [Fact]
    public void TenantFairnessOrdering_MaxPerTenantZero_ReturnsAllUnchanged()
    {
        var tenantA = Guid.NewGuid();
        var items = new[] { CreateJob(tenantA), CreateJob(tenantA) };
        var ordered = TenantFairnessOrdering.OrderByTenantFairness(items, j => j.CompanyId, maxPerTenant: 0);
        ordered.Should().HaveCount(2).And.ContainInOrder(items);
    }

    private static BackgroundJob CreateJob(Guid? companyId)
    {
        return new BackgroundJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            JobType = "Test",
            State = BackgroundJobState.Queued,
            CreatedAt = DateTime.UtcNow
        };
    }
}
