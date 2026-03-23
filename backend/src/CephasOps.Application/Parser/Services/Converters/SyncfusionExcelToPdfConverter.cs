using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using Syncfusion.Pdf;

namespace CephasOps.Application.Parser.Services.Converters;

/// <summary>
/// Primary Excel to PDF converter using Syncfusion
/// Provides high-quality conversion with full formatting preservation
/// </summary>
public class SyncfusionExcelToPdfConverter : IExcelToPdfConverter
{
    private readonly ILogger<SyncfusionExcelToPdfConverter> _logger;

    public string ConverterName => "Syncfusion";

    public SyncfusionExcelToPdfConverter(ILogger<SyncfusionExcelToPdfConverter> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ConvertToPdfAsync(IFormFile excelFile, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting Excel to PDF using Syncfusion: {FileName}", excelFile.FileName);

            // Read file into byte array first to ensure clean stream handling
            byte[] fileBytes;
            using (var tempStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(tempStream, cancellationToken);
                fileBytes = tempStream.ToArray();
            }

            // Create Excel engine
            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            
            // Handle different Excel versions
            var extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (extension == ".xlsx" || extension == ".xlsm")
            {
                application.DefaultVersion = ExcelVersion.Excel2016;
            }
            else
            {
                // Default to Excel 97/2003 for .xls and any other format
                application.DefaultVersion = ExcelVersion.Excel97to2003;
            }

            // Open Excel file with error handling
            IWorkbook? workbook = null;
            Exception? lastException = null;
            
            // Try with Automatic first (most forgiving), then fallback to direct open
            try
            {
                using var stream = new MemoryStream(fileBytes, writable: false);
                workbook = application.Workbooks.Open(stream, ExcelOpenType.Automatic);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogDebug("Automatic open failed, trying direct open: {Error}", ex.Message);
                
                // Fallback to direct open
                try
                {
                    using var stream = new MemoryStream(fileBytes, writable: false);
                    workbook = application.Workbooks.Open(stream);
                }
                catch (Exception directEx)
                {
                    _logger.LogWarning("Direct open also failed: {Error}", directEx.Message);
                    throw new Exception($"Syncfusion failed to open Excel file: {directEx.Message}", directEx);
                }
            }
            
            if (workbook == null)
            {
                throw lastException ?? new Exception("Failed to open Excel file for PDF conversion");
            }

            _logger.LogInformation("Excel opened successfully: {Sheets} sheets, First sheet: {Rows} rows, {Cols} columns",
                workbook.Worksheets.Count,
                workbook.Worksheets[0].UsedRange.LastRow,
                workbook.Worksheets[0].UsedRange.LastColumn);

            // Configure page settings for all worksheets BEFORE conversion
            foreach (var worksheet in workbook.Worksheets)
            {
                // Set landscape orientation for better form display
                worksheet.PageSetup.Orientation = ExcelPageOrientation.Landscape;
                // Fit to 1 page wide AND 1 page tall (single page snapshot for verification)
                worksheet.PageSetup.FitToPagesWide = 1;
                worksheet.PageSetup.FitToPagesTall = 1;
            }

            // Convert entire workbook to PDF
            var renderer = new XlsIORenderer();
            var pdfDocument = renderer.ConvertToPDF(workbook);

            // Save PDF to byte array
            using var outputStream = new MemoryStream();
            pdfDocument.Save(outputStream);
            pdfDocument.Close(true);
            workbook.Close();

            var pdfBytes = outputStream.ToArray();
            
            _logger.LogInformation("✅ Syncfusion conversion successful: {FileName}, PDF Size: {Size} KB",
                excelFile.FileName, pdfBytes.Length / 1024);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Syncfusion conversion failed: {FileName}", excelFile.FileName);
            throw;
        }
    }
}

