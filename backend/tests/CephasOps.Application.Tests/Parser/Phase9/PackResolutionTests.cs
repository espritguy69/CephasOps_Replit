using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase9;

[Collection("TenantScopeTests")]
public class PackResolutionTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ResolvePackAttachmentIdsAsync_AttachmentIdsDirect_ReturnsThem()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var att1 = Guid.NewGuid();
        var att2 = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.EmailAttachments.Add(new CephasOps.Domain.Parser.Entities.EmailAttachment { Id = att1, FileName = "a.xlsx", EmailMessageId = Guid.NewGuid(), CompanyId = companyId, StoragePath = "x", SizeBytes = 1, ContentType = "application/octet-stream", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.EmailAttachments.Add(new CephasOps.Domain.Parser.Entities.EmailAttachment { Id = att2, FileName = "b.xlsx", EmailMessageId = Guid.NewGuid(), CompanyId = companyId, StoragePath = "y", SizeBytes = 1, ContentType = "application/octet-stream", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var pack = new ProfilePackConfig { AttachmentIds = new List<Guid> { att1, att2 } };
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var ids = await svc.ResolvePackAttachmentIdsAsync(pack);
        Assert.Equal(2, ids.Count);
        Assert.Contains(att1, ids);
        Assert.Contains(att2, ids);
    }

    [Fact]
    public async Task ResolvePackAttachmentIdsAsync_ParseSessionIdsFallback_ResolvesViaSessionAndDraftFileName()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParseSessions.Add(new ParseSession { Id = sessionId, CompanyId = companyId, EmailMessageId = msgId, Status = "Completed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.ParsedOrderDrafts.Add(new ParsedOrderDraft { Id = Guid.NewGuid(), CompanyId = companyId, ParseSessionId = sessionId, SourceFileName = "order.xlsx", ValidationStatus = "Valid", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.EmailAttachments.Add(new CephasOps.Domain.Parser.Entities.EmailAttachment { Id = attId, EmailMessageId = msgId, FileName = "order.xlsx", CompanyId = companyId, StoragePath = "s", SizeBytes = 1, ContentType = "application/octet-stream", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var pack = new ProfilePackConfig { ParseSessionIds = new List<Guid> { sessionId } };
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var ids = await svc.ResolvePackAttachmentIdsAsync(pack);
        Assert.Single(ids);
        Assert.Equal(attId, ids[0]);
    }

    [Fact]
    public async Task ResolvePackAttachmentIdsAsync_EmptyPack_ReturnsEmpty()
    {
        await using var context = CreateContext();
        var pack = new ProfilePackConfig();
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var ids = await svc.ResolvePackAttachmentIdsAsync(pack);
        Assert.Empty(ids);
    }
}
