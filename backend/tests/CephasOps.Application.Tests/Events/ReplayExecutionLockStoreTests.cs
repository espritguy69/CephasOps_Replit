using CephasOps.Application.Events.Replay;
using CephasOps.Domain.Events;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>Uses SQLite in-memory so ReplayExecutionLockStore.ExecuteUpdateAsync (relational) is supported.</summary>
public class ReplayExecutionLockStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ReplayExecutionLockStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenNoLock_ReturnsTrue_AndInsertsRow()
    {
        var store = new ReplayExecutionLockStore(_context, Logger());
        var companyId = Guid.NewGuid();
        var opId = Guid.NewGuid();

        var acquired = await store.TryAcquireAsync(companyId, opId);

        acquired.Should().BeTrue();
        var row = await _context.ReplayExecutionLock.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.ReleasedAtUtc == null);
        row.Should().NotBeNull();
        row!.ReplayOperationId.Should().Be(opId);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenActiveLockExists_ReturnsFalse()
    {
        var store = new ReplayExecutionLockStore(_context, Logger());
        var companyId = Guid.NewGuid();
        var op1 = Guid.NewGuid();
        await store.TryAcquireAsync(companyId, op1);

        var op2 = Guid.NewGuid();
        var acquired = await store.TryAcquireAsync(companyId, op2);

        acquired.Should().BeFalse();
        var row = await _context.ReplayExecutionLock.FirstAsync(e => e.CompanyId == companyId && e.ReleasedAtUtc == null);
        row.ReplayOperationId.Should().Be(op1);
    }

    [Fact]
    public async Task ReleaseAsync_ClearsLock_SoSecondAcquireSucceeds()
    {
        var store = new ReplayExecutionLockStore(_context, Logger());
        var companyId = Guid.NewGuid();
        var op1 = Guid.NewGuid();
        await store.TryAcquireAsync(companyId, op1);
        await store.ReleaseAsync(companyId, op1);

        var op2 = Guid.NewGuid();
        var acquired = await store.TryAcquireAsync(companyId, op2);

        acquired.Should().BeTrue();
        var active = await _context.ReplayExecutionLock.FirstAsync(e => e.CompanyId == companyId && e.ReleasedAtUtc == null);
        active.ReplayOperationId.Should().Be(op2);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenStaleLockExists_ReclaimsAndReturnsTrue()
    {
        var companyId = Guid.NewGuid();
        var oldOpId = Guid.NewGuid();
        _context.ReplayExecutionLock.Add(new ReplayExecutionLock
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ReplayOperationId = oldOpId,
            AcquiredAtUtc = DateTime.UtcNow.AddHours(-3),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-1)
        });
        await _context.SaveChangesAsync();

        var store = new ReplayExecutionLockStore(_context, Logger());
        var newOpId = Guid.NewGuid();
        var acquired = await store.TryAcquireAsync(companyId, newOpId);

        acquired.Should().BeTrue();
        var row = await _context.ReplayExecutionLock.FirstAsync(e => e.CompanyId == companyId && e.ReleasedAtUtc == null);
        row.ReplayOperationId.Should().Be(newOpId);
    }

    private static ILogger<ReplayExecutionLockStore> Logger()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
        return services.BuildServiceProvider().GetRequiredService<ILogger<ReplayExecutionLockStore>>();
    }
}
