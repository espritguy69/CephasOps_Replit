namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Interface for Carbone document rendering
/// </summary>
public interface ICarboneRenderer
{
    /// <summary>
    /// Whether Carbone is configured and enabled
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Render PDF from HTML template using Carbone
    /// </summary>
    /// <param name="htmlTemplate">HTML template content with Carbone placeholders</param>
    /// <param name="data">Data model to merge with template</param>
    /// <param name="documentType">Document type for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF bytes</returns>
    Task<byte[]> RenderFromHtmlAsync(
        string htmlTemplate, 
        object data, 
        string documentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Render PDF from DOCX/ODT template file using Carbone
    /// </summary>
    /// <param name="templateFileId">File ID of the template (DOCX/ODT)</param>
    /// <param name="data">Data model to merge with template</param>
    /// <param name="documentType">Document type for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF bytes</returns>
    Task<byte[]> RenderFromFileAsync(
        Guid templateFileId, 
        object data, 
        string documentType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Exception thrown when Carbone is not configured but required
/// </summary>
public class CarboneNotConfiguredException : InvalidOperationException
{
    public CarboneNotConfiguredException()
        : base("Carbone engine is selected but Carbone is not configured. " +
               "Please configure Carbone in appsettings.json under the 'Carbone' section, " +
               "or change the template engine to 'Handlebars'.")
    {
    }

    public CarboneNotConfiguredException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when Carbone API call fails
/// </summary>
public class CarboneRenderException : Exception
{
    public string? DocumentType { get; }
    public int? StatusCode { get; }

    public CarboneRenderException(string message, string? documentType = null, int? statusCode = null)
        : base(message)
    {
        DocumentType = documentType;
        StatusCode = statusCode;
    }

    public CarboneRenderException(string message, Exception innerException, string? documentType = null)
        : base(message, innerException)
    {
        DocumentType = documentType;
    }
}

