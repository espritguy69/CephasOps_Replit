namespace CephasOps.Application.Common.Services;

/// <summary>
/// Exports report rows to Excel (.xlsx) and PDF formats. CSV remains via ICsvService.
/// </summary>
public interface IReportExportFormatService
{
    /// <summary>Export rows to Excel workbook (one sheet, header + data).</summary>
    byte[] ExportToExcelBytes<T>(IEnumerable<T> rows, string sheetName) where T : class;

    /// <summary>Export rows to PDF (title, timestamp, table).</summary>
    byte[] ExportToPdfBytes<T>(IEnumerable<T> rows, string reportTitle, DateTime? generatedAt = null) where T : class;
}
