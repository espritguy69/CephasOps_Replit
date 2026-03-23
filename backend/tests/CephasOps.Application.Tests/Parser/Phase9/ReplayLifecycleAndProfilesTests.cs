using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase9;

[Collection("TenantScopeTests")]
public class ReplayLifecycleAndProfilesTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ReplayByAttachmentIdAsync_WithLifecycleContext_ResultSummaryIncludesVersionOwnerPackName()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "T", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "p.xlsx", StoragePath = "z", FileId = fileId, SizeBytes = 100, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var session = new ParseSession { Id = sessionId, EmailMessageId = msgId, CompanyId = companyId, Status = "Completed", ParsedOrdersCount = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            ParseSessionId = sessionId,
            CompanyId = companyId,
            SourceFileName = "p.xlsx",
            ValidationStatus = "Valid",
            ConfidenceScore = 0.9m,
            ValidationNotes = " | [Audit] ParseStatus=Success; Sheet=Sheet1; HeaderRow=1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.EmailAccounts.Add(account);
        context.EmailMessages.Add(em);
        context.EmailAttachments.Add(att);
        context.ParseSessions.Add(session);
        context.ParsedOrderDrafts.Add(draft);
        await context.SaveChangesAsync();

        var mockFile = new Mock<CephasOps.Application.Files.Services.IFileService>();
        mockFile.Setup(x => x.GetFileContentAsync(fileId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 0x50, 0x4B });
        var mockParser = new Mock<ITimeExcelParserService>();
        mockParser.Setup(x => x.ParseAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<TemplateProfileContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeExcelParseResult
            {
                Success = true,
                ParseStatus = "Success",
                OrderData = new ParsedOrderData { ConfidenceScore = 0.9m },
                ParseReport = new ParseReport { ParseStatus = "Success", FinalConfidenceScore = 0.9m }
            });
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ParserReplayService>>();
        var replay = new ParserReplayService(context, mockFile.Object, mockParser.Object, mockLogger.Object);

