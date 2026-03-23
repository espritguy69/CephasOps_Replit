using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ExcelDataReader;
using System.Data;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF;

namespace CephasOps.Application.Parser.Services.Converters;

/// <summary>
/// Fallback Excel to PDF converter using ExcelDataReader + QuestPDF
/// Handles corrupted .xls files with broken OLE2/FAT structure that Syncfusion cannot read
/// </summary>
public class ExcelDataReaderToPdfConverter : IExcelToPdfConverter
{
    private readonly ILogger<ExcelDataReaderToPdfConverter> _logger;

    public string ConverterName => "ExcelDataReader+QuestPDF";

    public ExcelDataReaderToPdfConverter(ILogger<ExcelDataReaderToPdfConverter> logger)
    {
        _logger = logger;
        
        // Ensure CodePages encoding is registered (required for .xls files)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        // Set QuestPDF license (if using commercial version, otherwise free version)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ConvertToPdfAsync(IFormFile excelFile, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting Excel to PDF using ExcelDataReader+QuestPDF: {FileName}", excelFile.FileName);

            // Read file into byte array
            byte[] fileBytes;
            using (var tempStream = new MemoryStream())
            {
                await excelFile.CopyToAsync(tempStream, cancellationToken);
                fileBytes = tempStream.ToArray();
            }

            // Read Excel file using ExcelDataReader
            DataTable? dataTable = null;
            using (var stream = new MemoryStream(fileBytes, writable: false))
            {
                IExcelDataReader? reader = null;
                
                var extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
                if (extension == ".xls")
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (extension == ".xlsx" || extension == ".xlsm")
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported file extension: {extension}");
                }

                if (reader == null)
                {
                    throw new Exception("Failed to create ExcelDataReader");
                }

                try
                {
                    // Configure reader
                    var configuration = new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true // First row contains headers
                        }
                    };

                    // Read into DataSet
                    var dataSet = reader.AsDataSet(configuration);

                    if (dataSet.Tables.Count == 0)
                    {
                        throw new Exception("No worksheets found in Excel file");
                    }

                    // Get first worksheet as DataTable
                    dataTable = dataSet.Tables[0];
                    
                    _logger.LogInformation("ExcelDataReader read successfully: {Rows} rows, {Cols} columns",
                        dataTable.Rows.Count, dataTable.Columns.Count);
                }
                finally
                {
                    reader?.Close();
                }
            }

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                throw new Exception("Excel file is empty or could not be read");
            }

            // Generate PDF using QuestPDF
            var pdfBytes = GeneratePdfFromDataTable(dataTable, excelFile.FileName);
            
            _logger.LogInformation("✅ ExcelDataReader+QuestPDF conversion successful: {FileName}, PDF Size: {Size} KB",
                excelFile.FileName, pdfBytes.Length / 1024);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ExcelDataReader+QuestPDF conversion failed: {FileName}", excelFile.FileName);
            throw;
        }
    }

    private byte[] GeneratePdfFromDataTable(DataTable dataTable, string fileName)
    {
        var pdfDocument = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                
                page.Header().Column(column =>
                {
                    column.Item().Text($"Excel File: {fileName}")
                        .FontSize(14)
                        .Bold()
                        .AlignCenter();
                    
                    column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(10)
                        .AlignCenter()
                        .FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        // Define columns - equal width for all columns
                        var columnCount = dataTable.Columns.Count;
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < columnCount; i++)
                            {
                                columns.RelativeColumn(1);
                            }
                        });

                        // Header row
                        table.Header(header =>
                        {
                            foreach (DataColumn col in dataTable.Columns)
                            {
                                header.Cell()
                                    .Background(Colors.Blue.Medium)
                                    .Padding(8)
                                    .Text(col.ColumnName)
                                    .FontColor(Colors.White)
                                    .FontSize(10)
                                    .Bold()
                                    .AlignCenter();
                            }
                        });

                        // Data rows
                        foreach (DataRow row in dataTable.Rows)
                        {
                            foreach (var cellValue in row.ItemArray)
                            {
                                table.Cell()
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(6)
                                    .Text(cellValue?.ToString() ?? "")
                                    .FontSize(9)
                                    .AlignLeft();
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        // Generate PDF bytes
        return pdfDocument.GeneratePdf();
    }
}

