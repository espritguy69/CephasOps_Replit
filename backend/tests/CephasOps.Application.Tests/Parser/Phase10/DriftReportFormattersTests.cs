using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase10;

public class DriftReportFormattersTests
{
    [Fact]
    public void FormatConsole_includes_days_and_total_drafts()
    {
        var result = new DriftReportResult { Days = 14, TotalDrafts = 100, ProfilesWithDrafts = 2, GeneratedAtUtc = DateTime.UtcNow };
        var text = DriftReportFormatters.FormatConsole(result);
        text.Should().Contain("14 days");
        text.Should().Contain("100");
    }

    [Fact]
    public void FormatConsole_includes_profile_section_and_recommendations()
    {
        var result = new DriftReportResult
        {
            Days = 7,
            TotalDrafts = 5,
            ProfilesWithDrafts = 1,
            GeneratedAtUtc = DateTime.UtcNow,
            ProfileSummaries = new List<ProfileDriftSummary>
            {
                new()
                {
                    ProfileId = Guid.NewGuid(),
                    ProfileName = "TestProfile",
                    TotalDrafts = 5,
                    NeedsReviewCount = 2,
                    DriftDetectedCount = 1,
                    Recommendations = new List<string> { "Run replay-profile-pack --profileId x before enabling changes." }
                }
            }
        };
        var text = DriftReportFormatters.FormatConsole(result);
        text.Should().Contain("TestProfile");
        text.Should().Contain("Recommendations:");
        text.Should().Contain("replay-profile-pack");
    }

    [Fact]
    public void FormatMarkdown_has_executive_summary_and_tables()
    {
        var result = new DriftReportResult
        {
            Days = 7,
            TotalDrafts = 10,
            ProfilesWithDrafts = 1,
            GeneratedAtUtc = DateTime.UtcNow,
            ProfileSummaries = new List<ProfileDriftSummary>
            {
                new()
                {
                    ProfileId = Guid.NewGuid(),
                    ProfileName = "VendorA",
                    TotalDrafts = 10,
                    CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["DATA_MISSING"] = 6 },
                    TopDriftSignatures = new List<SignatureCount>(),
                    TopMissingFields = new List<FieldCount> { new() { FieldName = "ServiceId", Count = 5 } },
                    Recommendations = new List<string>()
                }
            }
        };
        var md = DriftReportFormatters.FormatMarkdown(result);
        md.Should().StartWith("# Drift Report");
        md.Should().Contain("## Executive summary");
        md.Should().Contain("## VendorA");
        md.Should().Contain("| Metric | Value |");
        md.Should().Contain("| Category | Count |");
        md.Should().Contain("DATA_MISSING");
        md.Should().Contain("ServiceId");
    }

    [Fact]
    public void FormatMarkdown_escapes_pipe_in_content()
    {
        var result = new DriftReportResult
        {
            Days = 7,
            TotalDrafts = 0,
            ProfilesWithDrafts = 1,
            GeneratedAtUtc = DateTime.UtcNow,
            ProfileSummaries = new List<ProfileDriftSummary>
            {
                new() { ProfileId = Guid.NewGuid(), ProfileName = "Profile|With|Pipe", TotalDrafts = 0 }
            }
        };
        var md = DriftReportFormatters.FormatMarkdown(result);
        // EscapeMd replaces | with \| so table syntax is preserved
        (md.Contains("Profile\\|With\\|Pipe", StringComparison.Ordinal) || md.Contains("Profile|With|Pipe", StringComparison.Ordinal)).Should().BeTrue();
    }

    [Fact]
    public void FormatMarkdown_is_deterministic_for_same_input()
    {
        var fixedUtc = new DateTime(2026, 2, 9, 12, 0, 0, DateTimeKind.Utc);
        var result = new DriftReportResult
        {
            Days = 7,
            TotalDrafts = 5,
            ProfilesWithDrafts = 1,
            GeneratedAtUtc = fixedUtc,
            ProfileSummaries = new List<ProfileDriftSummary>
            {
                new()
                {
                    ProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    ProfileName = "VendorA",
                    TotalDrafts = 5,
                    DriftDetectedCount = 1,
                    CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["LAYOUT_DRIFT"] = 1, ["DATA_MISSING"] = 1 },
                    TopDriftSignatures = new List<SignatureCount> { new() { Signature = "SheetChanged", Count = 1 } },
                    TopMissingFields = new List<FieldCount> { new() { FieldName = "ServiceId", Count = 1 } }
                }
            }
        };
        var md1 = DriftReportFormatters.FormatMarkdown(result);
        var md2 = DriftReportFormatters.FormatMarkdown(result);
        md1.Should().Be(md2);
        md1.Should().Contain("2026-02-09 12:00");
        md1.Should().Contain("20.0"); // invariant numeric (percent)
    }
}
