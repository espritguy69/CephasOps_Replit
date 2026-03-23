using CephasOps.Application.Audit;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Audit;

public class TenantActivityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantActivityService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public TenantActivityServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new TenantActivityService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task RecordAsync_AddsEvent()
    {
        await _context.Tenants.AddAsync(new CephasOps.Domain.Tenants.Entities.Tenant { Id = _tenantId, Name = "T", Slug = "t", IsActive = true });
        await _context.SaveChangesAsync();

        await _service.RecordAsync(_tenantId, "OrderCreated", "Order", Guid.NewGuid(), "Order created", null, null);

        var count = await _context.TenantActivityEvents.CountAsync(e => e.TenantId == _tenantId);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetTimelineAsync_ReturnsOnlyRequestedTenant()
    {
        await _context.Tenants.AddAsync(new CephasOps.Domain.Tenants.Entities.Tenant { Id = _tenantId, Name = "T", Slug = "t", IsActive = true });
        await _context.SaveChangesAsync();
        await _service.RecordAsync(_tenantId, "Login", null, null, "User login");
        var otherTenant = Guid.NewGuid();
        await _context.Tenants.AddAsync(new CephasOps.Domain.Tenants.Entities.Tenant { Id = otherTenant, Name = "O", Slug = "o", IsActive = true });
        await _context.SaveChangesAsync();
        await _service.RecordAsync(otherTenant, "OrderCreated");

        var timeline = await _service.GetTimelineAsync(_tenantId, 100);
        timeline.Should().ContainSingle(e => e.EventType == "Login");
        timeline.Should().NotContain(e => e.EventType == "OrderCreated");
    }
}