        var lifecycle = new ProfileLifecycleContext
        {
            ProfileId = profileId,
            ProfileName = "TestProfile",
            ProfileVersion = "2.0.0",
            EffectiveFrom = "2025-02-01",
            Owner = "ops@test.com",
            PackName = "Phase9 pack"
        };
        var result = await replay.ReplayByAttachmentIdAsync(attId, "ProfilePack", lifecycle);
        Assert.Null(result.Error);
        var run = await context.ParserReplayRuns.FirstAsync();
        Assert.NotNull(run.ResultSummary);
        var json = JsonSerializer.Deserialize<JsonElement>(run.ResultSummary);
        Assert.Equal(profileId, Guid.Parse(json.GetProperty("profileId").GetString()!));
        Assert.Equal("TestProfile", json.GetProperty("profileName").GetString());
        Assert.Equal("2.0.0", json.GetProperty("profileVersion").GetString());
        Assert.Equal("2025-02-01", json.GetProperty("effectiveFrom").GetString());
        Assert.Equal("ops@test.com", json.GetProperty("owner").GetString());
        Assert.Equal("Phase9 pack", json.GetProperty("packName").GetString());
    }

    [Fact]
    public async Task GetAttachmentIdsForProfileAsync_ReturnsAttachmentsForDraftsWithProfileInNotes()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParseSessions.Add(new ParseSession { Id = sessionId, CompanyId = companyId, EmailMessageId = msgId, Status = "Completed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.ParsedOrderDrafts.Add(new ParsedOrderDraft
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ParseSessionId = sessionId,
                SourceFileName = "f.xlsx",
                ValidationNotes = " | [Audit] Profile=" + profileId + "; Sheet=Sheet1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            });
            context.EmailAttachments.Add(new EmailAttachment { Id = attId, EmailMessageId = msgId, FileName = "f.xlsx", CompanyId = companyId, StoragePath = "s", SizeBytes = 1, ContentType = "application/octet-stream", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }

        var replay = new ParserReplayService(context, Mock.Of<CephasOps.Application.Files.Services.IFileService>(), Mock.Of<ITimeExcelParserService>(), Mock.Of<Microsoft.Extensions.Logging.ILogger<ParserReplayService>>());
        var ids = await replay.GetAttachmentIdsForProfileAsync(profileId, 30);
        Assert.Single(ids);
        Assert.Equal(attId, ids[0]);
    }

    [Fact]
    public async Task GetAllProfileConfigsAsync_EnabledOnly_ReturnsOnlyEnabledConfigs()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var tid1 = Guid.NewGuid();
        var tid2 = Guid.NewGuid();
        var json1 = "{\"profileId\":\"" + tid1 + "\",\"profileName\":\"E\",\"enabled\":true,\"matchRules\":{\"senderDomains\":[\"e.com\"]}}";
        var json2 = "{\"profileId\":\"" + tid2 + "\",\"profileName\":\"D\",\"enabled\":false,\"matchRules\":{\"senderDomains\":[\"d.com\"]}}";
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParserTemplates.Add(new ParserTemplate { Id = tid1, CompanyId = companyId, Name = "E", Code = "E", IsActive = true, Description = TemplateProfileService.ProfileJsonPrefix + json1 });
            context.ParserTemplates.Add(new ParserTemplate { Id = tid2, CompanyId = companyId, Name = "D", Code = "D", IsActive = true, Description = TemplateProfileService.ProfileJsonPrefix + json2 });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var svc = new TemplateProfileService(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateProfileService>.Instance);
        var list = await svc.GetAllProfileConfigsAsync(true);
        Assert.Single(list);
        Assert.Equal("E", list[0].Config.ProfileName);
    }

    [Fact]
    public async Task ReplayByAttachmentIdAsync_Result_PopulatesCategoryAndReasonForChange()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "T", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "q.xlsx", StoragePath = "z", FileId = fileId, SizeBytes = 100, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var session = new ParseSession { Id = sessionId, EmailMessageId = msgId, CompanyId = companyId, Status = "Completed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var draft = new ParsedOrderDraft { Id = Guid.NewGuid(), ParseSessionId = sessionId, CompanyId = companyId, SourceFileName = "q.xlsx", ValidationStatus = "NeedsReview", ConfidenceScore = 0.4m, ValidationNotes = " | [Audit] Category=LAYOUT_DRIFT; HeaderScore=2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.EmailAccounts.Add(account);
            context.EmailMessages.Add(em);
            context.EmailAttachments.Add(att);
            context.ParseSessions.Add(session);
            context.ParsedOrderDrafts.Add(draft);
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }

        var mockFile = new Mock<CephasOps.Application.Files.Services.IFileService>();
        mockFile.Setup(x => x.GetFileContentAsync(fileId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 0x50, 0x4B });
        var mockParser = new Mock<ITimeExcelParserService>();
        mockParser.Setup(x => x.ParseAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<TemplateProfileContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeExcelParseResult
            {
                Success = false,
                ParseStatus = "FailedRequiredFields",
                ParseReport = new ParseReport { ParseStatus = "FailedRequiredFields", ParseFailureCategory = "LAYOUT_DRIFT", FinalConfidenceScore = 0, DriftSignature = "SheetChanged:A->B" }
            });
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ParserReplayService>>();
        var replay = new ParserReplayService(context, mockFile.Object, mockParser.Object, mockLogger.Object);
        var result = await replay.ReplayByAttachmentIdAsync(attId, "CLI");
        Assert.NotNull(result.NewParseFailureCategory);
        Assert.Equal("LAYOUT_DRIFT", result.NewParseFailureCategory);
        Assert.NotNull(result.NewDriftSignature);
        Assert.Contains("SheetChanged", result.NewDriftSignature);
    }
}
