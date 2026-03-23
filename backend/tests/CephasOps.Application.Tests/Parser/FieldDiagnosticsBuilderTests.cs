using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Application.Parser.Utilities.ExcelParsing;
using Xunit;

namespace CephasOps.Application.Tests.Parser;

public class FieldDiagnosticsBuilderTests
{
    [Fact]
    public void Build_NullSheet_ReturnsEmpty()
    {
        var list = FieldDiagnosticsBuilder.Build(null!, "Sheet1", null, 1, 5);
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public void FieldDiagnostics_NeverContainsPII_OnlyLabelNamesAndMetadata()
    {
        var sheet = new InMemorySheetCellReader(new Dictionary<(int, int), string>
        {
            [(1, 1)] = "Customer Name",
            [(1, 2)] = "John Doe",
            [(2, 1)] = "Service ID",
            [(2, 2)] = "TBBN123"
        });
        var diagnostics = FieldDiagnosticsBuilder.Build(sheet, "Sheet1", null, 1, 2);
        Assert.True(diagnostics.Count > 0);
        foreach (var d in diagnostics)
        {
            Assert.NotNull(d.FieldName);
            Assert.NotNull(d.MatchType);
            Assert.NotNull(d.ExtractionMode);
            if (d.MatchedLabel != null)
            {
                Assert.DoesNotContain("John", d.MatchedLabel);
                Assert.DoesNotContain("Doe", d.MatchedLabel);
                Assert.DoesNotContain("TBBN123", d.MatchedLabel);
            }
        }
        var customerEntry = diagnostics.FirstOrDefault(d => d.FieldName == "CustomerName");
        Assert.NotNull(customerEntry);
        Assert.True(customerEntry.Found);
        Assert.Equal("Customer Name", customerEntry.MatchedLabel);
        Assert.True(customerEntry.RowIndex > 0);
        Assert.True(customerEntry.ColumnIndex > 0);
    }

    [Fact]
    public void GetBestSheetScores_Empty_ReturnsNullNull()
    {
        var (b, s) = FieldDiagnosticsBuilder.GetBestSheetScores(null);
        Assert.Null(b);
        Assert.Null(s);
    }

    [Fact]
    public void GetBestSheetScores_OneEntry_ReturnsBestNull()
    {
        var (b, s) = FieldDiagnosticsBuilder.GetBestSheetScores(new Dictionary<string, int> { ["Sheet1"] = 5 });
        Assert.Equal(5, b);
        Assert.Null(s);
    }

    [Fact]
    public void GetBestSheetScores_Multiple_ReturnsOrdered()
    {
        var scores = new Dictionary<string, int> { ["A"] = 3, ["B"] = 7, ["C"] = 5 };
        var (b, s) = FieldDiagnosticsBuilder.GetBestSheetScores(scores);
        Assert.Equal(7, b);
        Assert.Equal(5, s);
    }

    [Fact]
    public void CountRequiredFieldsFound_CountsOnlyRequired()
    {
        var list = new List<FieldDiagnosticEntry>
        {
            new() { FieldName = "ServiceId", Found = true },
            new() { FieldName = "CustomerName", Found = true },
            new() { FieldName = "PackageName", Found = true }
        };
        var n = FieldDiagnosticsBuilder.CountRequiredFieldsFound(list);
        Assert.Equal(2, n);
    }

    [Fact]
    public void RequiredLabelsFoundElsewhere_MissingAndFound_ReturnsTrue()
    {
        var missing = new[] { "ServiceId" };
        var diagnostics = new List<FieldDiagnosticEntry> { new() { FieldName = "ServiceId", Found = true } };
        Assert.True(FieldDiagnosticsBuilder.RequiredLabelsFoundElsewhere(missing, diagnostics));
    }

    [Fact]
    public void RequiredLabelsFoundElsewhere_MissingButNotFound_ReturnsFalse()
    {
        var missing = new[] { "ServiceId" };
        var diagnostics = new List<FieldDiagnosticEntry> { new() { FieldName = "ServiceId", Found = false } };
        Assert.False(FieldDiagnosticsBuilder.RequiredLabelsFoundElsewhere(missing, diagnostics));
    }

    [Fact]
    public void Build_WithSynonymOverrides_UsesOverridesForField()
    {
        var sheet = new InMemorySheetCellReader(new Dictionary<(int, int), string>
        {
            [(1, 1)] = "TBBN",
            [(1, 2)] = "123"
        });
        var overrides = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ServiceId"] = new[] { "TBBN", "Partner Service ID" }
        };
        var diagnostics = FieldDiagnosticsBuilder.Build(sheet, "Sheet1", null, 1, 1, overrides, null);
        var serviceIdEntry = diagnostics.FirstOrDefault(d => d.FieldName == "ServiceId");
        Assert.NotNull(serviceIdEntry);
        Assert.True(serviceIdEntry.Found);
        Assert.Equal("TBBN", serviceIdEntry.MatchedLabel);
    }

    private sealed class InMemorySheetCellReader : ISheetCellReader
    {
        private readonly Dictionary<(int row, int col), string> _cells;
        private readonly int _lastRow;
        private readonly int _lastCol;

        public InMemorySheetCellReader(Dictionary<(int, int), string> cells)
        {
            _cells = cells;
            _lastRow = cells.Keys.Select(k => k.Item1).DefaultIfEmpty(0).Max();
            _lastCol = cells.Keys.Select(k => k.Item2).DefaultIfEmpty(0).Max();
        }

        public int LastRow => _lastRow;
        public int LastColumn => _lastCol;
        public string? GetCellText(int row1Based, int col1Based) =>
            _cells.TryGetValue((row1Based, col1Based), out var t) ? t : null;
    }
}
