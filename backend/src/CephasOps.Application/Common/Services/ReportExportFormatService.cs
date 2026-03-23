using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Syncfusion.XlsIO;

namespace CephasOps.Application.Common.Services;

/// <summary>
/// Exports report rows to Excel (.xlsx) and PDF. Uses Syncfusion XlsIO and QuestPDF.
/// </summary>
public class ReportExportFormatService : IReportExportFormatService
{
    private readonly ILogger<ReportExportFormatService> _logger;

    static ReportExportFormatService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportExportFormatService(ILogger<ReportExportFormatService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public byte[] ExportToExcelBytes<T>(IEnumerable<T> rows, string sheetName) where T : class
    {
        var list = rows?.ToList() ?? new List<T>();
        var props = GetOrderedProperties<T>();

        using var excelEngine = new ExcelEngine();
        var application = excelEngine.Excel;
        application.DefaultVersion = ExcelVersion.Excel2016;
        var workbook = application.Workbooks.Create(1);
        var sheet = workbook.Worksheets[0];
        sheet.Name = SanitizeSheetName(sheetName);

        // Header row
        for (int c = 0; c < props.Count; c++)
        {
            sheet.Range[1, c + 1].Text = props[c].Name;
            sheet.Range[1, c + 1].CellStyle.Font.Bold = true;
        }

        // Data rows
        int rowIndex = 2;
        foreach (var row in list)
        {
            for (int c = 0; c < props.Count; c++)
            {
                var value = GetPropertyValue(props[c], row);
                sheet.Range[rowIndex, c + 1].Text = value ?? string.Empty;
            }
            rowIndex++;
        }

        // Auto-fit columns (optional, limit width to avoid huge columns)
        sheet.UsedRange.AutofitColumns();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Close();
        return stream.ToArray();
    }

    /// <inheritdoc />
    public byte[] ExportToPdfBytes<T>(IEnumerable<T> rows, string reportTitle, DateTime? generatedAt = null) where T : class
    {
        var list = rows?.ToList() ?? new List<T>();
        var props = GetOrderedProperties<T>();
        var generated = generatedAt ?? DateTime.UtcNow;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);

                page.Header().Column(column =>
                {
                    column.Item().Text(reportTitle).FontSize(14).Bold().AlignCenter();
                    column.Item().Text($"Generated: {generated:yyyy-MM-dd HH:mm:ss} UTC").FontSize(10).AlignCenter().FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < props.Count; i++)
                                columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            foreach (var p in props)
                            {
                                header.Cell()
                                    .Background(Colors.Blue.Medium)
                                    .Padding(6)
                                    .Text(PascalToLabel(p.Name))
                                    .FontColor(Colors.White)
                                    .FontSize(9)
                                    .Bold()
                                    .AlignCenter();
                            }
                        });

                        // Use non-breaking space for empty cells to avoid QuestPDF layout/zero-height issues (Issue #116, #920).
                        const string emptyCellPlaceholder = "\u00A0";
                        foreach (var row in list)
                        {
                            foreach (var p in props)
                            {
                                var value = GetPropertyValue(p, row);
                                var cellText = string.IsNullOrEmpty(value) ? emptyCellPlaceholder : value;
                                table.Cell()
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4)
                                    .Text(cellText)
                                    .FontSize(8)
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

        return doc.GeneratePdf();
    }

    private static List<(string Name, PropertyInfo Info)> GetOrderedProperties<T>() where T : class
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name)
            .Select(p => (p.Name, p))
            .ToList();
    }

    private static string? GetPropertyValue((string Name, PropertyInfo Info) prop, object row)
    {
        try
        {
            var v = prop.Info.GetValue(row);
            if (v == null) return null;
            if (v is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
            return v.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
        var s = string.Concat(name.Take(31)).Trim();
        return string.IsNullOrEmpty(s) ? "Sheet1" : s;
    }

    private static string PascalToLabel(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
