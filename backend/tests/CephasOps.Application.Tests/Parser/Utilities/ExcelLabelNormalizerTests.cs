using CephasOps.Application.Parser.Utilities;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Utilities;

public class ExcelLabelNormalizerTests
{
    [Theory]
    [InlineData("Customer Name", "CUSTOMER NAME")]
    [InlineData("  Service ID  ", "SERVICE ID")]
    [InlineData("Service-ID", "SERVICE ID")]
    [InlineData("Service_ID", "SERVICE ID")]
    [InlineData("Contact No:", "CONTACT NO")]
    public void Normalize_Trims_CollapsesWhitespace_RemovesPunctuation_Uppercase(string input, string expectedNormalized)
    {
        ExcelLabelNormalizer.Normalize(input).Should().Be(expectedNormalized);
    }

    [Fact]
    public void Normalize_NullOrWhiteSpace_ReturnsEmpty()
    {
        ExcelLabelNormalizer.Normalize(null).Should().BeEmpty();
        ExcelLabelNormalizer.Normalize("").Should().BeEmpty();
        ExcelLabelNormalizer.Normalize("   ").Should().BeEmpty();
    }

    [Theory]
    [InlineData("Customer Name", "Customer Name", true)]
    [InlineData("CUSTOMER NAME", "Customer Name", true)]
    [InlineData("Customer Name (as per IC)", "Customer Name", true)]
    [InlineData("Service Address", "Service Address", true)]
    [InlineData("Different", "Customer Name", false)]
    public void MatchesAny_ExactOrContains_ReturnsExpected(string cellValue, string label, bool expectedMatch)
    {
        ExcelLabelNormalizer.MatchesAny(cellValue, new[] { label }).Should().Be(expectedMatch);
    }

    [Fact]
    public void MatchesAny_WithSynonyms_MatchesAnySynonym()
    {
        var synonyms = new[] { "Service ID", "SERVICE ID", "TBBN", "ServiceID" };
        ExcelLabelNormalizer.MatchesAny("Service ID", synonyms).Should().BeTrue();
        ExcelLabelNormalizer.MatchesAny("SERVICE ID", synonyms).Should().BeTrue();
        ExcelLabelNormalizer.MatchesAny("ServiceID", synonyms).Should().BeTrue();
        ExcelLabelNormalizer.MatchesAny("TBBN", synonyms).Should().BeTrue();
        ExcelLabelNormalizer.MatchesAny("Customer Name", synonyms).Should().BeFalse();
    }

    [Fact]
    public void ExactMatch_NormalizedEquality()
    {
        ExcelLabelNormalizer.ExactMatch("  Customer Name  ", "Customer Name").Should().BeTrue();
        ExcelLabelNormalizer.ExactMatch("Service-ID", "Service ID").Should().BeTrue();
        ExcelLabelNormalizer.ExactMatch("Customer Name (IC)", "Customer Name").Should().BeFalse();
    }
}
