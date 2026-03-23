using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Files.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Services;

/// <summary>
/// Integration tests for ParserReplayService.
/// Asserts replay records are created and regression/improvement detection works.
/// Saves tenant-scoped entities; requires TenantScope (no bypass).
/// </summary>
[Collection("TenantScopeTests")]
public class ParserReplayServiceIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ITimeExcelParserService> _mockParser;
    private readonly Mock<ILogger<ParserReplayService>> _mockLogger;
    private readonly Guid _companyId;
    private readonly Guid? _previousTenantId;

    public ParserReplayServiceIntegrationTests()
    {
        _previousTenantId = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = _companyId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ReplayTest_{Guid.NewGuid()}")
            .Options;
        _context = new ApplicationDbContext(options);
        _mockFileService = new Mock<IFileService>();
        _mockParser = new Mock<ITimeExcelParserService>();
        _mockLogger = new Mock<ILogger<ParserReplayService>>();
    }

    [Fact]
    public async Task ReplayByAttachmentIdAsync_WhenAttachmentNotFound_ReturnsError()
    {
        var svc = new ParserReplayService(_context, _mockFileService.Object, _mockParser.Object, _mockLogger.Object);
        var result = await svc.ReplayByAttachmentIdAsync(Guid.NewGuid(), "Test");
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await _context.ParserReplayRuns.CountAsync());
    }

    [Fact]
    public async Task ReplayByAttachmentIdAsync_WhenAttachmentHasNoFileId_ReturnsError()
    {
        var companyId = _companyId;
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "Test", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m1", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "a.xlsx", StoragePath = "x", FileId = null, SizeBytes = 0, ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.EmailAccounts.Add(account);
        _context.EmailMessages.Add(em);
        _context.EmailAttachments.Add(att);
        await _context.SaveChangesAsync();

        var svc = new ParserReplayService(_context, _mockFileService.Object, _mockParser.Object, _mockLogger.Object);
        var result = await svc.ReplayByAttachmentIdAsync(attId, "Test");
        Assert.NotNull(result.Error);
        Assert.Equal(0, await _context.ParserReplayRuns.CountAsync());
    }

    [Fact]
    public async Task ReplayByAttachmentIdAsync_RecordsRunAndDetectsImprovement()
    {
        var companyId = _companyId;
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "Test", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m1", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "test.xlsx", StoragePath = "x", FileId = fileId, SizeBytes = 100, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var session = new ParseSession { Id = sessionId, EmailMessageId = msgId, CompanyId = companyId, Status = "Completed", ParsedOrdersCount = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            ParseSessionId = sessionId,
            CompanyId = companyId,
            SourceFileName = "test.xlsx",
            ValidationStatus = "NeedsReview",
            ConfidenceScore = 0.50m,
            ValidationNotes = "ParseStatus=FailedRequiredFields; Missing=ServiceId,CustomerName; Sheet=Sheet1; HeaderRow=2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmailAccounts.Add(account);
        _context.EmailMessages.Add(em);
        _context.EmailAttachments.Add(att);
        _context.ParseSessions.Add(session);
        _context.ParsedOrderDrafts.Add(draft);
        await _context.SaveChangesAsync();

        var minimalXlsx = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // minimal zip header so parser can be called
        _mockFileService.Setup(x => x.GetFileContentAsync(fileId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(minimalXlsx);

        var newReport = new ParseReport
        {
            ParseStatus = "Success",
            FinalConfidenceScore = 0.92m,
            MissingRequiredFields = new List<string>(),
            SelectedSheetName = "Sheet1",
            DetectedHeaderRow = 2
        };
        _mockParser.Setup(x => x.ParseAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<CephasOps.Application.Parser.DTOs.TemplateProfileContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeExcelParseResult
            {
                Success = true,
                ParseStatus = "Success",
                OrderData = new ParsedOrderData { ConfidenceScore = 0.92m },
                ParseReport = newReport
            });

        var svc = new ParserReplayService(_context, _mockFileService.Object, _mockParser.Object, _mockLogger.Object);
        var result = await svc.ReplayByAttachmentIdAsync(attId, "CLI");

        Assert.Null(result.Error);
        Assert.Equal(1, await _context.ParserReplayRuns.CountAsync());
        var run = await _context.ParserReplayRuns.FirstAsync();
        Assert.Equal(attId, run.AttachmentId);
        Assert.Equal("NeedsReview", run.OldParseStatus);
        Assert.Equal("Success", run.NewParseStatus);
        Assert.True(run.ImprovementDetected);
        Assert.Equal(0.50m, run.OldConfidence);
        Assert.Equal(0.92m, run.NewConfidence);
        Assert.NotNull(run.ResultSummary);
    }

    [Fact]
    public async Task ReplayByAttachmentIdAsync_DetectsRegression_WhenConfidenceDrops()
    {
        var companyId = _companyId;
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "Test", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m2", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "b.xlsx", StoragePath = "y", FileId = fileId, SizeBytes = 100, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var session = new ParseSession { Id = sessionId, EmailMessageId = msgId, CompanyId = companyId, Status = "Completed", ParsedOrdersCount = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            ParseSessionId = sessionId,
            CompanyId = companyId,
            SourceFileName = "b.xlsx",
            ValidationStatus = "Valid",
            ConfidenceScore = 0.95m,
            ValidationNotes = "Success; Sheet=Sheet1; HeaderRow=1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmailAccounts.Add(account);
        _context.EmailMessages.Add(em);
        _context.EmailAttachments.Add(att);
        _context.ParseSessions.Add(session);
        _context.ParsedOrderDrafts.Add(draft);
        await _context.SaveChangesAsync();

        _mockFileService.Setup(x => x.GetFileContentAsync(fileId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 0x50, 0x4B });
        _mockParser.Setup(x => x.ParseAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<CephasOps.Application.Parser.DTOs.TemplateProfileContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeExcelParseResult
            {
                Success = false,
                ParseStatus = "FailedRequiredFields",
                OrderData = null,
                ParseReport = new ParseReport { ParseStatus = "FailedRequiredFields", FinalConfidenceScore = 0, MissingRequiredFields = new List<string> { "ServiceId", "CustomerName" } }
            });

        var svc = new ParserReplayService(_context, _mockFileService.Object, _mockParser.Object, _mockLogger.Object);
        var result = await svc.ReplayByAttachmentIdAsync(attId, "CLI");

        Assert.Null(result.Error);
        Assert.True(result.RegressionDetected);
        var run = await _context.ParserReplayRuns.FirstAsync();
        Assert.True(run.RegressionDetected);
    }

    [Fact]
    public async Task Replay_ResultSummary_IncludesDiagnosticsAndReasonForChange()
    {
        var companyId = _companyId;
        var accountId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var attId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var account = new EmailAccount { Id = accountId, CompanyId = companyId, Name = "Test", Username = "t@t.com", Password = "x", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var em = new EmailMessage { Id = msgId, CompanyId = companyId, EmailAccountId = accountId, MessageId = "m3", FromAddress = "f@f.com", ToAddresses = "t@t.com", Subject = "s", ReceivedAt = DateTime.UtcNow, ParserStatus = "Parsed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var att = new EmailAttachment { Id = attId, EmailMessageId = msgId, CompanyId = companyId, FileName = "c.xlsx", StoragePath = "z", FileId = fileId, SizeBytes = 100, ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var session = new ParseSession { Id = sessionId, EmailMessageId = msgId, CompanyId = companyId, Status = "Completed", ParsedOrdersCount = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            ParseSessionId = sessionId,
            CompanyId = companyId,
            SourceFileName = "c.xlsx",
            ValidationStatus = "NeedsReview",
            ConfidenceScore = 0.4m,
            ValidationNotes = "ParseStatus=FailedRequiredFields; Missing=ServiceId; Sheet=Sheet1; HeaderRow=2; Category=DATA_MISSING; HeaderScore=3; BestSheetScore=5; RequiredFoundBy=ServiceId:n,CustomerName:NormalizedExact",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.EmailAccounts.Add(account);
        _context.EmailMessages.Add(em);
        _context.EmailAttachments.Add(att);
        _context.ParseSessions.Add(session);
        _context.ParsedOrderDrafts.Add(draft);
        await _context.SaveChangesAsync();

        _mockFileService.Setup(x => x.GetFileContentAsync(fileId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new byte[] { 0x50, 0x4B });
        _mockParser.Setup(x => x.ParseAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<CephasOps.Application.Parser.DTOs.TemplateProfileContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TimeExcelParseResult
            {
                Success = false,
                ParseStatus = "FailedRequiredFields",
                OrderData = null,
                ParseReport = new ParseReport
                {
                    ParseStatus = "FailedRequiredFields",
                    ParseFailureCategory = "LAYOUT_DRIFT",
                    FinalConfidenceScore = 0,
                    MissingRequiredFields = new List<string> { "ServiceId" },
                    HeaderScore = 2,
                    SheetScoreBest = 4,
                    FieldDiagnostics = new List<FieldDiagnosticEntry> { new() { FieldName = "ServiceId", Found = true, MatchedLabel = "Service ID", MatchType = "NormalizedExact" } }
                }
            });

        var svc = new ParserReplayService(_context, _mockFileService.Object, _mockParser.Object, _mockLogger.Object);
        await svc.ReplayByAttachmentIdAsync(attId, "CLI");

        var run = await _context.ParserReplayRuns.FirstAsync();
        Assert.NotNull(run.ResultSummary);
        Assert.Contains("reasonForChange", run.ResultSummary);
        Assert.Contains("oldParseFailureCategory", run.ResultSummary);
        Assert.Contains("newParseFailureCategory", run.ResultSummary);
        Assert.Contains("DATA_MISSING", run.ResultSummary);
        Assert.Contains("LAYOUT_DRIFT", run.ResultSummary);
    }

    public void Dispose()
    {
        TenantScope.CurrentTenantId = _previousTenantId;
        _context?.Dispose();
    }
}
