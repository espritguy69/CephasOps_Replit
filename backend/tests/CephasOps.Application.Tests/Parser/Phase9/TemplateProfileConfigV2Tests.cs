using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase9;

[Collection("TenantScopeTests")]
public class TemplateProfileConfigV2Tests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetProfileConfigByIdAsync_ParsesVersioningFields_WhenPresent()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var configJson = JsonSerializer.Serialize(new
        {
            profileId = templateId,
            profileName = "V2Profile",
            enabled = true,
            profileVersion = "1.2.0",
            effectiveFrom = "2025-02-01",
            changeNotes = "Added pack",
            owner = "ops@test.com",
            matchRules = new { senderDomains = new[] { "test.com" } }
        });
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = templateId,
                CompanyId = companyId,
                Name = "V2Profile",
                Code = "V2",
                IsActive = true,
                Description = TemplateProfileService.ProfileJsonPrefix + configJson
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var opt = await svc.GetProfileConfigByIdAsync(templateId);
        Assert.NotNull(opt);
        Assert.Equal("1.2.0", opt.Value.Config.ProfileVersion);
        Assert.Equal("2025-02-01", opt.Value.Config.EffectiveFrom);
        Assert.Equal("Added pack", opt.Value.Config.ChangeNotes);
        Assert.Equal("ops@test.com", opt.Value.Config.Owner);
    }

    [Fact]
    public async Task GetProfileConfigByIdAsync_MissingVersioningFields_DefaultsNull()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var configJson = "{\"profileId\":\"" + templateId + "\",\"profileName\":\"Legacy\",\"enabled\":true,\"matchRules\":{\"senderDomains\":[\"x.com\"]}}";
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = templateId,
                CompanyId = companyId,
                Name = "Legacy",
                Code = "L",
                IsActive = true,
                Description = TemplateProfileService.ProfileJsonPrefix + configJson
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var opt = await svc.GetProfileConfigByIdAsync(templateId);
        Assert.NotNull(opt);
        Assert.Null(opt.Value.Config.ProfileVersion);
        Assert.Null(opt.Value.Config.EffectiveFrom);
        Assert.Null(opt.Value.Config.Owner);
        Assert.Null(opt.Value.Config.Pack);
    }

    [Fact]
    public async Task GetProfileConfigByIdAsync_ParsesPack_WhenPresent()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var attId1 = Guid.NewGuid();
        var configJson = JsonSerializer.Serialize(new
        {
            profileId = templateId,
            profileName = "WithPack",
            enabled = false,
            profileVersion = "1.0.0",
            pack = new
            {
                packName = "Golden set",
                packDescription = "Regression pack",
                attachmentIds = new[] { attId1 },
                parseSessionIds = Array.Empty<Guid>()
            }
        });
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = templateId,
                CompanyId = companyId,
                Name = "WithPack",
                Code = "WP",
                IsActive = true,
                Description = TemplateProfileService.ProfileJsonPrefix + configJson
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var opt = await svc.GetProfileConfigByIdAsync(templateId);
        Assert.NotNull(opt);
        Assert.NotNull(opt.Value.Config.Pack);
        Assert.Equal("Golden set", opt.Value.Config.Pack.PackName);
        Assert.Equal("Regression pack", opt.Value.Config.Pack.PackDescription);
        Assert.Single(opt.Value.Config.Pack.AttachmentIds!);
        Assert.Equal(attId1, opt.Value.Config.Pack.AttachmentIds![0]);
    }

    [Fact]
    public async Task GetProfileConfigByIdAsync_NotFound_ReturnsNull()
    {
        await using var context = CreateContext();
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var opt = await svc.GetProfileConfigByIdAsync(Guid.NewGuid());
        Assert.Null(opt);
    }

    [Fact]
    public async Task GetProfileConfigByIdAsync_TemplateHasNoProfileJson_ReturnsNull()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate { Id = templateId, CompanyId = companyId, Name = "NoJson", Code = "N", IsActive = true, Description = "Just plain text" });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var opt = await svc.GetProfileConfigByIdAsync(templateId);
        Assert.Null(opt);
    }
}
