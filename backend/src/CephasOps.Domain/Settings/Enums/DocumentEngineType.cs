namespace CephasOps.Domain.Settings.Enums;

/// <summary>
/// Document rendering engine types
/// </summary>
public enum DocumentEngineType
{
    /// <summary>
    /// Handlebars.Net template rendering + QuestPDF for PDF generation (default)
    /// </summary>
    Handlebars = 0,

    /// <summary>
    /// Carbone engine with HTML template (stored in HtmlBody)
    /// </summary>
    CarboneHtml = 1,

    /// <summary>
    /// Carbone engine with DOCX/ODT template file (stored via TemplateFileId)
    /// </summary>
    CarboneDocx = 2
}

/// <summary>
/// Extension methods for DocumentEngineType
/// </summary>
public static class DocumentEngineTypeExtensions
{
    /// <summary>
    /// Parse engine string to enum with safe fallback to Handlebars
    /// </summary>
    public static DocumentEngineType ParseEngineType(string? engine)
    {
        if (string.IsNullOrWhiteSpace(engine))
            return DocumentEngineType.Handlebars;

        return engine.Trim().ToLowerInvariant() switch
        {
            "handlebars" => DocumentEngineType.Handlebars,
            "carbonehtml" => DocumentEngineType.CarboneHtml,
            "carbonedocx" => DocumentEngineType.CarboneDocx,
            // Legacy values map to Handlebars
            "razor" => DocumentEngineType.Handlebars,
            "liquid" => DocumentEngineType.Handlebars,
            _ => DocumentEngineType.Handlebars
        };
    }

    /// <summary>
    /// Check if engine requires Carbone service
    /// </summary>
    public static bool RequiresCarbone(this DocumentEngineType engineType)
    {
        return engineType == DocumentEngineType.CarboneHtml || engineType == DocumentEngineType.CarboneDocx;
    }
}

