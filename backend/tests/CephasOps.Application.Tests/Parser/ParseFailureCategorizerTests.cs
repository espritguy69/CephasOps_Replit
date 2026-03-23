using CephasOps.Application.Parser.Utilities;
using Xunit;

namespace CephasOps.Application.Tests.Parser;

public class ParseFailureCategorizerTests
{
    [Fact]
    public void Categorize_Success_ReturnsNone()
    {
        var c = ParseFailureCategorizer.Categorize("Success", false, false);
        Assert.Equal(ParseFailureCategory.None, c);
    }

    [Fact]
    public void Categorize_FailedRequiredFields_NoLabelsElsewhere_ReturnsDataMissing()
    {
        var c = ParseFailureCategorizer.Categorize("FailedRequiredFields", false, false);
        Assert.Equal(ParseFailureCategory.DataMissing, c);
    }

    [Fact]
    public void Categorize_FailedRequiredFields_LabelsFoundElsewhere_ReturnsLayoutDrift()
    {
        var c = ParseFailureCategorizer.Categorize("FailedRequiredFields", false, true);
        Assert.Equal(ParseFailureCategory.LayoutDrift, c);
    }

    [Fact]
    public void Categorize_FailedValidation_ReturnsValidationFail()
    {
        var c = ParseFailureCategorizer.Categorize("FailedValidation", false, false);
        Assert.Equal(ParseFailureCategory.ValidationFail, c);
    }

    [Fact]
    public void Categorize_ParseError_ConversionFailed_ReturnsConversionIssue()
    {
        var c = ParseFailureCategorizer.Categorize("ParseError", true, false);
        Assert.Equal(ParseFailureCategory.ConversionIssue, c);
    }

    [Fact]
    public void Categorize_ParseError_NoConversion_ReturnsParseError()
    {
        var c = ParseFailureCategorizer.Categorize("ParseError", false, false);
        Assert.Equal(ParseFailureCategory.ParseError, c);
    }

    [Fact]
    public void Categorize_NullOrEmpty_ReturnsNone()
    {
        Assert.Equal(ParseFailureCategory.None, ParseFailureCategorizer.Categorize(null, false, false));
        Assert.Equal(ParseFailureCategory.None, ParseFailureCategorizer.Categorize("", false, false));
    }
}
