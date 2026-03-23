using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase10;

[Collection("TenantScopeTests")]
public class DriftReportServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task BuildReportAsync_groups_drafts_by_ProfileId_from_audit_tokens()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var profileA = Guid.NewGuid();
        var profileB = Guid.NewGuid();
        // Use recent dates so drafts fall within "last 7 days" in BuildReportAsync(7, ...)
        var recent = DateTime.UtcNow.AddDays(-1);
        var drafts = new[]
        {
            CreateDraft(companyId, sessionId, profileA, "ProfileA", recent, "Category=LAYOUT_DRIFT"),
            CreateDraft(companyId, sessionId, profileA, "ProfileA", recent.AddHours(-1), "Category=DATA_MISSING"),
            CreateDraft(companyId, sessionId, profileB, "ProfileB", recent.AddHours(-2), "Category=PARSE_ERROR")
        };
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParsedOrderDrafts.AddRange(drafts);
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<DriftReportService>>();
        var profileLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TemplateProfileService>>();
        var profileService = new TemplateProfileService(context, profileLogger.Object);
        var service = new DriftReportService(context, profileService, logger.Object);
        var result = await service.BuildReportAsync(7, null, false);

        result.ProfileSummaries.Should().HaveCount(2);
        var sumA = result.ProfileSummaries.FirstOrDefault(s => s.ProfileId == profileA);
        var sumB = result.ProfileSummaries.FirstOrDefault(s => s.ProfileId == profileB);
        sumA.Should().NotBeNull();
        sumA!.TotalDrafts.Should().Be(2);
        sumB.Should().NotBeNull();
        sumB!.TotalDrafts.Should().Be(1);
    }

    [Fact]
    public async Task BuildReportAsync_aggregates_top_drift_signatures_and_missing_fields()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var recent = DateTime.UtcNow.AddDays(-1);
        var audit = "Profile=" + profileId + "; ProfileName=P; Category=LAYOUT_DRIFT; DriftSignature=SheetChanged:A->B; Missing=ServiceId,CustomerName";
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            for (var i = 0; i < 3; i++)
                context.ParsedOrderDrafts.Add(CreateDraft(companyId, sessionId, profileId, "P", recent.AddHours(-i), audit));
            audit = "Profile=" + profileId + "; ProfileName=P; Category=DATA_MISSING; DriftSignature=SheetChanged:A->B; Missing=ServiceId";
            context.ParsedOrderDrafts.Add(CreateDraft(companyId, sessionId, profileId, "P", recent.AddHours(-4), audit));
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<DriftReportService>>();
        var profileLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TemplateProfileService>>();
        var profileService = new TemplateProfileService(context, profileLogger.Object);
        var service = new DriftReportService(context, profileService, logger.Object);
        var result = await service.BuildReportAsync(7, null, false);

        var sum = result.ProfileSummaries.Single(s => s.ProfileId == profileId);
        sum.TopDriftSignatures.Should().Contain(s => s.Signature == "SheetChanged:A->B" && s.Count >= 3);
        sum.TopMissingFields.Should().Contain(f => f.FieldName == "ServiceId");
        sum.TopMissingFields.Should().Contain(f => f.FieldName == "CustomerName");
    }

    [Fact]
    public async Task BuildReportAsync_report_contains_only_token_field_names_not_PII_values()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var recent = DateTime.UtcNow.AddDays(-1);
        var draft = new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ParseSessionId = sessionId,
            CreatedAt = recent,
            UpdatedAt = recent,
            ValidationStatus = "NeedsReview",
            ValidationNotes = " | [Audit] Profile=" + profileId + "; ProfileName=TestProfile; Category=DATA_MISSING; Missing=ServiceId,CustomerName,AddressText",
            ServiceId = "PII-SERVICE-123",
            CustomerName = "John Doe",
            AddressText = "123 Main St",
            CustomerPhone = "+1234567890"
        };
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParsedOrderDrafts.Add(draft);
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<DriftReportService>>();
        var profileLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TemplateProfileService>>();
        var profileService = new TemplateProfileService(context, profileLogger.Object);
        var service = new DriftReportService(context, profileService, logger.Object);
        var result = await service.BuildReportAsync(7, null, false);

        var consoleOutput = DriftReportFormatters.FormatConsole(result);
        consoleOutput.Should().NotContain("PII-SERVICE-123");
        consoleOutput.Should().NotContain("John Doe");
        consoleOutput.Should().NotContain("123 Main St");
        consoleOutput.Should().NotContain("+1234567890");
        consoleOutput.Should().Contain("ServiceId");
        consoleOutput.Should().Contain("CustomerName");
        consoleOutput.Should().Contain("AddressText");
    }

    private static ParsedOrderDraft CreateDraft(Guid companyId, Guid sessionId, Guid profileId, string profileName, DateTime createdAt, string auditSuffix)
    {
        var notes = " | [Audit] Profile=" + profileId + "; ProfileName=" + profileName + "; " + auditSuffix;
        return new ParsedOrderDraft
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ParseSessionId = sessionId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            ValidationStatus = "NeedsReview",
            ValidationNotes = notes
        };
    }
}
