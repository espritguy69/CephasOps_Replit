using CephasOps.Application.Parser.Utilities;
using CephasOps.Application.Parser.Utilities.ExcelParsing;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Utilities;

public class SheetHeaderDetectorTests
{
    /// <summary>
    /// In-memory sheet for testing: grid of cell text, 1-based row/col.
    /// </summary>
    private sealed class InMemorySheet : ISheetCellReader
    {
        private readonly string?[,] _cells;
        public int LastRow { get; }
        public int LastColumn { get; }

        public InMemorySheet(int rows, int cols, string?[,]? cells = null)
        {
            LastRow = rows;
            LastColumn = cols;
            _cells = cells ?? new string?[rows + 1, cols + 1];
        }

        public string? GetCellText(int row1Based, int col1Based)
        {
            if (row1Based < 1 || row1Based > LastRow || col1Based < 1 || col1Based > LastColumn)
                return null;
            return _cells[row1Based, col1Based];
        }

        public void Set(int row1Based, int col1Based, string? value)
        {
            if (row1Based >= 1 && row1Based <= LastRow && col1Based >= 1 && col1Based <= LastColumn)
                _cells[row1Based, col1Based] = value;
        }
    }

    [Fact]
    public void ScoreSheet_WhenLabelsInFirstRows_ReturnsPositiveScore()
    {
        var sheet = new InMemorySheet(10, 5);
        sheet.Set(1, 1, "Customer Name");
        sheet.Set(1, 2, "John");
        sheet.Set(2, 1, "Service Address");
        sheet.Set(2, 2, "Kuala Lumpur");
        sheet.Set(3, 1, "Contact No");
        sheet.Set(3, 2, "0123456789");
        int score = SheetHeaderDetector.ScoreSheet(sheet);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ScoreSheet_WhenNoKnownLabels_ReturnsZero()
    {
        var sheet = new InMemorySheet(5, 3);
        sheet.Set(1, 1, "Foo");
        sheet.Set(1, 2, "Bar");
        SheetHeaderDetector.ScoreSheet(sheet).Should().Be(0);
    }

    [Fact]
    public void DetectHeaderRow_WhenRow2HasMostLabels_ReturnsRow2()
    {
        var sheet = new InMemorySheet(10, 6);
        sheet.Set(1, 1, "Title");
        sheet.Set(2, 1, "Customer Name");
        sheet.Set(2, 2, "Service ID");
        sheet.Set(2, 3, "Service Address");
        sheet.Set(2, 4, "Contact No");
        var (row, score) = SheetHeaderDetector.DetectHeaderRow(sheet);
        row.Should().Be(2);
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DetectHeaderRow_WhenNoLabels_ReturnsRow1WithZeroScore()
    {
        var sheet = new InMemorySheet(5, 3);
        var (row, score) = SheetHeaderDetector.DetectHeaderRow(sheet);
        row.Should().Be(1);
        score.Should().Be(0);
    }

    [Fact]
    public void ScoreHeaderRow_CountsMatchingLabelSets()
    {
        var sheet = new InMemorySheet(5, 5);
        sheet.Set(1, 1, "Customer Name");
        sheet.Set(1, 2, "Service Address");
        sheet.Set(1, 3, "Contact No");
        int score = SheetHeaderDetector.ScoreHeaderRow(sheet, 1);
        score.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void DetectHeaderRow_WithRange_ScansOnlyMinMaxRows()
    {
        var sheet = new InMemorySheet(40, 5);
        sheet.Set(1, 1, "Title");
        sheet.Set(5, 1, "Customer Name");
        sheet.Set(5, 2, "Service ID");
        sheet.Set(5, 3, "Service Address");
        sheet.Set(20, 1, "Foo");
        var (rowDefault, _) = SheetHeaderDetector.DetectHeaderRow(sheet);
        var (rowRestricted, _) = SheetHeaderDetector.DetectHeaderRow(sheet, 10, 25);
        rowDefault.Should().Be(5);
        rowRestricted.Should().Be(10);
    }
}
