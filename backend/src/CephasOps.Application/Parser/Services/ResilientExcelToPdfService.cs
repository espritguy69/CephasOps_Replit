using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CephasOps.Application.Parser.Services.Converters;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Resilient Excel to PDF conversion service with automatic fallback
/// Attempts Syncfusion first (high quality), falls back to ExcelDataReader+QuestPDF for corrupted files
/// </summary>
public class ResilientExcelToPdfService : IExcelToPdfService
{
    private readonly IExcelToPdfConverter _primaryConverter;
    private readonly IExcelToPdfConverter _fallbackConverter;
    private readonly ILogger<ResilientExcelToPdfService> _logger;

    public ResilientExcelToPdfService(
        SyncfusionExcelToPdfConverter primaryConverter,
        ExcelDataReaderToPdfConverter fallbackConverter,
        ILogger<ResilientExcelToPdfService> logger)
    {
        _primaryConverter = primaryConverter;
        _fallbackConverter = fallbackConverter;
        _logger = logger;
    }

    public async Task<byte[]> ConvertToPdfAsync(IFormFile excelFile, CancellationToken cancellationToken = default)
    {
        // Try primary converter first (Syncfusion)
        try
        {
            _logger.LogInformation("Attempting PDF conversion with {Converter} for {FileName}",
                _primaryConverter.ConverterName, excelFile.FileName);
            
            var pdfBytes = await _primaryConverter.ConvertToPdfAsync(excelFile, cancellationToken);
            
            _logger.LogInformation("✅ PDF conversion successful using {Converter} for {FileName}",
                _primaryConverter.ConverterName, excelFile.FileName);
            
            return pdfBytes;
        }
        catch (Exception primaryEx)
        {
            // Check if this is an OLE2/FAT structure error (corrupted file)
            var errorText = primaryEx.ToString().ToUpperInvariant();
            var isFileStructureError = errorText.Contains("FAT.GETSTREAM") ||
                                      errorText.Contains("COMPOUNDFILE") ||
                                      errorText.Contains("ENTRYSTREAM") ||
                                      errorText.Contains("OLE2") ||
                                      errorText.Contains("FILE STRUCTURE");

            if (isFileStructureError)
            {
                _logger.LogWarning(primaryEx,
                    "Syncfusion failed with file structure error for {FileName}. " +
                    "Attempting fallback to {FallbackConverter}",
                    excelFile.FileName, _fallbackConverter.ConverterName);

                // Try fallback converter
                try
                {
                    // Note: IFormFile streams are read-once, so we need to create a new form file wrapper
                    // The fallback converter will read the file again internally
                    var pdfBytes = await _fallbackConverter.ConvertToPdfAsync(excelFile, cancellationToken);
                    
                    _logger.LogInformation("✅ PDF conversion successful using fallback {Converter} for {FileName}",
                        _fallbackConverter.ConverterName, excelFile.FileName);
                    
                    return pdfBytes;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx,
                        "❌ Both converters failed for {FileName}. Primary: {PrimaryError}, Fallback: {FallbackError}",
                        excelFile.FileName, primaryEx.Message, fallbackEx.Message);
                    
                    throw new Exception(
                        $"Excel to PDF conversion failed with both converters. " +
                        $"Primary ({_primaryConverter.ConverterName}): {primaryEx.Message}. " +
                        $"Fallback ({_fallbackConverter.ConverterName}): {fallbackEx.Message}",
                        fallbackEx);
                }
            }
            else
            {
                // Not a file structure error - don't try fallback, just throw
                _logger.LogError(primaryEx,
                    "❌ Syncfusion conversion failed for {FileName} with non-structure error: {Error}",
                    excelFile.FileName, primaryEx.Message);
                
                throw;
            }
        }
    }
}

