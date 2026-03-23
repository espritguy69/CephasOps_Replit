using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase8;

[Collection("TenantScopeTests")]
public class TemplateProfileServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetBestMatchProfileAsync_NoTemplates_ReturnsNull()
    {
        await using var context = CreateContext();
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var result = await svc.GetBestMatchProfileAsync("a@time.com", "FTTH Order", "order.xlsx", null, null);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBestMatchProfileAsync_InvalidJsonInDescription_IgnoresTemplate()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = templateId,
                CompanyId = companyId,
                Name = "T1",
                Code = "T1",
                IsActive = true,
                Description = "PROFILE_JSON: { invalid json here "
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var result = await svc.GetBestMatchProfileAsync("a@time.com", "FTTH", "x.xlsx", null, null);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBestMatchProfileAsync_ValidProfile_Disabled_ReturnsNull()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = templateId,
                CompanyId = companyId,
                Name = "TIME",
                Code = "TIME",
                IsActive = true,
                Description = "PROFILE_JSON: {\"profileId\":\"" + templateId + "\",\"profileName\":\"TIME\",\"enabled\":false,\"matchRules\":{\"senderDomains\":[\"time.com.my\"]}}"
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var result = await svc.GetBestMatchProfileAsync("a@time.com.my", "Subj", "x.xlsx", null, null);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBestMatchProfileAsync_SenderDomainMatch_ReturnsProfile()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = templateId,
                CompanyId = companyId,
                Name = "TIME",
                Code = "TIME",
                IsActive = true,
                Priority = 1,
                Description = "PROFILE_JSON: {\"profileId\":\"" + templateId + "\",\"profileName\":\"TIME\",\"enabled\":true,\"matchRules\":{\"senderDomains\":[\"time.com.my\"]}}"
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var result = await svc.GetBestMatchProfileAsync("noreply@time.com.my", "FTTH", "order.xlsx", null, null);
        Assert.NotNull(result);
        Assert.Equal(templateId, result.ProfileId);
        Assert.Equal("TIME", result.ProfileName);
    }

    [Fact]
    public async Task GetBestMatchProfileAsync_PartnerIdMatch_BeatsSenderDomain()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var tid1 = Guid.NewGuid();
        var tid2 = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = tid1,
                CompanyId = companyId,
                Name = "ByPartner",
                Code = "P",
                IsActive = true,
                Priority = 10,
                Description = "PROFILE_JSON: {\"profileId\":\"" + tid1 + "\",\"profileName\":\"ByPartner\",\"enabled\":true,\"matchRules\":{\"partnerIds\":[\"" + partnerId + "\"]}}"
            });
            context.ParserTemplates.Add(new ParserTemplate
            {
                Id = tid2,
                CompanyId = companyId,
                Name = "ByDomain",
                Code = "D",
                IsActive = true,
                Priority = 5,
                Description = "PROFILE_JSON: {\"profileId\":\"" + tid2 + "\",\"profileName\":\"ByDomain\",\"enabled\":true,\"matchRules\":{\"senderDomains\":[\"time.com.my\"]}}"
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var result = await svc.GetBestMatchProfileAsync("noreply@time.com.my", "FTTH", "order.xlsx", partnerId, null);
        Assert.NotNull(result);
        Assert.Equal(tid1, result.ProfileId);
        Assert.Equal("ByPartner", result.ProfileName);
    }

}
