using CephasOps.Application.Audit.DTOs;
using CephasOps.Application.Audit.Services;
using CephasOps.Application.Auth;
using CephasOps.Application.Auth.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Auth;

/// <summary>
/// Tests for v1.4 Phase 2 suspicious activity detection. Mocks audit log events.
/// </summary>
public class SecurityAnomalyDetectionServiceTests
{
    private static SecurityActivityEntryDto Event(DateTime utc, Guid? userId, string action, string? ip = null, string? userEmail = null) => new()
    {
        Id = Guid.NewGuid(),
        Timestamp = utc,
        UserId = userId,
        UserEmail = userEmail ?? (userId.HasValue ? "user@example.com" : null),
        Action = action,
        IpAddress = ip
    };

    [Fact]
    public async Task DetectAsync_WhenExcessiveLoginFailures_ReturnsAlert()
    {
        var userId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddMinutes(-2);
        var events = new List<SecurityActivityEntryDto>();
        for (var i = 0; i < 11; i++)
            events.Add(Event(baseTime.AddSeconds(i * 5), userId, AuthEventTypes.LoginFailed, "1.2.3.4", "john@example.com"));

        var auditMock = new Mock<IAuditLogService>();
        auditMock.Setup(x => x.GetAuthEventsForDetectionAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var logger = new Mock<ILogger<SecurityAnomalyDetectionService>>();
        var service = new SecurityAnomalyDetectionService(auditMock.Object, logger.Object);

        var result = await service.DetectAsync(cancellationToken: default);

        result.Should().ContainSingle(a => a.AlertType == SecurityAlertTypes.ExcessiveLoginFailures && a.UserId == userId);
        result.First(a => a.AlertType == SecurityAlertTypes.ExcessiveLoginFailures).EventCount.Should().BeGreaterThan(SecurityDetectionRules.ExcessiveLoginFailuresThreshold);
    }

    [Fact]
    public async Task DetectAsync_WhenLoginFailuresUnderThreshold_ReturnsNoExcessiveAlert()
    {
        var userId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddMinutes(-2);
        var events = new List<SecurityActivityEntryDto>();
        for (var i = 0; i < 5; i++)
            events.Add(Event(baseTime.AddSeconds(i), userId, AuthEventTypes.LoginFailed, "1.2.3.4", "jane@example.com"));

        var auditMock = new Mock<IAuditLogService>();
        auditMock.Setup(x => x.GetAuthEventsForDetectionAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var logger = new Mock<ILogger<SecurityAnomalyDetectionService>>();
        var service = new SecurityAnomalyDetectionService(auditMock.Object, logger.Object);

        var result = await service.DetectAsync(cancellationToken: default);

        result.Should().NotContain(a => a.AlertType == SecurityAlertTypes.ExcessiveLoginFailures);
    }

    [Fact]
    public async Task DetectAsync_WhenPasswordResetAbuse_ReturnsAlert()
    {
        var userId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddMinutes(-5);
        var events = new List<SecurityActivityEntryDto>
        {
            Event(baseTime, userId, AuthEventTypes.PasswordResetRequested, "1.2.3.4", "u@example.com"),
            Event(baseTime.AddMinutes(2), userId, AuthEventTypes.PasswordResetRequested, "1.2.3.4", "u@example.com"),
            Event(baseTime.AddMinutes(4), userId, AuthEventTypes.PasswordResetRequested, "5.6.7.8", "u@example.com"),
            Event(baseTime.AddMinutes(6), userId, AuthEventTypes.PasswordResetRequested, "5.6.7.8", "u@example.com")
        };

        var auditMock = new Mock<IAuditLogService>();
        auditMock.Setup(x => x.GetAuthEventsForDetectionAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var logger = new Mock<ILogger<SecurityAnomalyDetectionService>>();
        var service = new SecurityAnomalyDetectionService(auditMock.Object, logger.Object);

        var result = await service.DetectAsync(cancellationToken: default);

        result.Should().ContainSingle(a => a.AlertType == SecurityAlertTypes.PasswordResetAbuse && a.UserId == userId);
        result.First(a => a.AlertType == SecurityAlertTypes.PasswordResetAbuse).EventCount.Should().BeGreaterThan(SecurityDetectionRules.PasswordResetAbuseThreshold);
    }

    [Fact]
    public async Task DetectAsync_WhenMultipleIpLogin_ReturnsAlert()
    {
        var userId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddMinutes(-3);
        var events = new List<SecurityActivityEntryDto>
        {
            Event(baseTime, userId, AuthEventTypes.LoginSuccess, "1.2.3.4", "multi@example.com"),
            Event(baseTime.AddMinutes(2), userId, AuthEventTypes.LoginSuccess, "5.6.7.8", "multi@example.com"),
            Event(baseTime.AddMinutes(4), userId, AuthEventTypes.LoginSuccess, "9.10.11.12", "multi@example.com")
        };

        var auditMock = new Mock<IAuditLogService>();
        auditMock.Setup(x => x.GetAuthEventsForDetectionAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var logger = new Mock<ILogger<SecurityAnomalyDetectionService>>();
        var service = new SecurityAnomalyDetectionService(auditMock.Object, logger.Object);

        var result = await service.DetectAsync(cancellationToken: default);

        result.Should().ContainSingle(a => a.AlertType == SecurityAlertTypes.MultipleIpLogin && a.UserId == userId);
        result.First(a => a.AlertType == SecurityAlertTypes.MultipleIpLogin).EventCount.Should().Be(3);
    }

    [Fact]
    public async Task DetectAsync_WhenNormalActivity_ReturnsNoAlerts()
    {
        var userId = Guid.NewGuid();
        var events = new List<SecurityActivityEntryDto>
        {
            Event(DateTime.UtcNow.AddMinutes(-10), userId, AuthEventTypes.LoginSuccess, "1.2.3.4", "normal@example.com"),
            Event(DateTime.UtcNow.AddMinutes(-5), userId, AuthEventTypes.TokenRefresh, "1.2.3.4", "normal@example.com")
        };

        var auditMock = new Mock<IAuditLogService>();
        auditMock.Setup(x => x.GetAuthEventsForDetectionAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var logger = new Mock<ILogger<SecurityAnomalyDetectionService>>();
        var service = new SecurityAnomalyDetectionService(auditMock.Object, logger.Object);

        var result = await service.DetectAsync(cancellationToken: default);

        result.Should().BeEmpty();
    }
}
