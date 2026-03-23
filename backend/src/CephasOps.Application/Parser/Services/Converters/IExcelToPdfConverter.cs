using Microsoft.AspNetCore.Http;

namespace CephasOps.Application.Parser.Services.Converters;

/// <summary>
/// Interface for Excel to PDF conversion
/// Implementations should handle specific conversion strategies
/// </summary>
public interface IExcelToPdfConverter
{
    /// <summary>
    /// Convert Excel file to PDF
    /// </summary>
    /// <param name="excelFile">Excel file to convert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF as byte array</returns>
    Task<byte[]> ConvertToPdfAsync(IFormFile excelFile, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the name of this converter (for logging purposes)
    /// </summary>
    string ConverterName { get; }
}

