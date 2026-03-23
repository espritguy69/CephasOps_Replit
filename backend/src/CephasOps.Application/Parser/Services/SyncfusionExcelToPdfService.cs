using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using Syncfusion.XlsIORenderer;
using Syncfusion.Pdf;
using Syncfusion.Drawing;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// High-quality Excel to PDF conversion using Syncfusion
/// Replaces PdfSharpCore with professional-grade conversion
/// - No column/row truncation (was limited to 15 cols x 50 rows)
/// - Preserves all formatting (colors, fonts, borders, merged cells)
/// - Perfect fidelity to original Excel form
/// </summary>
public class SyncfusionExcelToPdfService : IExcelToPdfService
{
    private readonly ILogger<SyncfusionExcelToPdfService> _logger;

    public SyncfusionExcelToPdfService(ILogger<SyncfusionExcelToPdfService> logger)
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
            
            // Handle different Excel versions - Default to Excel 97/2003 (.xls)
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

            // Open Excel file with Automatic type detection (handles corrupted/non-standard files better)
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
                using var stream = new MemoryStream(fileBytes, writable: false);
                workbook = application.Workbooks.Open(stream);
            }
            
            if (workbook == null)
            {
                throw lastException ?? new Exception("Failed to open Excel file for PDF conversion");
            }

            _logger.LogInformation("Excel opened: {Sheets} sheets, {Rows} rows, {Cols} columns",
                workbook.Worksheets.Count,
                workbook.Worksheets[0].UsedRange.LastRow,
                workbook.Worksheets[0].UsedRange.LastColumn);

            // Configure page settings for all worksheets BEFORE conversion
            foreach (var worksheet in workbook.Worksheets)
            {
                // Set landscape orientation for better form display
                worksheet.PageSetup.Orientation = ExcelPageOrientation.Landscape;
                // ✅ Fit to 1 page wide AND 1 page tall (ensures single page, all content visible)
                worksheet.PageSetup.FitToPagesWide = 1;
                worksheet.PageSetup.FitToPagesTall = 1; // Changed from 0 to 1
                // Optional: Set zoom to ensure readability (Syncfusion will auto-scale to fit)
                worksheet.PageSetup.Zoom = 0; // 0 means auto-fit based on FitToPages settings
            }

            // ✅ FIX: Use the simpler, official Syncfusion pattern
            // Convert entire workbook to PDF at once (recommended approach from Syncfusion examples)
            // This is more efficient and handles all worksheets automatically
            var renderer = new XlsIORenderer();
            var pdfDocument = renderer.ConvertToPDF(workbook);

            // Save PDF to byte array
            using var outputStream = new MemoryStream();
            pdfDocument.Save(outputStream);
            pdfDocument.Close(true);
            workbook.Close();

            var pdfBytes = outputStream.ToArray();
            
            _logger.LogInformation("✅ Excel converted to PDF successfully: {FileName}, Size: {Size} KB",
                excelFile.FileName, pdfBytes.Length / 1024);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error converting Excel to PDF: {FileName}", excelFile.FileName);
            throw;
        }
    }
}

