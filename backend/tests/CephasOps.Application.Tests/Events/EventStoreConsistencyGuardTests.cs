using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Unit tests for EventStoreConsistencyGuard: metadata, company when entity context, parent/root linkage, stream consistency.
/// </summary>
public class EventStoreConsistencyGuardTests
{
    [Fact]
    public void RequireEventMetadata_WhenValid_DoesNotThrow()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "OrderCompleted" };
        var act = () => EventStoreConsistencyGuard.RequireEventMetadata(entry);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireEventMetadata_WhenEventIdEmpty_Throws()
    {
        var entry = new EventStoreEntry { EventId = Guid.Empty, EventType = "OrderCompleted" };
        var act = () => EventStoreConsistencyGuard.RequireEventMetadata(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EventId*required*cannot be empty*");
    }

    [Fact]
    public void RequireEventMetadata_WhenEventTypeNull_Throws()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = null! };
        var act = () => EventStoreConsistencyGuard.RequireEventMetadata(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EventType*required*");
    }

    [Fact]
    public void RequireEventMetadata_WhenEventTypeEmpty_Throws()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "" };
        var act = () => EventStoreConsistencyGuard.RequireEventMetadata(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EventType*required*");
    }

    [Fact]
    public void RequireCompanyWhenEntityContext_WhenNoEntityContext_DoesNotThrow()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", EntityType = null, EntityId = null };
        var act = () => EventStoreConsistencyGuard.RequireCompanyWhenEntityContext(entry);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireCompanyWhenEntityContext_WhenEntityContextAndCompanyPresent_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "OrderCompleted", EntityType = "Order", EntityId = Guid.NewGuid(), CompanyId = companyId };
        var act = () => EventStoreConsistencyGuard.RequireCompanyWhenEntityContext(entry);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireCompanyWhenEntityContext_WhenEntityContextButCompanyMissing_Throws()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "OrderCompleted", EntityType = "Order", EntityId = Guid.NewGuid(), CompanyId = null };
        var act = () => EventStoreConsistencyGuard.RequireCompanyWhenEntityContext(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CompanyId*required*entity context*");
    }

    [Fact]
    public void RequireCompanyWhenEntityContext_WhenEntityContextButCompanyEmpty_Throws()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "OrderCompleted", EntityType = "Order", EntityId = Guid.NewGuid(), CompanyId = Guid.Empty };
        var act = () => EventStoreConsistencyGuard.RequireCompanyWhenEntityContext(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CompanyId*required*entity context*");
    }

    [Fact]
    public void RequireValidParentRootLinkage_WhenNoParent_DoesNotThrow()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", ParentEventId = null, RootEventId = null };
        var act = () => EventStoreConsistencyGuard.RequireValidParentRootLinkage(entry);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireValidParentRootLinkage_WhenParentEqualsEventId_Throws()
    {
        var eventId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = eventId, EventType = "X", ParentEventId = eventId };
        var act = () => EventStoreConsistencyGuard.RequireValidParentRootLinkage(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ParentEventId*cannot equal EventId*self-reference*");
    }

    [Fact]
    public void RequireValidParentRootLinkage_WhenParentSetButRootEmpty_Throws()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", ParentEventId = Guid.NewGuid(), RootEventId = Guid.Empty };
        var act = () => EventStoreConsistencyGuard.RequireValidParentRootLinkage(entry);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RootEventId*cannot be empty*ParentEventId*");
    }

    [Fact]
    public void RequireStreamConsistency_WhenNoPriorEvents_DoesNotThrow()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", EntityType = "Order", EntityId = Guid.NewGuid(), CompanyId = Guid.NewGuid() };
        var act = () => EventStoreConsistencyGuard.RequireStreamConsistency(entry, new List<EventStoreEntry>());
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireStreamConsistency_WhenPriorSameCompany_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "Y", EntityType = "Order", EntityId = entityId, CompanyId = companyId };
        var prior = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", EntityType = "Order", EntityId = entityId, CompanyId = companyId };
        var act = () => EventStoreConsistencyGuard.RequireStreamConsistency(entry, new List<EventStoreEntry> { prior });
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireStreamConsistency_WhenPriorDifferentCompany_Throws()
    {
        var entityId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "Y", EntityType = "Order", EntityId = entityId, CompanyId = Guid.NewGuid() };
        var prior = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", EntityType = "Order", EntityId = entityId, CompanyId = Guid.NewGuid() };
        var act = () => EventStoreConsistencyGuard.RequireStreamConsistency(entry, new List<EventStoreEntry> { prior });
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Company mismatch*event stream*");
    }

    [Fact]
    public void RequireStreamConsistency_WhenPriorDifferentEntityType_Throws()
    {
        var entityId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "Y", EntityType = "Invoice", EntityId = entityId, CompanyId = companyId };
        var prior = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", EntityType = "Order", EntityId = entityId, CompanyId = companyId };
        var act = () => EventStoreConsistencyGuard.RequireStreamConsistency(entry, new List<EventStoreEntry> { prior });
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EntityType mismatch*event stream*");
    }

    [Fact]
    public void RequireTenantOrBypassForAppend_WhenTenantSet_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = companyId;
            var act = () => EventStoreConsistencyGuard.RequireTenantOrBypassForAppend("Append");
            act.Should().NotThrow();
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public void RequireTenantOrBypassForAppend_WhenBypassActive_DoesNotThrow()
    {
        try
        {
            TenantSafetyGuard.EnterPlatformBypass();
            var act = () => EventStoreConsistencyGuard.RequireTenantOrBypassForAppend("Append");
            act.Should().NotThrow();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    [Fact]
    public void RequireTenantOrBypassForAppend_WhenNoTenantAndNoBypass_Throws()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = null;
            var act = () => EventStoreConsistencyGuard.RequireTenantOrBypassForAppend("Append");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Append*EventStore append requires*tenant context*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public void RequireParentRootCompanyMatch_WhenEntryNoCompany_DoesNotThrow()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", CompanyId = null };
        var parent = new EventStoreEntry { EventId = Guid.NewGuid(), CompanyId = Guid.NewGuid() };
        var act = () => EventStoreConsistencyGuard.RequireParentRootCompanyMatch(entry, parent, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireParentRootCompanyMatch_WhenParentNullRootNull_DoesNotThrow()
    {
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "X", CompanyId = Guid.NewGuid() };
        var act = () => EventStoreConsistencyGuard.RequireParentRootCompanyMatch(entry, null, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireParentRootCompanyMatch_WhenParentSameCompany_DoesNotThrow()
    {
        var companyId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "Y", CompanyId = companyId, ParentEventId = Guid.NewGuid() };
        var parent = new EventStoreEntry { EventId = entry.ParentEventId!.Value, CompanyId = companyId };
        var act = () => EventStoreConsistencyGuard.RequireParentRootCompanyMatch(entry, parent, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void RequireParentRootCompanyMatch_WhenParentCompanyMismatch_Throws()
    {
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "Y", CompanyId = companyId, ParentEventId = Guid.NewGuid() };
        var parent = new EventStoreEntry { EventId = entry.ParentEventId!.Value, CompanyId = otherCompanyId };
        var act = () => EventStoreConsistencyGuard.RequireParentRootCompanyMatch(entry, parent, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Parent event must belong to the same company*");
    }

    [Fact]
    public void RequireParentRootCompanyMatch_WhenRootCompanyMismatch_Throws()
    {
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var entry = new EventStoreEntry { EventId = Guid.NewGuid(), EventType = "Y", CompanyId = companyId, RootEventId = Guid.NewGuid() };
        var root = new EventStoreEntry { EventId = entry.RootEventId!.Value, CompanyId = otherCompanyId };
        var act = () => EventStoreConsistencyGuard.RequireParentRootCompanyMatch(entry, null, root);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Root event must belong to the same company*");
    }

    [Fact]
    public void RequireDuplicateAppendRejected_Throws()
    {
        var eventId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var act = () => EventStoreConsistencyGuard.RequireDuplicateAppendRejected(eventId, companyId, null);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate event append*")
            .WithMessage("*EventId*already exists*");
    }
}
