using CephasOps.Application.Notifications.DTOs;
using CephasOps.Application.Notifications.Services;
using CephasOps.Domain.Notifications;
using CephasOps.Domain.Notifications.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Notifications;

/// <summary>
/// Unit tests for NotificationService (includes Phase 6 email dispatch).
/// </summary>
[Collection("TenantScopeTests")]
public class NotificationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<INotificationDispatchStore> _mockDispatchStore;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _service;
    private readonly Guid _companyId;
    private readonly Guid _userId;
    private readonly Guid? _previousTenantId;

    public NotificationServiceTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockDispatchStore = new Mock<INotificationDispatchStore>();
        _mockDispatchStore.Setup(x => x.ExistsByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockLogger = new Mock<ILogger<NotificationService>>();
        _service = new NotificationService(_dbContext, _mockDispatchStore.Object, _mockLogger.Object);
    }

    #region GetNotifications Tests

    [Fact]
    public async Task GetNotificationsAsync_NoFilters_ReturnsAllNotifications()
    {
        // Arrange
        await CreateTestNotificationAsync("Order", "Unread");
        await CreateTestNotificationAsync("System", "Read");
        await CreateTestNotificationAsync("Order", "Unread", userId: Guid.NewGuid()); // Different user

        // Act
        var result = await _service.GetNotificationsAsync(_userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.UserId == _userId);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithStatusFilter_ReturnsFilteredNotifications()
    {
        // Arrange
        await CreateTestNotificationAsync("Order", "Unread");
        await CreateTestNotificationAsync("System", "Read");
        await CreateTestNotificationAsync("Order", "Unread");

        // Act
        var result = await _service.GetNotificationsAsync(_userId, status: "Unread");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.Status == "Unread");
    }

    [Fact]
    public async Task GetNotificationsAsync_WithTypeFilter_ReturnsFilteredNotifications()
    {
        // Arrange
        await CreateTestNotificationAsync("Order", "Unread");
        await CreateTestNotificationAsync("System", "Unread");
        await CreateTestNotificationAsync("Order", "Unread");

        // Act
        var result = await _service.GetNotificationsAsync(_userId, type: "Order");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(n => n.Type == "Order");
    }

    [Fact]
    public async Task GetNotificationsAsync_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        await CreateTestNotificationAsync("Order", "Unread");
        await CreateTestNotificationAsync("System", "Unread");
        await CreateTestNotificationAsync("Order", "Unread");

        // Act
        var result = await _service.GetNotificationsAsync(_userId, limit: 2);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotificationsAsync_NoNotifications_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetNotificationsAsync(_userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetNotificationById Tests

    [Fact]
    public async Task GetNotificationByIdAsync_ValidId_ReturnsNotification()
    {
        // Arrange
        var notification = await CreateTestNotificationAsync("Order", "Unread");

        // Act
        var result = await _service.GetNotificationByIdAsync(notification.Id, _userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(notification.Id);
        result.Type.Should().Be("Order");
    }

    [Fact]
    public async Task GetNotificationByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetNotificationByIdAsync(Guid.NewGuid(), _userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotificationByIdAsync_DifferentUser_ReturnsNull()
    {
        // Arrange
        var notification = await CreateTestNotificationAsync("Order", "Unread", userId: Guid.NewGuid());

        // Act
        var result = await _service.GetNotificationByIdAsync(notification.Id, _userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region MarkNotificationStatus Tests

    [Fact]
    public async Task MarkNotificationStatusAsync_MarkAsRead_SetsReadStatus()
    {
        // Arrange
        var notification = await CreateTestNotificationAsync("Order", "Unread");
        var dto = new MarkNotificationStatusDto { IsRead = true };

        // Act
        var result = await _service.MarkNotificationStatusAsync(notification.Id, dto, _userId);

        // Assert
        result.Status.Should().Be("Read");
        result.ReadAt.Should().NotBeNull();
        result.ReadByUserId.Should().Be(_userId);
    }

    [Fact]
    public async Task MarkNotificationStatusAsync_MarkAsArchived_SetsArchivedStatus()
    {
        // Arrange
        var notification = await CreateTestNotificationAsync("Order", "Unread");
        var dto = new MarkNotificationStatusDto { IsArchived = true };

        // Act
        var result = await _service.MarkNotificationStatusAsync(notification.Id, dto, _userId);

        // Assert
        result.Status.Should().Be("Archived");
        result.ArchivedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkNotificationStatusAsync_InvalidId_ThrowsException()
    {
        // Arrange
        var dto = new MarkNotificationStatusDto { IsRead = true };

        // Act
        var act = async () => await _service.MarkNotificationStatusAsync(Guid.NewGuid(), dto, _userId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region GetUnreadCount Tests (via GetNotifications)

    [Fact]
    public async Task GetNotificationsAsync_WithUnreadFilter_ReturnsUnreadCount()
    {
        // Arrange
        await CreateTestNotificationAsync("Order", "Unread");
        await CreateTestNotificationAsync("System", "Unread");
        await CreateTestNotificationAsync("Order", "Read");

        // Act
        var result = await _service.GetNotificationsAsync(_userId, status: "Unread");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNotificationsAsync_NoUnreadNotifications_ReturnsEmpty()
    {
        // Arrange
        await CreateTestNotificationAsync("Order", "Read");
        await CreateTestNotificationAsync("System", "Read");

        // Act
        var result = await _service.GetNotificationsAsync(_userId, status: "Unread");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateNotificationAsync (Phase 6 email dispatch)

    [Fact]
    public async Task CreateNotificationAsync_WithInAppOnly_DoesNotCallDispatchStore()
    {
        _dbContext.Users.Add(new CephasOps.Domain.Users.Entities.User
        {
            Id = _userId,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var dto = new CreateNotificationDto
        {
            CompanyId = _companyId,
            UserId = _userId,
            Type = "Order",
            Title = "Test",
            Message = "Body",
            DeliveryChannels = "InApp"
        };

        await _service.CreateNotificationAsync(dto);

        _mockDispatchStore.Verify(x => x.AddAsync(It.IsAny<NotificationDispatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmailChannel_EnqueuesEmailDispatch_AndPreservesCompanyId()
    {
        _dbContext.Users.Add(new CephasOps.Domain.Users.Entities.User
        {
            Id = _userId,
            Name = "Test User",
            Email = "user@company.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var dto = new CreateNotificationDto
        {
            CompanyId = _companyId,
            UserId = _userId,
            Type = "VipEmail",
            Title = "VIP Email",
            Message = "Preview text",
            DeliveryChannels = "InApp,Email"
        };

        var result = await _service.CreateNotificationAsync(dto);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        _mockDispatchStore.Verify(x => x.ExistsByIdempotencyKeyAsync(It.Is<string>(k => k == $"{_companyId:N}:{result.Id}:Email"), It.IsAny<CancellationToken>()), Times.Once);
        _mockDispatchStore.Verify(x => x.AddAsync(It.Is<NotificationDispatch>(d =>
            d.Channel == "Email" &&
            d.Target == "user@company.com" &&
            d.CompanyId == _companyId &&
            d.IdempotencyKey == $"{_companyId:N}:{result.Id}:Email" &&
            d.Status == "Pending" &&
            d.PayloadJson != null && d.PayloadJson.Contains("VIP Email") && d.PayloadJson.Contains("Preview text")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmailChannel_UserHasNoEmail_DoesNotEnqueue()
    {
        _dbContext.Users.Add(new CephasOps.Domain.Users.Entities.User
        {
            Id = _userId,
            Name = "No Email User",
            Email = "",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var dto = new CreateNotificationDto
        {
            CompanyId = _companyId,
            UserId = _userId,
            Type = "Order",
            Title = "Test",
            Message = "Body",
            DeliveryChannels = "InApp,Email"
        };

        await _service.CreateNotificationAsync(dto);

        _mockDispatchStore.Verify(x => x.AddAsync(It.IsAny<NotificationDispatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNotificationAsync_WhenCompanyIdNullAndNoTenantScope_Throws()
    {
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = null;
        try
        {
            var dto = new CreateNotificationDto
            {
                CompanyId = null,
                UserId = _userId,
                Type = "Test",
                Title = "T",
                Message = "M"
            };
            var act = () => _service.CreateNotificationAsync(dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*CompanyId*required*");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    [Fact]
    public async Task CreateNotificationAsync_WhenCompanyIdNullButTenantScopeSet_UsesTenantScope()
    {
        var previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId;
        try
        {
            _dbContext.Users.Add(new CephasOps.Domain.Users.Entities.User
            {
                Id = _userId,
                Name = "Test User",
                Email = "test@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            var dto = new CreateNotificationDto
            {
                CompanyId = null,
                UserId = _userId,
                Type = "Test",
                Title = "T",
                Message = "M"
            };
            var result = await _service.CreateNotificationAsync(dto);
            result.CompanyId.Should().Be(_companyId, "CreateNotificationAsync should use TenantScope when dto.CompanyId is null");
        }
        finally
        {
            TenantScope.CurrentTenantId = previousTenantId;
        }
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmailChannel_IdempotencyKeyAlreadyExists_DoesNotCallAddAsync()
    {
        _dbContext.Users.Add(new CephasOps.Domain.Users.Entities.User
        {
            Id = _userId,
            Name = "Test User",
            Email = "user@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _mockDispatchStore.Setup(x => x.ExistsByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dto = new CreateNotificationDto
        {
            CompanyId = _companyId,
            UserId = _userId,
            Type = "Order",
            Title = "Test",
            Message = "Body",
            DeliveryChannels = "InApp,Email"
        };

        await _service.CreateNotificationAsync(dto);

        _mockDispatchStore.Verify(x => x.AddAsync(It.IsAny<NotificationDispatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private async Task<Notification> CreateTestNotificationAsync(
        string type,
        string status,
        Guid? userId = null,
        Guid? companyId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? _companyId,
            UserId = userId ?? _userId,
            Type = type,
            Status = status,
            Priority = "Normal",
            Title = $"Test {type} Notification",
            Message = "Test message",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();
        return notification;
    }

    #endregion

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _dbContext?.Dispose();
    }
}

