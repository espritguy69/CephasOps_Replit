namespace CephasOps.Application.Parser.Utilities.ExcelParsing;

/// <summary>
/// Abstraction for reading cell text from a worksheet (Syncfusion IWorksheet or in-memory test sheet).
/// </summary>
public interface ISheetCellReader
{
    int LastRow { get; }
    int LastColumn { get; }
    string? GetCellText(int row1Based, int col1Based);
}
