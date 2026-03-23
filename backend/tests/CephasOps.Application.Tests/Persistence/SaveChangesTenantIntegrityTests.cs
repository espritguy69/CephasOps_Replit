using CephasOps.Domain.Orders.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Persistence;

/// <summary>
/// Tests for SaveChanges tenant-integrity validation: same-tenant saves succeed,
/// mismatched CompanyId (Added/Modified/Deleted) fails, platform bypass still works.
/// </summary>
[Collection("SaveChangesTenantIntegrity")]
public class SaveChangesTenantIntegrityTests : IDisposable
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task SaveChangesAsync_SameTenant_AddedEntity_Succeeds()
    {
        var companyId = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyId;
        try
        {
            await using var context = CreateContext();
            context.OrderTypes.Add(new OrderType
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Test",
                Code = "T",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            var act = () => context.SaveChangesAsync(CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task SaveChangesAsync_AddedEntity_WithMismatchedCompanyId_Throws()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        TenantScope.CurrentTenantId = companyA;
        try
        {
            await using var context = CreateContext();
            context.OrderTypes.Add(new OrderType
            {
                Id = Guid.NewGuid(),
                CompanyId = companyB,
                Name = "Test",
                Code = "T",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            var act = () => context.SaveChangesAsync(CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tenant integrity*CompanyId*does not match current tenant*");
        }
        finally
        {
            TenantScope.CurrentTenantId = null;
        }
    }

    [Fact]
    public async Task SaveChangesAsync_ModifiedEntity_WithMismatchedCompanyId_Throws()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            TenantSafetyGuard.EnterPlatformBypass();
            try
            {
                seedContext.OrderTypes.Add(new OrderType
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyB,
                    Name = "Other",
                    Code = "O",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await seedContext.SaveChangesAsync(CancellationToken.None);
            }
            finally
            {
                TenantSafetyGuard.ExitPlatformBypass();
            }
        }

        await using (var context = new ApplicationDbContext(options))
        {
            var orderType = await context.OrderTypes.FirstAsync();
            TenantScope.CurrentTenantId = companyA;
            try
            {
                orderType.Name = "Hacked";
                var act = () => context.SaveChangesAsync(CancellationToken.None);
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*Tenant integrity*modify or delete*does not belong to current tenant*");
            }
            finally
            {
                TenantScope.CurrentTenantId = null;
            }
        }
    }

    [Fact]
    public async Task SaveChangesAsync_DeletedEntity_WithMismatchedCompanyId_Throws()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var orderTypeId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            TenantSafetyGuard.EnterPlatformBypass();
            try
            {
                seedContext.OrderTypes.Add(new OrderType
                {
                    Id = orderTypeId,
                    CompanyId = companyB,
                    Name = "Other",
                    Code = "O",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await seedContext.SaveChangesAsync(CancellationToken.None);
            }
            finally
            {
                TenantSafetyGuard.ExitPlatformBypass();
            }
        }

        await using (var context = new ApplicationDbContext(options))
        {
            var orderType = await context.OrderTypes.FirstAsync(ot => ot.Id == orderTypeId);
            TenantScope.CurrentTenantId = companyA;
            try
            {
                context.OrderTypes.Remove(orderType);
                var act = () => context.SaveChangesAsync(CancellationToken.None);
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*Tenant integrity*modify or delete*does not belong to current tenant*");
            }
            finally
            {
                TenantScope.CurrentTenantId = null;
            }
        }
    }

    [Fact]
    public async Task SaveChangesAsync_PlatformBypass_AllowsAnyCompanyId()
    {
        var companyId = Guid.NewGuid();
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            await using var context = CreateContext();
            context.OrderTypes.Add(new OrderType
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = "Test",
                Code = "T",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            var act = () => context.SaveChangesAsync(CancellationToken.None);
            await act.Should().NotThrowAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }

    [Fact]
    public async Task SaveChangesAsync_NoTenantContext_AndTenantScopedEntity_Throws()
    {
        TenantScope.CurrentTenantId = null;
        await using var context = CreateContext();
        context.OrderTypes.Add(new OrderType
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Name = "Test",
            Code = "T",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var act = () => context.SaveChangesAsync(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot save tenant-scoped entity without tenant context*");
    }
}
