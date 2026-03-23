using CephasOps.Application.Events.Ledger;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Events;

/// <summary>
/// Ledger query service: bounded pagination (page 0 or negative does not cause negative skip).
/// </summary>
public class LedgerQueryServiceSafetyTests
{
    [Fact]
    public async Task ListAsync_WhenPageZero_ReturnsFirstPage_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "LedgerSafety_" + Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var service = new LedgerQueryService(context);

        var (items, total) = await service.ListAsync(
            companyId: null,
            entityType: null,
            entityId: null,
            ledgerFamily: null,
            fromOccurredUtc: null,
            toOccurredUtc: null,
            page: 0,
            pageSize: 20,
            CancellationToken.None);

        total.Should().Be(0);
        items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ListAsync_WhenPageNegative_ReturnsFirstPage_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "LedgerSafety_" + Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options);
        var service = new LedgerQueryService(context);

        var (items, total) = await service.ListAsync(
            companyId: null,
            entityType: null,
            entityId: null,
            ledgerFamily: null,
            fromOccurredUtc: null,
            toOccurredUtc: null,
            page: -1,
            pageSize: 10,
            CancellationToken.None);

        total.Should().Be(0);
        items.Should().NotBeNull().And.BeEmpty();
    }
}
