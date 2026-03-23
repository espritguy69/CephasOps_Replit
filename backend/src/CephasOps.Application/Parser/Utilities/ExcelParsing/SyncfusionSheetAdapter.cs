using Syncfusion.XlsIO;

namespace CephasOps.Application.Parser.Utilities.ExcelParsing;

/// <summary>
/// Wraps Syncfusion IWorksheet as ISheetCellReader for sheet scoring and header detection.
/// </summary>
public sealed class SyncfusionSheetAdapter : ISheetCellReader
{
    private readonly IWorksheet _sheet;

    public SyncfusionSheetAdapter(IWorksheet sheet)
    {
        _sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
    }

    public int LastRow => _sheet.UsedRange?.LastRow ?? 0;
    public int LastColumn => _sheet.UsedRange?.LastColumn ?? 0;

    public string? GetCellText(int row1Based, int col1Based)
    {
        try
        {
            var range = _sheet.Range[row1Based, col1Based];
            var text = range?.Text?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }
}
