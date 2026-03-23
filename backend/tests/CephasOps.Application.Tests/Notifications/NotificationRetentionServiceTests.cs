using CephasOps.Application.Notifications;
using CephasOps.Application.Notifications.Services;
using CephasOps.Domain.Notifications.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Notifications;

/// <summary>
/// Tests for NotificationRetentionService (Phase 7).
/// Tenant-scoped Notification entities require TenantScope or bypass when saving; tests set scope for setup.
/// </summary>
[Collection("TenantScopeTests")]
public class NotificationRetentionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationRetentionService _service;
    private readonly Guid _companyA;
    private readonly Guid _companyB;

    public NotificationRetentionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "Retention_" + Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new NotificationRetentionService(_context, new Mock<ILogger<NotificationRetentionService>>().Object);
        _companyA = Guid.NewGuid();
        _companyB = Guid.NewGuid();
    }

    [Fact]
    public async Task RunRetentionAsync_ArchivesOldReadUnread_AndDeletesOldArchived()
    {
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var veryOldArchivedDate = DateTime.UtcNow.AddDays(-400);

        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyA;
        try
        {
            _context.Notifications.AddRange(
                CreateNotification("Unread", oldDate, null, _companyA),
                CreateNotification("Read", oldDate, null, _companyA),
                CreateNotification("Archived", veryOldArchivedDate, veryOldArchivedDate, _companyA));
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var result = await _service.RunRetentionAsync(archiveDays: 90, deleteDays: 365, companyId: null);

        result.ArchivedCount.Should().Be(2);
        result.DeletedCount.Should().Be(1);

        var unread = await _context.Notifications.FirstOrDefaultAsync(n => n.Status == "Unread");
        unread.Should().BeNull();
        var archived = await _context.Notifications.Where(n => n.Status == "Archived").ToListAsync();
        archived.Should().HaveCount(2);
        var any = await _context.Notifications.CountAsync();
        any.Should().Be(2);
    }

    [Fact]
    public async Task RunRetentionAsync_WhenCompanyIdSet_ScopesToCompany()
    {
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyA; // guard: need scope to add tenant-scoped Notification
        try
        {
            _context.Notifications.Add(CreateNotification("Read", oldDate, null, _companyA));
            _context.Notifications.Add(CreateNotification("Read", oldDate, null, _companyB));
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var result = await _service.RunRetentionAsync(archiveDays: 90, deleteDays: 365, companyId: _companyA);

        result.ArchivedCount.Should().Be(1);
        var archivedA = await _context.Notifications.CountAsync(n => n.CompanyId == _companyA && n.Status == "Archived");
        var archivedB = await _context.Notifications.CountAsync(n => n.CompanyId == _companyB && n.Status == "Archived");
        archivedA.Should().Be(1);
        archivedB.Should().Be(0);
    }

    [Fact]
    public async Task RunRetentionAsync_DoesNotArchiveRecent()
    {
        var recentDate = DateTime.UtcNow.AddDays(-10);
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyA;
        try
        {
            _context.Notifications.Add(CreateNotification("Read", recentDate, null, _companyA));
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var result = await _service.RunRetentionAsync(archiveDays: 90, deleteDays: 365, companyId: null);

        result.ArchivedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
        var n = await _context.Notifications.FirstAsync();
        n.Status.Should().Be("Read");
    }

    [Fact]
    public async Task RunRetentionAsync_DoesNotDeleteRecentlyArchived()
    {
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var archivedRecently = DateTime.UtcNow.AddDays(-5);
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyA;
        try
        {
            _context.Notifications.Add(CreateNotification("Archived", oldDate, archivedRecently, _companyA));
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var result = await _service.RunRetentionAsync(archiveDays: 90, deleteDays: 365, companyId: null);

        result.ArchivedCount.Should().Be(0);
        result.DeletedCount.Should().Be(0);
        (await _context.Notifications.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RunRetentionAsync_InvalidDays_UseDefaults()
    {
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyA;
        try
        {
            _context.Notifications.Add(CreateNotification("Read", oldDate, null, _companyA));
            await _context.SaveChangesAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }

        var result = await _service.RunRetentionAsync(archiveDays: 0, deleteDays: -1, companyId: null);

        result.ArchivedCount.Should().Be(1);
        (await _context.Notifications.FirstAsync()).Status.Should().Be("Archived");
    }

    [Fact]
    public async Task RunRetentionAsync_WhenCompanyIdNull_RestoresTenantScopeAfterRun()
    {
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyA;
        try
        {
            await _service.RunRetentionAsync(archiveDays: 90, deleteDays: 365, companyId: null);
            TenantScope.CurrentTenantId.Should().Be(_companyA, "RunRetentionAsync(companyId: null) must restore tenant scope in finally");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    [Fact]
    public async Task RunRetentionAsync_WhenCompanyIdSet_RestoresTenantScopeAfterRun()
    {
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyB;
        try
        {
            await _service.RunRetentionAsync(archiveDays: 90, deleteDays: 365, companyId: _companyA);
            TenantScope.CurrentTenantId.Should().Be(_companyB, "RunRetentionAsync(companyId set) must restore tenant scope in finally");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    private static Notification CreateNotification(string status, DateTime createdAt, DateTime? archivedAt, Guid? companyId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = Guid.NewGuid(),
            Type = "Test",
            Status = status,
            Title = "T",
            Message = "M",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            ArchivedAt = archivedAt
        };
    }

    public void Dispose() => _context?.Dispose();
}
