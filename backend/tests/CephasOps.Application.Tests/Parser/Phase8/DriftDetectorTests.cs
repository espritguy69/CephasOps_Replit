using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Phase8;

public class DriftDetectorTests
{
    [Fact]
    public void Detect_NoBaseline_ReturnsNotDetected()
    {
        var current = new ParseReport
        {
            SelectedSheetName = "Sheet1",
            DetectedHeaderRow = 2,
            HeaderScore = 5,
            SheetScoreBest = 6
        };
        var result = DriftDetector.Detect(current, baseline: null, thresholds: null);
        Assert.False(result.DriftDetected);
        Assert.Empty(result.DriftSignature);
    }

    [Fact]
    public void Detect_SheetChanged_BeyondThreshold_Detected()
    {
        var current = new ParseReport { SelectedSheetName = "Sheet1", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 6 };
        var baseline = new DriftDetector.DriftBaseline { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 6 };
        var thresholds = new TemplateProfileDriftThresholds { HeaderRowShift = 3, HeaderScoreDrop = 2, BestSheetScoreDrop = 3 };
        var result = DriftDetector.Detect(current, baseline, thresholds);
        Assert.True(result.DriftDetected);
        Assert.Contains("SheetChanged:Orders->Sheet1", result.DriftSignature);
    }

    [Fact]
    public void Detect_HeaderRowShift_BeyondThreshold_Detected()
    {
        var current = new ParseReport { SelectedSheetName = "Orders", DetectedHeaderRow = 6, HeaderScore = 5, SheetScoreBest = 6 };
        var baseline = new DriftDetector.DriftBaseline { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 6 };
        var thresholds = new TemplateProfileDriftThresholds { HeaderRowShift = 3, HeaderScoreDrop = 2, BestSheetScoreDrop = 3 };
        var result = DriftDetector.Detect(current, baseline, thresholds);
        Assert.True(result.DriftDetected);
        Assert.Contains("HeaderRowShift:+4", result.DriftSignature);
    }

    [Fact]
    public void Detect_HeaderScoreDrop_BeyondThreshold_Detected()
    {
        var current = new ParseReport { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 2, SheetScoreBest = 6 };
        var baseline = new DriftDetector.DriftBaseline { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 6 };
        var thresholds = new TemplateProfileDriftThresholds { HeaderRowShift = 3, HeaderScoreDrop = 2, BestSheetScoreDrop = 3 };
        var result = DriftDetector.Detect(current, baseline, thresholds);
        Assert.True(result.DriftDetected);
        Assert.Contains("HeaderScoreDrop", result.DriftSignature);
    }

    [Fact]
    public void Detect_BestSheetScoreDrop_BeyondThreshold_Detected()
    {
        var current = new ParseReport { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 2 };
        var baseline = new DriftDetector.DriftBaseline { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 6 };
        var thresholds = new TemplateProfileDriftThresholds { HeaderRowShift = 3, HeaderScoreDrop = 2, BestSheetScoreDrop = 3 };
        var result = DriftDetector.Detect(current, baseline, thresholds);
        Assert.True(result.DriftDetected);
        Assert.Contains("BestSheetScoreDrop", result.DriftSignature);
    }

    [Fact]
    public void Detect_WithinThresholds_NotDetected()
    {
        var current = new ParseReport { SelectedSheetName = "Orders", DetectedHeaderRow = 3, HeaderScore = 4, SheetScoreBest = 5 };
        var baseline = new DriftDetector.DriftBaseline { SelectedSheetName = "Orders", DetectedHeaderRow = 2, HeaderScore = 5, SheetScoreBest = 6 };
        var thresholds = new TemplateProfileDriftThresholds { HeaderRowShift = 3, HeaderScoreDrop = 2, BestSheetScoreDrop = 3 };
        var result = DriftDetector.Detect(current, baseline, thresholds);
        Assert.False(result.DriftDetected);
        Assert.Empty(result.DriftSignature);
    }
}
