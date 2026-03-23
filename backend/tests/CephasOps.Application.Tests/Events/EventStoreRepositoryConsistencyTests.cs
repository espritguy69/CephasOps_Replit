using CephasOps.Application.Events;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// EventStore append path consistency: valid append succeeds; company mismatch for same entity stream fails.
/// </summary>
public class EventStoreRepositoryConsistencyTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventStoreRepository _repo;
    private readonly Guid _orderId;
    private readonly Guid _companyA;
    private readonly Guid _companyB;

    public EventStoreRepositoryConsistencyTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "EventStoreConsistency_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repo = new EventStoreRepository(_context);
        _orderId = Guid.NewGuid();
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
    }

    [Fact]
    public async Task AppendAsync_DuplicateEventId_ThrowsBeforeSave()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var eventId = Guid.NewGuid();
            var evt = new OrderCompletedEvent
            {
                EventId = eventId,
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };
            await _repo.AppendAsync(evt);

            var evt2 = new OrderCompletedEvent
            {
                EventId = eventId,
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = Guid.NewGuid(),
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };
            var act = () => _repo.AppendAsync(evt2);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Duplicate event append*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public async Task AppendAsync_ValidEventWithEntityContextAndCompany_Succeeds()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var evt = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };

            await _repo.AppendAsync(evt);

            var entry = await _context.EventStore.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == evt.EventId);
            entry.Should().NotBeNull();
            entry!.EntityType.Should().Be("Order");
            entry.EntityId.Should().Be(_orderId);
            entry.CompanyId.Should().Be(_companyA);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public async Task AppendAsync_SameEntityStreamDifferentCompany_ThrowsBeforeSave()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var evt1 = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };
            await _repo.AppendAsync(evt1);

            TenantScope.CurrentTenantId = _companyB;
            var evt2 = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyB,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };

            var act = () => _repo.AppendAsync(evt2);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Company mismatch*event stream*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public async Task AppendAsync_SameEntityStreamSameCompany_Succeeds()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var evt1 = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };
            await _repo.AppendAsync(evt1);

            var evt2 = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };

            await _repo.AppendAsync(evt2);

            var count = await _context.EventStore.AsNoTracking().CountAsync(e => e.EntityId == _orderId && e.EntityType == "Order");
            count.Should().Be(2);
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public void AppendInCurrentTransaction_EventWithEntityContextButNoCompany_ThrowsBeforeAdd()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var evt = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = null,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };

            var act = () => _repo.AppendInCurrentTransaction(evt);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*CompanyId*required*entity context*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public void AppendInCurrentTransaction_SameEntityStreamDifferentCompanyInSameTransaction_ThrowsBeforeAdd()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var evt1 = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };
            _repo.AppendInCurrentTransaction(evt1);

            var evt2 = new OrderCompletedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyB,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };

            var act = () => _repo.AppendInCurrentTransaction(evt2);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Company mismatch*event stream*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    [Fact]
    public void AppendInCurrentTransaction_EventWithSelfReferenceParent_ThrowsBeforeAdd()
    {
        var previous = TenantScope.CurrentTenantId;
        try
        {
            TenantScope.CurrentTenantId = _companyA;
            var eventId = Guid.NewGuid();
            var evt = new OrderCompletedEvent
            {
                EventId = eventId,
                EventType = PlatformEventTypes.OrderCompleted,
                OrderId = _orderId,
                CompanyId = _companyA,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "Test"
            };
            evt.ParentEventId = eventId;

            var act = () => _repo.AppendInCurrentTransaction(evt);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*ParentEventId*cannot equal EventId*self-reference*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previous;
        }
    }

    public void Dispose() => _context.Dispose();
}
