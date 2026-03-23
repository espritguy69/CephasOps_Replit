using CephasOps.Application.Notifications;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Domain.Notifications;
using CephasOps.Domain.Notifications.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Notifications;

/// <summary>
/// Tests for NotificationDispatchRequestService (tenant-boundary: null CompanyId handling).
/// </summary>
public class NotificationDispatchRequestServiceTests
{
    [Fact]
    public async Task RequestOrderStatusNotificationAsync_WhenOrderAndParamHaveNoCompanyId_DoesNotCallAddAsync()
    {
        var orderId = Guid.NewGuid();
        var mockOrderService = new Mock<IOrderService>();
        mockOrderService
            .Setup(x => x.GetOrderByIdAsync(orderId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderDto
            {
                Id = orderId,
                CompanyId = null,
                CustomerPhone = "+60123456789",
                Status = "Assigned"
            });

        var mockGlobalSettings = new Mock<IGlobalSettingsService>();
        mockGlobalSettings.Setup(x => x.GetValueAsync<bool>("SMS_AutoSendOnStatusChange", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockGlobalSettings.Setup(x => x.GetValueAsync<bool>("WhatsApp_AutoSendOnStatusChange", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var mockDispatchStore = new Mock<INotificationDispatchStore>();
        mockDispatchStore.Setup(x => x.ExistsByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = new NotificationDispatchRequestService(
            mockOrderService.Object,
            mockGlobalSettings.Object,
            mockDispatchStore.Object,
            new Mock<ILogger<NotificationDispatchRequestService>>().Object);

        await service.RequestOrderStatusNotificationAsync(
            orderId,
            "Assigned",
            companyId: null,
            sourceEventId: Guid.NewGuid(),
            correlationId: null,
            causationId: null,
            CancellationToken.None);

        mockDispatchStore.Verify(x => x.AddAsync(It.IsAny<NotificationDispatch>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
