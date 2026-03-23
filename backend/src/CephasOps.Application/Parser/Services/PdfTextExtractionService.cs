using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service to extract text from PDF files
/// </summary>
public interface IPdfTextExtractionService
{
    /// <summary>
    /// Extract all text from a PDF file
    /// </summary>
    Task<string> ExtractTextAsync(IFormFile pdfFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract all text from a PDF byte array
    /// </summary>
    Task<string> ExtractTextFromBytesAsync(byte[] pdfBytes, CancellationToken cancellationToken = default);
}

public class PdfTextExtractionService : IPdfTextExtractionService
{
    private readonly ILogger<PdfTextExtractionService> _logger;

    public PdfTextExtractionService(ILogger<PdfTextExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(IFormFile pdfFile, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await pdfFile.CopyToAsync(stream, cancellationToken);
        return await ExtractTextFromBytesAsync(stream.ToArray(), cancellationToken);
    }

    public async Task<string> ExtractTextFromBytesAsync(byte[] pdfBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var textBuilder = new System.Text.StringBuilder();

            foreach (var page in document.GetPages())
            {
                var words = page.GetWords();
                foreach (var word in words)
                {
                    textBuilder.Append(word.Text);
                    textBuilder.Append(" ");
                }
                textBuilder.AppendLine(); // New line for each page
            }

            var extractedText = textBuilder.ToString();
            _logger.LogInformation("Extracted {Length} characters from PDF", extractedText.Length);
            
            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF");
            throw;
        }
    }
}

