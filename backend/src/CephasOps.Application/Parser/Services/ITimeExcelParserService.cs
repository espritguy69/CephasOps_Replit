using CephasOps.Application.Parser.DTOs;
using Microsoft.AspNetCore.Http;
using System.Data;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Parser service interface for TIME Excel order forms (Activation, Modification, etc.)
/// Supports both .xls (Excel 97-2003) and .xlsx (Excel 2007+) formats
/// </summary>
public interface ITimeExcelParserService
{
    /// <summary>
    /// Parse a TIME Excel file and extract order data. Optional profile context provides hints (Phase 8).
    /// </summary>
    Task<TimeExcelParseResult> ParseAsync(IFormFile file, TemplateProfileContext? profileContext = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Detect the order type from filename and/or content
    /// </summary>
    string DetectOrderType(string fileName, DataTable? worksheet = null);
}

