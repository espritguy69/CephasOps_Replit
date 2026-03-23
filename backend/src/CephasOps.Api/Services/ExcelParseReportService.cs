using System.Text;
using CephasOps.Api.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Hosting;

namespace CephasOps.Api.Services;

/// <summary>
/// Read-only Excel parse report using ExcelDataReader only (no Syncfusion).
/// Parses backend/test-data/A1862604.xls and returns a JSON-friendly report.
/// </summary>
public class ExcelParseReportService
{
    private readonly IWebHostEnvironment _env;
    private const string TestFileName = "A1862604.xls";
    private const string TestDataFolder = "test-data";

    private static readonly HashSet<string> KeyFieldPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "OrderNo", "Order No", "OrderNumber", "Order Number",
        "CustomerName", "Customer Name", "Customer",
        "Address", "Address1", "Address2", "Location",
        "Phone", "Contact", "Mobile", "Tel",
        "Package", "Plan", "Product",
        "Devices", "Device", "Equipment", "Serial"
    };

    public ExcelParseReportService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public ParseExcelReportDto GetReport()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var contentRoot = _env.ContentRootPath;
        // From Api project: go up to backend, then test-data
        var backendDir = Path.GetFullPath(Path.Combine(contentRoot, "..", ".."));
        var filePath = Path.Combine(backendDir, TestDataFolder, TestFileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Test file not found: {filePath}");

        var bytes = File.ReadAllBytes(filePath);
        using var stream = new MemoryStream(bytes);
        IExcelDataReader reader = ExcelReaderFactory.CreateBinaryReader(stream);
        var dataSet = reader.AsDataSet();
        reader.Close();

        if (dataSet.Tables.Count == 0)
            return new ParseExcelReportDto { SheetName = "(no sheets)" };

        var table = dataSet.Tables[0]!;
        var sheetName = table.TableName;
        var totalRows = table.Rows.Count;
        var headerRowIndex = DetectHeaderRow(table);
        var headers = GetHeaders(table, headerRowIndex);
        var dataStartRow = headerRowIndex + 1;
        var first20Rows = GetFirst20RowsAsKeyValue(table, headers, dataStartRow);
        var nullOrEmptyRowCount = CountNullOrEmptyRows(table, dataStartRow, headers.Count);
        var columnStats = GetColumnStats(table, headers, dataStartRow);
        var detectedKeyFields = DetectKeyFields(headers);

        return new ParseExcelReportDto
        {
            SheetName = sheetName,
            TotalRows = totalRows,
            HeaderRowIndex = headerRowIndex,
            Headers = headers,
            First20Rows = first20Rows,
            NullOrEmptyRowCount = nullOrEmptyRowCount,
            ColumnStats = columnStats,
            DetectedKeyFields = detectedKeyFields
        };
    }

    private static int DetectHeaderRow(System.Data.DataTable table)
    {
        if (table.Rows.Count == 0) return 0;
        // Find first row that has at least 2 non-empty string-like cells (candidate header)
        for (int r = 0; r < Math.Min(10, table.Rows.Count); r++)
        {
            var row = table.Rows[r];
            int nonEmpty = 0;
            for (int c = 0; c < table.Columns.Count; c++)
            {
                var v = row[c];
                if (v != null && v != DBNull.Value && !string.IsNullOrWhiteSpace(v.ToString()))
                    nonEmpty++;
            }
            if (nonEmpty >= 2)
                return r;
        }
        return 0;
    }

    private static List<string> GetHeaders(System.Data.DataTable table, int headerRowIndex)
    {
        var list = new List<string>();
        var row = table.Rows[headerRowIndex];
        for (int c = 0; c < table.Columns.Count; c++)
        {
            var v = row[c];
            var s = (v != null && v != DBNull.Value) ? v.ToString()?.Trim() ?? "" : "";
            if (string.IsNullOrEmpty(s))
                s = $"Column{c + 1}";
            list.Add(s);
        }
        return list;
    }

    private static List<Dictionary<string, object?>> GetFirst20RowsAsKeyValue(
        System.Data.DataTable table,
        List<string> headers,
        int dataStartRow)
    {
        var result = new List<Dictionary<string, object?>>();
        int count = 0;
        for (int r = dataStartRow; r < table.Rows.Count && count < 20; r++)
        {
            var row = table.Rows[r];
            var dict = new Dictionary<string, object?>();
            for (int c = 0; c < headers.Count && c < table.Columns.Count; c++)
            {
                var key = headers[c];
                var v = row[c];
                if (v == null || v == DBNull.Value)
                    dict[key] = null;
                else
                    dict[key] = v is DateTime dt ? dt.ToString("O") : v.ToString();
            }
            result.Add(dict);
            count++;
        }
        return result;
    }

    private static int CountNullOrEmptyRows(System.Data.DataTable table, int dataStartRow, int headerCount)
    {
        int empty = 0;
        for (int r = dataStartRow; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            bool allEmpty = true;
            for (int c = 0; c < headerCount && c < table.Columns.Count; c++)
            {
                var v = row[c];
                if (v != null && v != DBNull.Value && !string.IsNullOrWhiteSpace(v.ToString()))
                {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty) empty++;
        }
        return empty;
    }

    private static Dictionary<string, int> GetColumnStats(System.Data.DataTable table, List<string> headers, int dataStartRow)
    {
        var stats = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var h in headers)
            stats[h] = 0;

        for (int r = dataStartRow; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            for (int c = 0; c < headers.Count && c < table.Columns.Count; c++)
            {
                var v = row[c];
                if (v == null || v == DBNull.Value || string.IsNullOrWhiteSpace(v.ToString()))
                    stats[headers[c]]++;
            }
        }
        return stats;
    }

    private static List<string> DetectKeyFields(List<string> headers)
    {
        var detected = new List<string>();
        foreach (var h in headers)
        {
            var normalized = h.Trim();
            if (string.IsNullOrEmpty(normalized)) continue;
            foreach (var pattern in KeyFieldPatterns)
            {
                if (normalized.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                    normalized.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    detected.Add(h);
                    break;
                }
            }
        }
        return detected;
    }
}
