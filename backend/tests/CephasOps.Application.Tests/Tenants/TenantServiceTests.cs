using CephasOps.Application.Tenants.DTOs;
using CephasOps.Application.Tenants.Services;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Tenants;

public class TenantServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsTenant()
    {
        await using var context = CreateDbContext();
        var service = new TenantService(context);

        var result = await service.CreateAsync(new CreateTenantRequest
        {
            Name = "Acme Corp",
            Slug = "acme",
            IsActive = true
        });

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Acme Corp");
        result.Slug.Should().Be("acme");
        result.IsActive.Should().BeTrue();

        var bySlug = await service.GetBySlugAsync("acme");
        bySlug.Should().NotBeNull();
        bySlug!.Id.Should().Be(result.Id);
    }

    [Fact]
    public async Task ListAsync_FiltersByIsActive()
    {
        await using var context = CreateDbContext();
        var service = new TenantService(context);

        await service.CreateAsync(new CreateTenantRequest { Name = "Active", Slug = "active", IsActive = true });
        await service.CreateAsync(new CreateTenantRequest { Name = "Inactive", Slug = "inactive", IsActive = false });

        var activeOnly = await service.ListAsync(isActive: true);
        activeOnly.Should().HaveCount(1);
        activeOnly[0].Slug.Should().Be("active");

        var inactiveOnly = await service.ListAsync(isActive: false);
        inactiveOnly.Should().HaveCount(1);
        inactiveOnly[0].Slug.Should().Be("inactive");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields()
    {
        await using var context = CreateDbContext();
        var service = new TenantService(context);

        var created = await service.CreateAsync(new CreateTenantRequest
        {
            Name = "Original",
            Slug = "orig",
            IsActive = true
        });

        var updated = await service.UpdateAsync(created.Id, new UpdateTenantRequest
        {
            Name = "Updated Name",
            Slug = "updated",
            IsActive = false
        });

        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Slug.Should().Be("updated");
        updated.IsActive.Should().BeFalse();
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TenantServiceTests_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
