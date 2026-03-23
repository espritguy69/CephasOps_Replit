using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase10;

/// <summary>
/// Phase 10: Recommendation rules applied in DriftReportService.ApplyRecommendations (tested via summary builder).
/// </summary>
public class DriftReportRecommendationRulesTests
{
    [Fact]
    public void Recommendations_always_include_replay_profile_pack_for_non_empty_profile_id()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.NewGuid(),
            ProfileName = "Test",
            TotalDrafts = 10,
            DriftDetectedCount = 0,
            CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            TopDriftSignatures = new List<SignatureCount>(),
            TopMissingFields = new List<FieldCount>(),
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().Contain(r => r.Contains("replay-profile-pack") && r.Contains("--profileId"));
    }

    [Fact]
    public void Recommendations_include_preferredSheetNames_when_high_drift_rate_and_SheetChanged_signature()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.NewGuid(),
            ProfileName = "Test",
            TotalDrafts = 100,
            DriftDetectedCount = 20,
            CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            TopDriftSignatures = new List<SignatureCount> { new() { Signature = "SheetChanged:Orders->Data", Count = 15 } },
            TopMissingFields = new List<FieldCount>(),
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().Contain(r => r.Contains("preferredSheetNames", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Recommendations_include_headerRowRange_when_HeaderRowShift_or_HeaderScoreDrop_signature()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.NewGuid(),
            ProfileName = "Test",
            TotalDrafts = 50,
            DriftDetectedCount = 5,
            CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            TopDriftSignatures = new List<SignatureCount> { new() { Signature = "HeaderRowShift:2->3", Count = 4 } },
            TopMissingFields = new List<FieldCount>(),
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().Contain(r => r.Contains("headerRowRange", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Recommendations_include_headerRowRange_for_HeaderScoreDrop_signature()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.NewGuid(),
            ProfileName = "Test",
            TotalDrafts = 50,
            DriftDetectedCount = 3,
            CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            TopDriftSignatures = new List<SignatureCount> { new() { Signature = "HeaderScoreDrop:90->50", Count = 3 } },
            TopMissingFields = new List<FieldCount>(),
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().Contain(r => r.Contains("headerRowRange", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Recommendations_include_DATA_MISSING_and_top_missing_fields_when_dominant()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.NewGuid(),
            ProfileName = "Test",
            TotalDrafts = 80,
            DriftDetectedCount = 0,
            CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["DATA_MISSING"] = 60, ["LAYOUT_DRIFT"] = 10 },
            TopDriftSignatures = new List<SignatureCount>(),
            TopMissingFields = new List<FieldCount> { new() { FieldName = "ServiceId", Count = 40 }, new() { FieldName = "CustomerName", Count = 25 } },
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().Contain(r => r.Contains("DATA_MISSING", StringComparison.OrdinalIgnoreCase) && r.Contains("vendor", StringComparison.OrdinalIgnoreCase));
        summary.Recommendations.Should().Contain(r => r.Contains("ServiceId") && r.Contains("CustomerName"));
    }

    [Fact]
    public void Recommendations_include_CONVERSION_ISSUE_when_present()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.NewGuid(),
            ProfileName = "Test",
            TotalDrafts = 20,
            DriftDetectedCount = 0,
            CountByCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["CONVERSION_ISSUE"] = 5 },
            TopDriftSignatures = new List<SignatureCount>(),
            TopMissingFields = new List<FieldCount>(),
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().Contain(r => r.Contains("CONVERSION_ISSUE", StringComparison.OrdinalIgnoreCase) && r.Contains(".xls", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Unassigned_profile_does_not_add_replay_pack_recommendation()
    {
        var summary = new ProfileDriftSummary
        {
            ProfileId = Guid.Empty,
            ProfileName = "Unassigned",
            TotalDrafts = 5,
            Recommendations = new List<string>()
        };
        DriftReportRecommendations.Apply(summary);
        summary.Recommendations.Should().NotContain(r => r.Contains("replay-profile-pack"));
    }

}
