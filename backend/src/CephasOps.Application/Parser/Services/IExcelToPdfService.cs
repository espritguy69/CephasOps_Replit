using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service to convert Excel files to PDF for parsing
/// </summary>
public interface IExcelToPdfService
{
    /// <summary>
    /// Convert Excel file (.xls or .xlsx) to PDF
    /// </summary>
    Task<byte[]> ConvertToPdfAsync(IFormFile excelFile, CancellationToken cancellationToken = default);
}

