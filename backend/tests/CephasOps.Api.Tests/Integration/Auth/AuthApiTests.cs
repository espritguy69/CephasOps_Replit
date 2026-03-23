using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CephasOps.Api.Tests.Infrastructure;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Domain.Users.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CephasOps.Api.Tests.Integration.Auth;

/// <summary>
/// Phase 1 API integration tests for auth: login success and login invalid credentials.
/// Uses real POST /api/auth/login; login success requires a seeded user (InMemory DB).
/// </summary>
[Collection("InventoryIntegration")]
public class AuthApiTests : IClassFixture<CephasOpsWebApplicationFactory>
{
    private const string TestUserEmail = "apitest@example.com";
    private const string TestUserPassword = "TestPassword123!";

    private readonly CephasOpsWebApplicationFactory _factory;

    public AuthApiTests(CephasOpsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndAccessToken()
    {
        await SeedLoginUserAsync(TestUserEmail, TestUserPassword);

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new
        {
            Email = TestUserEmail,
            Password = TestUserPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var success = root.TryGetProperty("Success", out var s1) && s1.GetBoolean()
                     || root.TryGetProperty("success", out var s2) && s2.GetBoolean();
        success.Should().BeTrue("login response should indicate success");

        var data = root.TryGetProperty("Data", out var d) ? d : root.TryGetProperty("data", out var d2) ? d2 : default;
        data.ValueKind.Should().Be(JsonValueKind.Object);
        var tokenEl = data.TryGetProperty("AccessToken", out var t1) ? t1 : data.TryGetProperty("accessToken", out var t2) ? t2 : default;
        tokenEl.ValueKind.Should().Be(JsonValueKind.String, "response data should contain access token");
        tokenEl.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SeedLoginUserAsync(string email, string password)
    {
        TenantSafetyGuard.EnterPlatformBypass();
        try
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                return;

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Member" && r.Scope == "Global");
            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Member",
                    Scope = "Global"
                };
                db.Roles.Add(role);
                await db.SaveChangesAsync();
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = "API Test User",
                Email = email,
                PasswordHash = hasher.HashPassword(password),
                IsActive = true,
                CompanyId = null,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                CompanyId = null,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        finally
        {
            TenantSafetyGuard.ExitPlatformBypass();
        }
    }
}
