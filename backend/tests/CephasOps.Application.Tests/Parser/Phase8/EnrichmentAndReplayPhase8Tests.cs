using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase8;

public class EnrichmentAndReplayPhase8Tests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Replay_ResultSummary_IncludesProfileAndDriftFields()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "Test", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "c.xlsx", StoragePath = "z", FileId = fileId, SizeBytes = 100, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var session = new ParseSession { Id = sessionId, EmailMessageId = msgId, CompanyId = companyId, ParserTemplateId = templateId, Status = "Completed", ParsedOrdersCount = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            ParseSessionId = sessionId,
            CompanyId = companyId,
            SourceFileName = "c.xlsx",
            ValidationStatus = "NeedsReview",
            ConfidenceScore = 0.4m,
            ValidationNotes = " | [Audit] Category=DATA_MISSING; Profile=" + templateId + "; ProfileName=OldProfile; DriftDetected=false",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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

        var newProfileId = Guid.NewGuid();
        var mockFileService = new Mock<CephasOps.Application.Files.Services.IFileService>();
        mockFileService.Setup(x => x.GetFileContentAsync(fileId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 0x50, 0x4B });
        var mockParser = new Mock<ITimeExcelParserService>();
        mockParser.Setup(x => x.ParseAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<TemplateProfileContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeExcelParseResult
            {
                Success = false,
                ParseStatus = "FailedRequiredFields",
                ParseReport = new ParseReport
                {
                    ParseStatus = "FailedRequiredFields",
                    ParseFailureCategory = "LAYOUT_DRIFT",
                    FinalConfidenceScore = 0,
                    MissingRequiredFields = new List<string> { "ServiceId" },
                    TemplateProfileId = newProfileId,
                    TemplateProfileName = "NewProfile",
                    DriftDetected = true,
                    DriftSignature = "SheetChanged:Orders->Sheet1"
                }
            });
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<ParserReplayService>>();
        var replaySvc = new ParserReplayService(context, mockFileService.Object, mockParser.Object, mockLogger.Object);
        var result = await replaySvc.ReplayByAttachmentIdAsync(attId, "Test");

        Assert.Null(result.Error);
        var run = await context.ParserReplayRuns.FirstAsync();
        Assert.NotNull(run.ResultSummary);
        var json = JsonSerializer.Deserialize<JsonElement>(run.ResultSummary);
        Assert.True(json.TryGetProperty("newProfileId", out _));
        Assert.True(json.TryGetProperty("newProfileName", out _));
        Assert.True(json.TryGetProperty("newDriftDetected", out _));
        Assert.True(json.TryGetProperty("newDriftSignature", out _));
        Assert.True(json.TryGetProperty("reasonForChange", out var reason));
        Assert.Contains("DriftSignature=", reason.GetString());
    }
}
