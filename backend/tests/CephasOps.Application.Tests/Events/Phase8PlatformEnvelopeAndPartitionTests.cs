using System.Text.Json;
using CephasOps.Application.Events;
using CephasOps.Application.Events.Partitioning;
using CephasOps.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Phase 8: Platform envelope builder, partition key resolver, and lineage helper.
/// </summary>
public class Phase8PlatformEnvelopeAndPartitionTests
{
    [Fact]
    public void DefaultPartitionKeyResolver_WhenCompanyIdSet_ReturnsCompanyPrefix()
    {
        var resolver = new DefaultPartitionKeyResolver();
        var evt = new TestDomainEvent { CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111") };
        var key = resolver.GetPartitionKey(evt);
        key.Should().StartWith("c:");
        key.Should().Contain("11111111111111111111111111111111");
    }

    [Fact]
    public void DefaultPartitionKeyResolver_WhenNoCompanyButEntityContext_ReturnsEntityPrefix()
    {
        var resolver = new DefaultPartitionKeyResolver();
        var evt = new TestEntityEvent { EntityId = Guid.Parse("22222222-2222-2222-2222-222222222222") };
        var key = resolver.GetPartitionKey(evt);
        key.Should().StartWith("e:");
        key.Should().Contain("Order");
        key.Should().Contain("22222222222222222222222222222222");
    }

    [Fact]
    public void DefaultPartitionKeyResolver_WhenNoCompanyNoEntityButCorrelation_ReturnsCorrelationPrefix()
    {
        var resolver = new DefaultPartitionKeyResolver();
        var evt = new TestDomainEvent { CorrelationId = "workflow-123" };
        var key = resolver.GetPartitionKey(evt);
        key.Should().StartWith("k:");
        key.Should().Contain("workflow-123");
    }

    [Fact]
    public void DefaultPartitionKeyResolver_WhenOnlyEventId_ReturnsEventPrefix()
    {
        var resolver = new DefaultPartitionKeyResolver();
        var id = Guid.NewGuid();
        var evt = new TestDomainEvent { EventId = id };
        var key = resolver.GetPartitionKey(evt);
        key.Should().StartWith("v:");
        key.Should().Contain(id.ToString("N"));
    }

    [Fact]
    public void PlatformEventEnvelopeBuilder_Build_SetsPartitionKeyAndRootFromEvent()
    {
        var resolver = new DefaultPartitionKeyResolver();
        var options = Options.Create(new PlatformEventEnvelopeOptions { SourceService = "TestApi", SourceModule = "Test" });
        var builder = new PlatformEventEnvelopeBuilder(resolver, options);
        var evt = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            RootEventId = Guid.NewGuid()
        };
        var meta = builder.Build(evt);
        meta.PartitionKey.Should().NotBeNullOrEmpty();
        meta.RootEventId.Should().Be(evt.RootEventId);
        meta.SourceService.Should().Be("TestApi");
        meta.SourceModule.Should().Be("Test");
        meta.CapturedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void EventLineageHelper_SetLineageFrom_SetsParentRootCausationCorrelation()
    {
        var parent = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = "corr-1",
            RootEventId = Guid.NewGuid()
        };
        var child = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(child, parent);
        child.ParentEventId.Should().Be(parent.EventId);
        child.CausationId.Should().Be(parent.EventId);
        child.CorrelationId.Should().Be("corr-1");
        child.RootEventId.Should().Be(parent.RootEventId);
    }

    [Fact]
    public void EventLineageHelper_SetLineageFrom_WhenParentHasNoRoot_SetsRootToParentEventId()
    {
        var parent = new TestDomainEvent { EventId = Guid.NewGuid(), CorrelationId = "c1" };
        parent.RootEventId = null; // parent is root
        var child = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(child, parent);
        child.RootEventId.Should().Be(parent.EventId);
    }

    [Fact]
    public void EventLineageHelper_MultiLevelChain_PreservesRootAndParentChain()
    {
        var root = new TestDomainEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = "order-123",
            RootEventId = null
        };
        root.RootEventId = root.EventId;

        var child1 = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(child1, root);
        child1.ParentEventId.Should().Be(root.EventId);
        child1.RootEventId.Should().Be(root.EventId);
        child1.CausationId.Should().Be(root.EventId);
        child1.CorrelationId.Should().Be("order-123");

        var child2 = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(child2, child1);
        child2.ParentEventId.Should().Be(child1.EventId);
        child2.RootEventId.Should().Be(root.EventId);
        child2.CausationId.Should().Be(child1.EventId);
        child2.CorrelationId.Should().Be("order-123");
    }

    [Fact]
    public void EventLineageHelper_ReplayLineageIntegrity_SerializedPayloadPreservesLineage()
    {
        var root = new TestDomainEvent { EventId = Guid.NewGuid(), CorrelationId = "replay-corr", RootEventId = null };
        root.RootEventId = root.EventId;
        var child = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(child, root);

        var payload = JsonSerializer.Serialize(child);
        var deserialized = JsonSerializer.Deserialize<TestDomainEvent>(payload);
        deserialized.Should().NotBeNull();
        deserialized!.ParentEventId.Should().Be(root.EventId);
        deserialized.RootEventId.Should().Be(root.EventId);
        deserialized.CausationId.Should().Be(root.EventId);
        deserialized.CorrelationId.Should().Be("replay-corr");
    }

    [Fact]
    public void EventLineageHelper_CorrelationTree_AllDescendantsShareSameRootEventId()
    {
        var root = new TestDomainEvent { EventId = Guid.NewGuid(), CorrelationId = "tree-1", RootEventId = null };
        root.RootEventId = root.EventId;

        var a = new TestDomainEvent { EventId = Guid.NewGuid() };
        var b = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(a, root);
        EventLineageHelper.SetLineageFrom(b, root);

        a.RootEventId.Should().Be(root.EventId);
        b.RootEventId.Should().Be(root.EventId);
        a.ParentEventId.Should().Be(root.EventId);
        b.ParentEventId.Should().Be(root.EventId);

        var grandchild = new TestDomainEvent { EventId = Guid.NewGuid() };
        EventLineageHelper.SetLineageFrom(grandchild, a);
        grandchild.RootEventId.Should().Be(root.EventId);
        grandchild.ParentEventId.Should().Be(a.EventId);
    }

    private sealed class TestDomainEvent : DomainEvent
    {
    }

    private sealed class TestEntityEvent : DomainEvent, IHasEntityContext
    {
        public Guid? EntityId { get; set; }
        string? IHasEntityContext.EntityType => "Order";
        Guid? IHasEntityContext.EntityId => EntityId;
    }
}
