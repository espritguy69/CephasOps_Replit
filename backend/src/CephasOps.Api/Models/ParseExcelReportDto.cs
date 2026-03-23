namespace CephasOps.Api.Models;

/// <summary>
/// Read-only report from parsing a test Excel file with ExcelDataReader (no Syncfusion).
/// </summary>
public class ParseExcelReportDto
{
    public string SheetName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int HeaderRowIndex { get; set; }
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, object?>> First20Rows { get; set; } = new();
    public int NullOrEmptyRowCount { get; set; }
    public Dictionary<string, int> ColumnStats { get; set; } = new();
    public List<string> DetectedKeyFields { get; set; } = new();
}
