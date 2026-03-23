using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using System.Text;

namespace CephasOps.Application.Parser.Services.Converters;

/// <summary>
/// Converts legacy .xls files to modern .xlsx format
/// Uses ExcelDataReader to read .xls (handles legacy files well) and Syncfusion to create .xlsx
/// Used for email parsing to ensure reliable conversion of .xls attachments
/// </summary>
public class ExcelFormatConverter
{
    private readonly ILogger<ExcelFormatConverter> _logger;

    public ExcelFormatConverter(ILogger<ExcelFormatConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert .xls file to .xlsx format using ExcelDataReader + Syncfusion
    /// ExcelDataReader reads the data, Syncfusion creates the .xlsx file
    /// </summary>
    public async Task<byte[]> ConvertXlsToXlsxAsync(byte[] xlsBytes, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting .xls to .xlsx using ExcelDataReader: {FileName} ({Size} bytes)", fileName, xlsBytes.Length);
        
        // ExcelDataReader requires CodePages encoding for .xls files
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        
        // Step 1: Read .xls with ExcelDataReader (handles legacy/corrupted files well)
        using var xlsStream = new MemoryStream(xlsBytes);
        IExcelDataReader? reader = null;
        
        try
        {
            reader = ExcelReaderFactory.CreateBinaryReader(xlsStream);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create ExcelDataReader binary reader: {Error}", ex.Message);
            xlsStream.Position = 0;
            reader = ExcelReaderFactory.CreateOpenXmlReader(xlsStream);
        }
        
        if (reader == null)
        {
            throw new InvalidOperationException($"Failed to create ExcelDataReader for file '{fileName}'. The file may be corrupted or in an unsupported format.");
        }
        
        var dataSet = reader.AsDataSet();
        reader.Close();
        
        if (dataSet.Tables.Count == 0)
        {
            throw new InvalidOperationException($"No data tables found in .xls file '{fileName}'. The file may be empty or corrupted.");
        }
        
        var dataTable = dataSet.Tables[0];
        _logger.LogInformation("Read .xls file with ExcelDataReader: {Rows} rows, {Cols} columns", dataTable.Rows.Count, dataTable.Columns.Count);
        
        // Step 2: Create new Syncfusion workbook (.xlsx format)
        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016; // .xlsx format
        
        var workbook = application.Workbooks.Create(1);
        var worksheet = workbook.Worksheets[0];
        
        // Step 3: Copy data from DataTable to Syncfusion workbook
        _logger.LogInformation("Copying data to .xlsx format (formatting may be lost, but data preserved)...");
        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var value = dataTable.Rows[row][col];
                if (value != null && value != DBNull.Value)
                {
                    var cellValue = value.ToString();
                    if (!string.IsNullOrEmpty(cellValue))
                    {
                        worksheet.Range[row + 1, col + 1].Text = cellValue;
                    }
                }
            }
        }
        
        _logger.LogInformation("Data copied: {Rows} rows, {Cols} columns", dataTable.Rows.Count, dataTable.Columns.Count);
        
        // Step 4: Save as .xlsx to memory stream (DefaultVersion is already set to Excel2016)
        using var xlsxStream = new MemoryStream();
        workbook.SaveAs(xlsxStream);
        workbook.Close();
        
        var xlsxBytes = xlsxStream.ToArray();
        _logger.LogInformation("✅ Converted to .xlsx using ExcelDataReader+Syncfusion: {OriginalSize} bytes -> {XlsxSize} bytes", 
            xlsBytes.Length, xlsxBytes.Length);
        
        return xlsxBytes;
    }

    /// <summary>
    /// Convert IFormFile from .xls to .xlsx format
    /// </summary>
    public async Task<IFormFile> ConvertXlsFormFileToXlsxAsync(IFormFile xlsFile, CancellationToken cancellationToken = default)
    {
        // Read original file
        byte[] xlsBytes;
        using (var stream = new MemoryStream())
        {
            await xlsFile.CopyToAsync(stream, cancellationToken);
            xlsBytes = stream.ToArray();
        }

        // Convert to .xlsx
        var xlsxBytes = await ConvertXlsToXlsxAsync(xlsBytes, xlsFile.FileName, cancellationToken);
        
        // Create new IFormFile with .xlsx extension
        var xlsxFileName = Path.ChangeExtension(xlsFile.FileName, ".xlsx");
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        
        return new InMemoryFormFile(xlsxBytes, xlsxFileName, contentType);
    }
}

/// <summary>
/// In-memory implementation of IFormFile for converted files
/// </summary>
public class InMemoryFormFile : IFormFile
{
    private readonly byte[] _bytes;
    private readonly string _fileName;
    private readonly string _contentType;

    public InMemoryFormFile(byte[] bytes, string fileName, string contentType)
    {
        _bytes = bytes;
        _fileName = fileName;
        _contentType = contentType;
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => _bytes.Length;
    public string Name => "file";
    public string FileName => _fileName;

    public Stream OpenReadStream()
    {
        return new MemoryStream(_bytes, writable: false);
    }

    public void CopyTo(Stream target)
    {
        target.Write(_bytes, 0, _bytes.Length);
    }

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        return target.WriteAsync(_bytes, 0, _bytes.Length, cancellationToken);
    }
}

