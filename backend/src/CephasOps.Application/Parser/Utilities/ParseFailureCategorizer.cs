namespace CephasOps.Application.Parser.Utilities;

/// <summary>
/// Deterministic failure reason categorization for parse diagnostics.
/// </summary>
public static class ParseFailureCategory
{
    public const string None = "NONE";
    public const string DataMissing = "DATA_MISSING";
    public const string LayoutDrift = "LAYOUT_DRIFT";
    public const string ConversionIssue = "CONVERSION_ISSUE";
    public const string ValidationFail = "VALIDATION_FAIL";
    public const string ParseError = "PARSE_ERROR";
}

/// <summary>
/// Maps ParseStatus + signals to a single category for operational diagnostics.
/// </summary>
public static class ParseFailureCategorizer
{
    /// <summary>
    /// Categorize failure reason. Does not store PII.
    /// </summary>
    /// <param name="parseStatus">Success, FailedRequiredFields, FailedValidation, ParseError, etc.</param>
    /// <param name="conversionFailed">True if .xls→.xlsx conversion failed or workbook unreadable.</param>
    /// <param name="requiredLabelsFoundElsewhere">True if labels for missing required fields were detected elsewhere (layout drift signal).</param>
    public static string Categorize(
        string? parseStatus,
        bool conversionFailed = false,
        bool requiredLabelsFoundElsewhere = false)
    {
        if (string.IsNullOrEmpty(parseStatus))
            return ParseFailureCategory.None;

        if (string.Equals(parseStatus, "Success", StringComparison.OrdinalIgnoreCase))
            return ParseFailureCategory.None;

        if (string.Equals(parseStatus, "ParseError", StringComparison.OrdinalIgnoreCase))
            return conversionFailed ? ParseFailureCategory.ConversionIssue : ParseFailureCategory.ParseError;

        if (string.Equals(parseStatus, "FailedValidation", StringComparison.OrdinalIgnoreCase))
            return ParseFailureCategory.ValidationFail;

        if (string.Equals(parseStatus, "FailedRequiredFields", StringComparison.OrdinalIgnoreCase))
            return requiredLabelsFoundElsewhere ? ParseFailureCategory.LayoutDrift : ParseFailureCategory.DataMissing;

        return ParseFailureCategory.ParseError;
    }
}
