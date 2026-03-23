using CephasOps.Api.Authorization;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Domain.Authorization;
using FluentAssertions;
using Moq;
using Xunit;

namespace CephasOps.Api.Tests.Authorization;

public class FieldLevelSecurityFilterTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserPermissionProvider> _userPermissionProvider = new();

    private FieldLevelSecurityFilter CreateSut() =>
        new FieldLevelSecurityFilter(_currentUserService.Object, _userPermissionProvider.Object);

    [Fact]
    public async Task ApplyOrderDtoAsync_WhenUserHasOrdersViewPrice_LeavesAmountsUnchanged()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(x => x.IsSuperAdmin).Returns(false);
        _currentUserService.Setup(x => x.UserId).Returns(userId);
        _userPermissionProvider
            .Setup(x => x.GetPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { PermissionCatalog.OrdersViewPrice });

        var dto = new OrderDto
        {
            Id = Guid.NewGuid(),
            RevenueAmount = 100m,
            PayoutAmount = 60m,
            ProfitAmount = 40m
        };

        var sut = CreateSut();
        await sut.ApplyOrderDtoAsync(dto);

        dto.RevenueAmount.Should().Be(100m);
        dto.PayoutAmount.Should().Be(60m);
        dto.ProfitAmount.Should().Be(40m);
    }

    [Fact]
    public async Task ApplyOrderDtoAsync_WhenUserLacksOrdersViewPrice_MasksAmounts()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(x => x.IsSuperAdmin).Returns(false);
        _currentUserService.Setup(x => x.UserId).Returns(userId);
        _userPermissionProvider
            .Setup(x => x.GetPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { PermissionCatalog.OrdersView }); // no orders.view.price

        var dto = new OrderDto
        {
            Id = Guid.NewGuid(),
            RevenueAmount = 100m,
            PayoutAmount = 60m,
            ProfitAmount = 40m
        };

        var sut = CreateSut();
        await sut.ApplyOrderDtoAsync(dto);

        dto.RevenueAmount.Should().BeNull();
        dto.PayoutAmount.Should().BeNull();
        dto.ProfitAmount.Should().BeNull();
    }

    [Fact]
    public async Task ApplyOrderDtoAsync_WhenSuperAdmin_LeavesAmountsUnchanged()
    {
        _currentUserService.Setup(x => x.IsSuperAdmin).Returns(true);
        _userPermissionProvider.Setup(x => x.GetPermissionNamesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());

        var dto = new OrderDto
        {
            Id = Guid.NewGuid(),
            RevenueAmount = 100m,
            PayoutAmount = 60m,
            ProfitAmount = 40m
        };

        var sut = CreateSut();
        await sut.ApplyOrderDtoAsync(dto);

        dto.RevenueAmount.Should().Be(100m);
        dto.PayoutAmount.Should().Be(60m);
        dto.ProfitAmount.Should().Be(40m);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenSuperAdmin_ReturnsTrue()
    {
        _currentUserService.Setup(x => x.IsSuperAdmin).Returns(true);

        var sut = CreateSut();
        var result = await sut.HasPermissionAsync("any.permission");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasPermission_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(x => x.IsSuperAdmin).Returns(false);
        _currentUserService.Setup(x => x.UserId).Returns(userId);
        _userPermissionProvider
            .Setup(x => x.GetPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { PermissionCatalog.OrdersViewPrice });

        var sut = CreateSut();
        var result = await sut.HasPermissionAsync(PermissionCatalog.OrdersViewPrice);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserLacksPermission_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(x => x.IsSuperAdmin).Returns(false);
        _currentUserService.Setup(x => x.UserId).Returns(userId);
        _userPermissionProvider
            .Setup(x => x.GetPermissionNamesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { PermissionCatalog.OrdersView });

        var sut = CreateSut();
        var result = await sut.HasPermissionAsync(PermissionCatalog.OrdersViewPrice);

        result.Should().BeFalse();
    }
}
