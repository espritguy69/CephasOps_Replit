using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase8;

[Collection("TenantScopeTests")]
public class DriftBaselineProviderTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetBaselineAsync_NoValidDraft_ReturnsNull()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParseSessions.Add(new ParseSession { Id = sessionId, CompanyId = companyId, ParserTemplateId = templateId, Status = "Completed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.ParsedOrderDrafts.Add(new ParsedOrderDraft
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ParseSessionId = sessionId,
                ValidationStatus = "NeedsReview",
                ValidationNotes = " | [Audit] Sheet=Orders; HeaderRow=2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var provider = new DriftBaselineProvider(context);
        var baseline = await provider.GetBaselineAsync(templateId);
        Assert.Null(baseline);
    }

    [Fact]
    public async Task GetBaselineAsync_ValidDraft_ReturnsBaseline()
    {
        await using var context = CreateContext();
        var companyId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var prev = TenantScope.CurrentTenantId;
        TenantScope.CurrentTenantId = companyId;
        try
        {
            context.ParseSessions.Add(new ParseSession { Id = sessionId, CompanyId = companyId, ParserTemplateId = templateId, Status = "Completed", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            context.ParsedOrderDrafts.Add(new ParsedOrderDraft
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                ParseSessionId = sessionId,
                ValidationStatus = "Valid",
                ValidationNotes = "Success | [Audit] ParseStatus=Success; Sheet=Orders; HeaderRow=2; HeaderScore=5; BestSheetScore=6",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
        finally { TenantScope.CurrentTenantId = prev; }
        var provider = new DriftBaselineProvider(context);
        var baseline = await provider.GetBaselineAsync(templateId);
        Assert.NotNull(baseline);
        Assert.Equal("Orders", baseline.SelectedSheetName);
        Assert.Equal(2, baseline.DetectedHeaderRow);
        Assert.Equal(5, baseline.HeaderScore);
        Assert.Equal(6, baseline.SheetScoreBest);
    }
}
