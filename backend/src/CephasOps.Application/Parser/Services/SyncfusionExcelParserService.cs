using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services.Converters;
using CephasOps.Application.Parser.Utilities;
using CephasOps.Application.Parser.Utilities.ExcelParsing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Syncfusion.XlsIO;
using ExcelDataReader;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Security.Cryptography;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Enhanced Excel parser using Syncfusion XlsIO for superior Excel reading and parsing
/// Fixes timezone handling (GMT+8), VOIP ID extraction, and material extraction.
/// .xls: unified pipeline = convert to .xlsx via ExcelFormatConverter then parse with Syncfusion only.
/// Structure Gate: parse is NOT USABLE when required fields are missing (confidence 0, FailedRequiredFields).
/// </summary>
public interface ISyncfusionExcelParserService
{
    Task<TimeExcelParseResult> ParseAsync(IFormFile file, TemplateProfileContext? profileContext = null, CancellationToken cancellationToken = default);
    string DetectOrderType(string fileName, IWorksheet? worksheet = null);
}

public class SyncfusionExcelParserService : ISyncfusionExcelParserService, ITimeExcelParserService
{
    private readonly ILogger<SyncfusionExcelParserService> _logger;
    private readonly ExcelFormatConverter? _excelFormatConverter;
    private readonly IDriftBaselineProvider? _driftBaselineProvider;
    private static readonly TimeZoneInfo MalaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"); // GMT+8

    public SyncfusionExcelParserService(
        ILogger<SyncfusionExcelParserService> logger,
        ExcelFormatConverter? excelFormatConverter = null,
        IDriftBaselineProvider? driftBaselineProvider = null)
    {
        _logger = logger;
        _excelFormatConverter = excelFormatConverter;
        _driftBaselineProvider = driftBaselineProvider;
    }

    public async Task<TimeExcelParseResult> ParseAsync(IFormFile file, TemplateProfileContext? profileContext = null, CancellationToken cancellationToken = default)
    {
        var result = new TimeExcelParseResult();

        try
        {
            // Read file into byte array first to ensure clean stream handling
            byte[] fileBytes;
            using (var tempStream = new MemoryStream())
            {
                await file.CopyToAsync(tempStream, cancellationToken);
                fileBytes = tempStream.ToArray();
            }
            
            // Detect extension to give the engine the correct "DefaultVersion" hint
            // This ensures robust support for both legacy (.xls) and modern (.xlsx/.xlsm) formats
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            // INSTRUMENTATION: Log file metadata before parsing
            var fileSignature = fileBytes.Length >= 8 ? BitConverter.ToString(fileBytes, 0, 8).Replace("-", "") : "Unknown";
            byte[] hashBytes;
            using (var sha256 = SHA256.Create())
            {
                hashBytes = sha256.ComputeHash(fileBytes);
            }
            var fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            _logger.LogInformation(
                "ParserInstrumentation: FileName={FileName}, OriginalExtension={Extension}, FileSize={Size} bytes, SHA256={Hash}, FileSignature={Signature}",
                file.FileName, extension, fileBytes.Length, fileHash, fileSignature);
            
            _logger.LogInformation("File loaded: {FileName}, Size: {Size} bytes", file.FileName, fileBytes.Length);

            // DIAGNOSTIC MODE: Perform file validation before attempting to open
            var diagnostics = PerformFileDiagnostics(fileBytes, file.FileName, extension);
            _logger.LogInformation("File diagnostics for {FileName}: {Issues}", 
                file.FileName, string.Join("; ", diagnostics.Issues));
            
            if (!diagnostics.IsValid)
            {
                var diagnosticMessage = string.Join("; ", diagnostics.Issues);
                _logger.LogError("File validation failed for {FileName}: {Diagnostics}", file.FileName, diagnosticMessage);
            }

            // Check file header to determine actual format (more reliable than extension)
            var fileHeader = diagnostics.FileHeader ?? (fileBytes.Length >= 8 ? BitConverter.ToString(fileBytes, 0, 8) : "Unknown");
            _logger.LogInformation("File header (first 8 bytes): {Header}", fileHeader);

            // ✅ UNIFIED .xls PIPELINE: Convert .xls -> .xlsx then always parse with Syncfusion (no ExcelDataReader as final parser)
            bool conversionPerformed = false;
            string? convertedFilePath = null;
            long? convertedFileSizeBytes = null;
            if (extension == ".xls" && _excelFormatConverter != null)
            {
                try
                {
                    _logger.LogInformation("Detected .xls file {FileName} - converting to .xlsx then parsing with Syncfusion only", file.FileName);
                    var xlsxBytes = await _excelFormatConverter.ConvertXlsToXlsxAsync(fileBytes, file.FileName, cancellationToken);
                    fileBytes = xlsxBytes;
                    extension = ".xlsx";
                    conversionPerformed = true;
                    convertedFileSizeBytes = xlsxBytes.Length;
                    _logger.LogInformation("ParserRoutingDecision: ParserChosen=Syncfusion, Reason=XlsConvertedToXlsx, FileName={FileName}", file.FileName);
                }
                catch (Exception convEx)
                {
                    _logger.LogWarning(convEx, " .xls conversion failed for {FileName}, will try Syncfusion direct open: {Error}", file.FileName, convEx.Message);
                    // Keep fileBytes and extension as .xls; Syncfusion path below will use Excel97to2003
                }
            }
            else if (extension == ".xls")
            {
                _logger.LogInformation("ParserRoutingDecision: ParserChosen=Syncfusion, Reason=ExtensionIsXlsNoConverter, FileName={FileName}", file.FileName);
            }
            else
            {
                _logger.LogInformation("ParserRoutingDecision: ParserChosen=Syncfusion, Reason=ExtensionIsXlsxOrXlsm, Extension={Extension}, FileName={FileName}", extension, file.FileName);
            }

            // ✅ STRATEGY 1: For .xlsx/.xlsm files, or .xls files where ExcelDataReader failed, use Syncfusion
            using var excelEngine = new ExcelEngine();
            var application = excelEngine.Excel;
            
            // Check if file is supported (this can catch some format issues early)
            bool isSupported = false;
            try
            {
                using var checkStream = new MemoryStream(fileBytes, writable: false);
                isSupported = application.IsSupported(checkStream);
                _logger.LogInformation("File support check for {FileName}: {IsSupported}", file.FileName, isSupported);
            }
            catch (Exception checkEx)
            {
                _logger.LogWarning(checkEx, "IsSupported check failed for {FileName}, will attempt to open anyway", file.FileName);
                isSupported = true; // Assume supported if check fails, let actual open attempt determine
            }
            
            // Set DefaultVersion BEFORE opening (critical for .xls files)
            if (extension == ".xls")
            {
                application.DefaultVersion = ExcelVersion.Excel97to2003; // Legacy format
                _logger.LogInformation("Set DefaultVersion to Excel97to2003 for .xls file: {FileName}", file.FileName);
            }
            else
            {
                application.DefaultVersion = ExcelVersion.Excel2016; // Modern format (.xlsx, .xlsm)
                _logger.LogInformation("Set DefaultVersion to Excel2016 for {Extension} file: {FileName}", extension, file.FileName);
            }
            
            // ✅ FIX: Handle password-protected files
            // Subscribe to OnPasswordRequired event to try common passwords
            var passwordAttempts = new System.Collections.Generic.List<string>();
            var commonPasswords = new[] { "", "password", "1234", "admin", "TIME", "time" };
            
            application.OnPasswordRequired += (sender, e) =>
            {
                // Try common passwords in sequence
                var nextPasswordIndex = passwordAttempts.Count;
                if (nextPasswordIndex < commonPasswords.Length)
                {
                    var password = commonPasswords[nextPasswordIndex];
                    passwordAttempts.Add(password);
                    e.NewPassword = password;
                    _logger.LogInformation("Password required for {FileName}, attempt {Attempt}/{Total}, trying: {Password}", 
                        file.FileName, nextPasswordIndex + 1, commonPasswords.Length, 
                        string.IsNullOrEmpty(password) ? "(empty)" : "***");
                }
                else
                {
                    // No more passwords to try
                    e.NewPassword = null;
                    _logger.LogWarning("Password required for {FileName}, but all common passwords exhausted", file.FileName);
                }
            };
            
            IWorkbook? workbook = null;
            Exception? lastException = null;
            bool isPasswordProtected = false;
            
            // For .xls files, try the simple direct approach first (as recommended)
            if (extension == ".xls")
            {
                try
                {
                    // Use MemoryStream (equivalent to FileStream for IFormFile)
                    // FileShare.ReadWrite is not applicable for MemoryStream, but we use writable: false for read-only
                    using var stream = new MemoryStream(fileBytes, writable: false);
                    
                    _logger.LogInformation("Attempting to open .xls file {FileName} with DefaultVersion=Excel97to2003 (direct open)", 
                        file.FileName);
                    
                    // ✅ SIMPLE ERROR LOGGING: Direct error capture at the exact failure point
                    try
                    {
                        // Direct open without ExcelOpenType.Automatic (simpler, recommended for .xls)
                        workbook = application.Workbooks.Open(stream);
                        
                        _logger.LogInformation("✅ Successfully opened {FileName} using direct open", file.FileName);
                    }
                    catch (Exception openEx)
                    {
                        // ✅ YOUR SUGGESTION: Simple, direct error logging to capture the real exception
                        _logger.LogError(openEx,
                            "Excel parsing failed for file {FileName}. Inner: {InnerException}",
                            file.FileName,
                            openEx.InnerException?.Message);
                        
                        // Re-throw to be caught by outer catch block for further processing
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    var errorDetails = ExtractExceptionDetails(ex);
                    
                    // ✅ FIX: More accurate password detection - check for actual password-related errors
                    // The generic "Exception of type 'System.Exception' was thrown" is NOT a password error
                    // Check for actual password-related keywords in the stack trace or specific error types
                    var errorText = ex.ToString().ToUpperInvariant();
                    var stackTrace = ex.StackTrace?.ToUpperInvariant() ?? "";
                    var isActuallyPasswordProtected = 
                        (errorText.Contains("PASSWORD REQUIRED") || 
                         errorText.Contains("FILE IS PROTECTED") ||
                         errorText.Contains("PASSWORD PROTECTED") ||
                         stackTrace.Contains("PASSWORDREQUIRED") ||
                         stackTrace.Contains("ONPASSWORDREQUIRED")) &&
                        !errorText.Contains("FAT.GETSTREAM") && // Exclude FAT errors (file structure issues)
                        !errorText.Contains("COMPOUNDFILE") && // Exclude compound file errors
                        !errorText.Contains("ENTRYSTREAM"); // Exclude entry stream errors
                    
                    if (isActuallyPasswordProtected)
                    {
                        isPasswordProtected = true;
                        _logger.LogWarning("Password-protected file detected: {FileName}", file.FileName);
                        
                        // The OnPasswordRequired event handler will be called automatically
                        // Try opening again - the event handler will provide passwords
                        try
                        {
                            using var passwordStream = new MemoryStream(fileBytes, writable: false);
                            _logger.LogInformation("Retrying password-protected file {FileName} with OnPasswordRequired event handler", file.FileName);
                            
                            // The OnPasswordRequired event will be triggered and provide passwords
                            workbook = application.Workbooks.Open(passwordStream, ExcelOpenType.Automatic);
                            _logger.LogInformation("✅ Successfully opened password-protected file {FileName}", file.FileName);
                            lastException = null; // Clear error since we succeeded
                        }
                        catch (Exception passwordEx)
                        {
                            _logger.LogWarning("Password-protected file {FileName} could not be opened with available passwords: {Error}", 
                                file.FileName, passwordEx.Message);
                            try { workbook?.Close(); } catch { }
                            workbook = null;
                        }
                    }
                    else
                    {
                        // This is NOT a password error - it's a file structure/corruption issue
                        _logger.LogError(ex, 
                            "Direct open failed for .xls file {FileName} | Error: {Error} | Type: {ExceptionType} | Stack: {StackTrace}", 
                            file.FileName, errorDetails, ex.GetType().FullName, 
                            ex.StackTrace?.Split('\n').FirstOrDefault() ?? "No stack trace");
                        
                        // Check if this is a FAT/CompoundFile error (file structure issue)
                        if (errorText.Contains("FAT.GETSTREAM") || errorText.Contains("COMPOUNDFILE") || errorText.Contains("ENTRYSTREAM"))
                        {
                            _logger.LogWarning("File structure error detected (FAT/CompoundFile) for {FileName}. " +
                                "This may indicate file corruption or non-standard Excel format that Excel tolerates but Syncfusion cannot read.", 
                                file.FileName);
                        }
                        
                        if (ex.InnerException != null)
                        {
                            _logger.LogError(ex.InnerException, 
                                "Inner exception for {FileName}: {InnerException}", 
                                file.FileName, ex.InnerException.ToString());
                        }
                    }
                    
                    try { workbook?.Close(); } catch { }
                    workbook = null;
                    
                    // Fallback: Try with Automatic type
                    _logger.LogInformation("Trying fallback: Automatic type for {FileName}", file.FileName);
                    try
                    {
                        using var fallbackStream = new MemoryStream(fileBytes, writable: false);
                        workbook = application.Workbooks.Open(fallbackStream, ExcelOpenType.Automatic);
                        _logger.LogInformation("✅ Successfully opened {FileName} using Automatic type", file.FileName);
                        lastException = null; // Clear error since we succeeded
                    }
                    catch (Exception fallbackEx)
                    {
                        lastException = fallbackEx;
                        var fallbackError = ExtractExceptionDetails(fallbackEx);
                        _logger.LogError(fallbackEx, 
                            "Automatic type also failed for {FileName} | Error: {Error}", 
                            file.FileName, fallbackError);
                        try { workbook?.Close(); } catch { }
                        workbook = null;
                    }
                }
            }
            else
            {
                // For .xlsx/.xlsm files, try Automatic first, then direct
                try
                {
                    using var stream = new MemoryStream(fileBytes, writable: false);
                    _logger.LogInformation("Attempting to open {Extension} file {FileName} with DefaultVersion=Excel2016, ExcelOpenType=Automatic", 
                        extension, file.FileName);
                    
                    // ✅ SIMPLE ERROR LOGGING: Direct error capture at the exact failure point
                    try
                    {
                        workbook = application.Workbooks.Open(stream, ExcelOpenType.Automatic);
                        _logger.LogInformation("✅ Successfully opened {FileName} using Automatic type", file.FileName);
                    }
                    catch (Exception openEx)
                    {
                        // ✅ YOUR SUGGESTION: Simple, direct error logging to capture the real exception
                        _logger.LogError(openEx,
                            "Excel parsing failed for file {FileName}. Inner: {InnerException}",
                            file.FileName,
                            openEx.InnerException?.Message);
                        
                        // Re-throw to be caught by outer catch block for further processing
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Automatic open failed, trying direct open: {Error}", ex.Message);
                    try
                    {
                        using var fallbackStream = new MemoryStream(fileBytes, writable: false);
                        workbook = application.Workbooks.Open(fallbackStream);
                        _logger.LogInformation("✅ Successfully opened {FileName} using direct open", file.FileName);
                        lastException = null;
                    }
                    catch (Exception fallbackEx)
                    {
                        lastException = fallbackEx;
                        try { workbook?.Close(); } catch { }
                        workbook = null;
                    }
                }
            }
            
            // Last resort: Try opening directly from IFormFile stream
            if (workbook == null)
            {
                _logger.LogInformation("All strategies failed, trying direct IFormFile stream for {FileName}", file.FileName);
                try
                {
                    using var directStream = file.OpenReadStream();
                    application.DefaultVersion = extension == ".xls" ? ExcelVersion.Excel97to2003 : ExcelVersion.Excel2016;
                    workbook = application.Workbooks.Open(directStream);
                    _logger.LogInformation("✅ Successfully opened {FileName} using direct IFormFile stream", file.FileName);
                }
                catch (Exception directEx)
                {
                    _logger.LogWarning("Direct IFormFile stream also failed: {Error}", directEx.Message);
                    try { workbook?.Close(); } catch { }
                    workbook = null;
                    if (lastException == null)
                    {
                        lastException = directEx;
                    }
                }
            }
            
            // ✅ STRATEGY 2: If Syncfusion fails with FAT/CompoundFile error, try Excel COM Interop repair (if available)
            if (workbook == null && lastException != null)
            {
                var detailedError = ExtractExceptionDetails(lastException);
                var isFileStructureError = detailedError.Contains("FAT.GetStream") || 
                                          detailedError.Contains("CompoundFile") || 
                                          detailedError.Contains("EntryStream");
                
                if (isFileStructureError)
                {
                    _logger.LogInformation("Attempting Excel COM Interop repair for {FileName} (file structure error detected)", file.FileName);
                    var repairedBytes = await TryRepairWithExcelComInteropAsync(fileBytes, file.FileName, extension);
                    
                    if (repairedBytes != null && repairedBytes.Length > 0)
                    {
                        _logger.LogInformation("✅ Excel COM Interop repair succeeded for {FileName}, retrying with Syncfusion", file.FileName);
                        fileBytes = repairedBytes; // Use repaired file
                        
                        // Retry with repaired file
                        try
                        {
                            using var repairedStream = new MemoryStream(fileBytes, writable: false);
                            workbook = application.Workbooks.Open(repairedStream, 
                                extension == ".xls" ? ExcelOpenType.Automatic : ExcelOpenType.Automatic);
                            _logger.LogInformation("✅ Successfully opened repaired {FileName} with Syncfusion", file.FileName);
                            lastException = null; // Clear error since we succeeded
                        }
                        catch (Exception retryEx)
                        {
                            _logger.LogWarning(retryEx, "Repaired file still failed to open with Syncfusion: {Error}", retryEx.Message);
                            // Continue to error handling below
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Excel COM Interop repair not available or failed for {FileName}", file.FileName);
                    }
                }
            }
            
            // ✅ STRATEGY 3: For .xlsx/.xlsm only, if Syncfusion fails with file structure errors, try ExcelDataReader as fallback.
            // For .xls we convert to xlsx and parse with Syncfusion only; this fallback does not apply to .xls.
            if (workbook == null && lastException != null && (extension == ".xlsx" || extension == ".xlsm"))
            {
                var detailedError = ExtractExceptionDetails(lastException);
                var isFileStructureError = detailedError.Contains("FAT.GetStream") || 
                                          detailedError.Contains("CompoundFile") || 
                                          detailedError.Contains("EntryStream");
                
                if (isFileStructureError)
                {
                    _logger.LogInformation("Syncfusion failed with file structure error for {Extension} file {FileName}, trying ExcelDataReader fallback", 
                        extension, file.FileName);
                    var fallbackResult = await TryParseWithExcelDataReaderAsync(fileBytes, file.FileName, extension);
                    
                    if (fallbackResult != null && fallbackResult.OrderData != null)
                    {
                        var fbOrderType = fallbackResult.OrderData.OrderTypeCode ?? "Unknown";
                        var fbMissingRequired = GetMissingRequiredFields(fallbackResult.OrderData, fbOrderType);
                        if (fbMissingRequired.Count > 0)
                        {
                            fallbackResult.Success = false;
                            fallbackResult.ParseStatus = "FailedRequiredFields";
                            fallbackResult.ErrorMessage = "Required fields missing: " + string.Join(", ", fbMissingRequired);
                            fallbackResult.ValidationErrors = fbMissingRequired.Select(f => $"{f} is required").Concat(fallbackResult.ValidationErrors ?? new List<string>()).ToList();
                            var report = BuildParseReport(file.FileName, fileHeader, fileBytes.Length, fileHash, "ExcelDataReader+Syncfusion", false, null, null, null, new Dictionary<string, int>(), null, null, fallbackResult.OrderData, fbOrderType, fbMissingRequired, fallbackResult.ValidationErrors ?? new List<string>());
                            (decimal fc, report.ConfidenceBreakdown) = ComputeConfidenceBreakdown(fallbackResult.OrderData, fbOrderType, fbMissingRequired);
                            report.FinalConfidenceScore = fc;
                            report.ParseStatus = "FailedRequiredFields";
                            fallbackResult.ParseReport = report;
                            fallbackResult.OrderData.ConfidenceScore = 0m;
                            _logger.LogWarning("ExcelDataReader fallback: Structure Gate failed for {FileName}: missing required: {Missing}", file.FileName, string.Join(", ", fbMissingRequired));
                            return fallbackResult;
                        }
                        if (fallbackResult.ValidationErrors?.Count > 0)
                        {
                            fallbackResult.Success = false;
                            fallbackResult.ParseStatus = "FailedValidation";
                            var report = BuildParseReport(file.FileName, fileHeader, fileBytes.Length, fileHash, "ExcelDataReader+Syncfusion", false, null, null, null, new Dictionary<string, int>(), null, null, fallbackResult.OrderData, fbOrderType, new List<string>(), fallbackResult.ValidationErrors);
                            (decimal fc, report.ConfidenceBreakdown) = ComputeConfidenceBreakdown(fallbackResult.OrderData, fbOrderType, new List<string>());
                            report.FinalConfidenceScore = fc;
                            report.ParseStatus = "FailedValidation";
                            fallbackResult.ParseReport = report;
                            _logger.LogWarning("ExcelDataReader fallback parsed {FileName} with validation errors: {Errors}", file.FileName, string.Join(", ", fallbackResult.ValidationErrors));
                            return fallbackResult;
                        }
                        fallbackResult.Success = true;
                        fallbackResult.ParseStatus = "Success";
                        var successReport = BuildParseReport(file.FileName, fileHeader, fileBytes.Length, fileHash, "ExcelDataReader+Syncfusion", false, null, null, null, new Dictionary<string, int>(), null, null, fallbackResult.OrderData, fbOrderType, new List<string>(), new List<string>());
                        (decimal finalConf, successReport.ConfidenceBreakdown) = ComputeConfidenceBreakdown(fallbackResult.OrderData, fbOrderType, new List<string>());
                        successReport.FinalConfidenceScore = finalConf;
                        successReport.ParseStatus = "Success";
                        fallbackResult.ParseReport = successReport;
                        fallbackResult.OrderData.ConfidenceScore = finalConf;
                        _logger.LogInformation("✅ ExcelDataReader fallback succeeded for {FileName}", file.FileName);
                        return fallbackResult;
                    }
                    if (fallbackResult != null)
                        return fallbackResult;
                }
            }
            
            if (workbook == null)
            {
                var errorMessage = "Failed to open Excel file with all strategies";
                if (lastException != null)
                {
                    var detailedError = ExtractExceptionDetails(lastException);
                    
                    // Provide specific message based on error type
                    if (isPasswordProtected)
                    {
                        errorMessage = "File is password-protected and the password could not be determined automatically. " +
                                      "Please remove the password protection from the Excel file and try again, " +
                                      "or contact support if this file should be accessible.";
                    }
                    else if (detailedError.Contains("FAT.GetStream") || detailedError.Contains("CompoundFile") || detailedError.Contains("EntryStream"))
                    {
                        if (extension == ".xls")
                        {
                            errorMessage = "File structure error: The .xls file has a non-standard or corrupted internal structure. " +
                                          "Both ExcelDataReader and Syncfusion failed to parse it. " +
                                          "\n\nSOLUTION:\n" +
                                          "1. Open the file in Microsoft Excel\n" +
                                          "2. Go to File → Save As\n" +
                                          "3. Choose 'Excel 97-2003 Workbook (*.xls)' or 'Excel Workbook (*.xlsx)'\n" +
                                          "4. Save with a new name\n" +
                                          "5. Upload the newly saved file\n\n" +
                                          "Alternatively, use Excel's 'Open and Repair' feature:\n" +
                                          "1. In Excel: File → Open → Browse\n" +
                                          "2. Select the file\n" +
                                          "3. Click the arrow next to 'Open' → 'Open and Repair'\n" +
                                          "4. Click 'Repair' and save the repaired file";
                        }
                        else
                        {
                            errorMessage = "File structure error: The Excel file has a non-standard or corrupted internal structure. " +
                                          "While Excel may be able to open it, both Syncfusion and ExcelDataReader cannot parse it. " +
                                          "\n\nSOLUTION:\n" +
                                          "1. Open the file in Microsoft Excel\n" +
                                          "2. Go to File → Save As\n" +
                                          "3. Choose 'Excel Workbook (*.xlsx)'\n" +
                                          "4. Save with a new name\n" +
                                          "5. Upload the newly saved file\n\n" +
                                          "Alternatively, use Excel's 'Open and Repair' feature:\n" +
                                          "1. In Excel: File → Open → Browse\n" +
                                          "2. Select the file\n" +
                                          "3. Click the arrow next to 'Open' → 'Open and Repair'\n" +
                                          "4. Click 'Repair' and save the repaired file";
                        }
                    }
                    else
                    {
                        errorMessage = $"Failed to open Excel file: {detailedError}";
                    }
                    
                    // Log comprehensive error information
                    _logger.LogError(lastException, 
                        "❌ All strategies exhausted for {FileName} | Final Error: {Error} | Diagnostics: {Diagnostics} | Full Exception: {FullException}", 
                        file.FileName, detailedError, string.Join("; ", diagnostics.Issues), lastException.ToString());
                    
                    // Log inner exception separately
                    if (lastException.InnerException != null)
                    {
                        _logger.LogError(lastException.InnerException, 
                            "❌ Root cause (inner exception) for {FileName}: {InnerException}", 
                            file.FileName, lastException.InnerException.ToString());
                    }
                }
                else
                {
                    if (isPasswordProtected)
                    {
                        errorMessage = "File is password-protected and the password could not be determined automatically.";
                    }
                    _logger.LogError("❌ Failed to open {FileName} - No exception captured. Diagnostics: {Diagnostics}", 
                        file.FileName, string.Join("; ", diagnostics.Issues));
                }
                
                throw new Exception(errorMessage, lastException);
            }

            // Deterministic sheet selection: score each worksheet by known labels in first 30 rows (Phase 8: profile preferred sheets boost)
            IWorksheet worksheet;
            string? selectedSheetName = null;
            var sheetScores = new Dictionary<string, int>();
            int? detectedHeaderRow = null;
            int? headerScore = null;
            var headerRange = profileContext?.HeaderRowRange ?? (1, SheetHeaderDetector.MaxRowsToScan);
            if (workbook.Worksheets.Count > 0)
            {
                int bestScore = -1;
                int bestIndex = 0;
                for (int i = 0; i < workbook.Worksheets.Count; i++)
                {
                    var ws = workbook.Worksheets[i];
                    var adapter = new SyncfusionSheetAdapter(ws);
                    int score = SheetHeaderDetector.ScoreSheet(adapter);
                    var name = ws.Name ?? $"Sheet{i + 1}";
                    if (profileContext?.PreferredSheetNames != null && profileContext.PreferredSheetNames.Count > 0
                        && profileContext.PreferredSheetNames.Contains(name, StringComparer.OrdinalIgnoreCase) && score >= 1)
                        score += 2;
                    sheetScores[name] = score;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                        selectedSheetName = name;
                    }
                }
                worksheet = workbook.Worksheets[bestIndex];
                var selectedAdapter = new SyncfusionSheetAdapter(worksheet);
                var (headerRow1Based, hScore) = SheetHeaderDetector.DetectHeaderRow(selectedAdapter, headerRange.Min, headerRange.Max);
                detectedHeaderRow = headerRow1Based;
                headerScore = hScore;
            }
            else
            {
                if (workbook.Worksheets.Count == 0)
                    throw new InvalidOperationException("Workbook has no worksheets.");
                worksheet = workbook.Worksheets[0];
                selectedSheetName = worksheet.Name ?? "Sheet1";
                var adapterForFirst = new SyncfusionSheetAdapter(worksheet);
                var (hr, hs) = SheetHeaderDetector.DetectHeaderRow(adapterForFirst, headerRange.Min, headerRange.Max);
                detectedHeaderRow = hr;
                headerScore = hs;
            }

            var sheetAdapter = new SyncfusionSheetAdapter(worksheet);
            _logger.LogInformation("Parsing Excel file with Syncfusion: {FileName}, Sheet: {Sheet}, Rows: {Rows}, Cols: {Cols}, HeaderRow: {HeaderRow}",
                file.FileName, selectedSheetName ?? "Sheet1", worksheet.UsedRange.LastRow, worksheet.UsedRange.LastColumn, detectedHeaderRow ?? 1);

            // Detect order type
            var orderType = DetectOrderType(file.FileName, worksheet);

            // Parse based on order type
            ParsedOrderData orderData;
            try
            {
                orderData = orderType switch
                {
                    "MODIFICATION_OUTDOOR" => ParseModificationOutdoor(worksheet, file.FileName),
                    "MODIFICATION_INDOOR" => ParseModificationIndoor(worksheet, file.FileName),
                    "ACTIVATION" => DetectAndParseActivation(worksheet, file.FileName),
                    _ => ParseGenericTimeOrder(worksheet, file.FileName, orderType)
                };
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "Error during parsing phase for {FileName}: {Error}", file.FileName, ExtractExceptionDetails(parseEx));
                throw new Exception($"Error parsing Excel content: {ExtractExceptionDetails(parseEx)}", parseEx);
            }

            // Structure Gate: required fields check (do not treat parse as usable if required missing)
            var missingRequired = GetMissingRequiredFields(orderData, orderType);
            bool structureGatePassed = missingRequired.Count == 0;

            // Validate (additional validation errors beyond required fields)
            var validationErrors = ValidateOrderData(orderData, orderType);

            // Phase 7/8: Field diagnostics (label/match/location only; no PII). Phase 8: profile synonym overrides and header range.
            var fieldDiagnostics = FieldDiagnosticsBuilder.Build(sheetAdapter, selectedSheetName, sheetScores, detectedHeaderRow, headerScore,
                profileContext?.RequiredFieldSynonymOverrides, profileContext?.HeaderRowRange);
            var (sheetScoreBest, sheetScoreSecondBest) = FieldDiagnosticsBuilder.GetBestSheetScores(sheetScores);
            var totalLabelHits = FieldDiagnosticsBuilder.CountRequiredFieldsFound(fieldDiagnostics);
            var requiredLabelsFoundElsewhere = FieldDiagnosticsBuilder.RequiredLabelsFoundElsewhere(missingRequired, fieldDiagnostics);
            var parseStatusForCategory = !structureGatePassed ? "FailedRequiredFields" : (validationErrors.Count > 0 ? "FailedValidation" : "Success");
            var parseFailureCategory = ParseFailureCategorizer.Categorize(parseStatusForCategory, conversionFailed: false, requiredLabelsFoundElsewhere);

            // Build ParseReport for explainability
            var parseReport = BuildParseReport(
                attachmentFileName: file.FileName,
                detectedFileSignature: fileHeader,
                fileSizeBytes: fileBytes.Length,
                fileHash: fileHash,
                engineUsed: "Syncfusion",
                conversionPerformed: conversionPerformed,
                convertedFilePath: convertedFilePath,
                convertedFileSizeBytes: convertedFileSizeBytes,
                selectedSheetName: selectedSheetName,
                sheetScores: sheetScores,
                detectedHeaderRow: detectedHeaderRow,
                headerScore: headerScore,
                orderData: orderData,
                orderType: orderType,
                missingRequiredFields: missingRequired,
                validationErrors: validationErrors,
                fieldDiagnostics: fieldDiagnostics,
                parseFailureCategory: parseFailureCategory,
                sheetScoreBest: sheetScoreBest,
                sheetScoreSecondBest: sheetScoreSecondBest,
                totalLabelHitsForRequiredFields: totalLabelHits);

            // Confidence: if missing required => 0 and hard fail; else 50% required, 20% order type, 20% validity, 10% enrichment
            (decimal finalConfidence, parseReport.ConfidenceBreakdown) = ComputeConfidenceBreakdown(orderData, orderType, missingRequired);
            parseReport.FinalConfidenceScore = finalConfidence;

            // Phase 8: template profile and drift (PII-safe)
            if (profileContext != null)
            {
                parseReport.TemplateProfileId = profileContext.ProfileId;
                parseReport.TemplateProfileName = profileContext.ProfileName;
                if (parseFailureCategory == ParseFailureCategory.LayoutDrift && _driftBaselineProvider != null)
                {
                    var baseline = await _driftBaselineProvider.GetBaselineAsync(profileContext.ProfileId, cancellationToken);
                    var driftResult = DriftDetector.Detect(parseReport, baseline, profileContext.DriftThresholds);
                    parseReport.DriftDetected = driftResult.DriftDetected;
                    parseReport.DriftSignature = driftResult.DriftSignature;
                }
            }

            if (!structureGatePassed)
            {
                result.Success = false;
                result.ParseStatus = "FailedRequiredFields";
                result.ErrorMessage = "Required fields missing: " + string.Join(", ", missingRequired);
                result.ValidationErrors = missingRequired.Select(f => $"{f} is required").Concat(validationErrors).ToList();
                result.OrderData = orderData;
                parseReport.ParseStatus = "FailedRequiredFields";
                result.ParseReport = parseReport;
                result.ParseFailureCategory = parseReport.ParseFailureCategory;
                _logger.LogWarning("Structure Gate failed for {FileName}: missing required fields: {Missing}. Confidence=0.", file.FileName, string.Join(", ", missingRequired));
            }
            else if (validationErrors.Count > 0)
            {
                result.Success = false;
                result.ParseStatus = "FailedValidation";
                result.ValidationErrors = validationErrors;
                result.OrderData = orderData;
                parseReport.ParseStatus = "FailedValidation";
                result.ParseReport = parseReport;
                result.ParseFailureCategory = parseReport.ParseFailureCategory;
                _logger.LogWarning("Parse completed with validation errors for {FileName}: {Errors}", file.FileName, string.Join(", ", validationErrors));
            }
            else
            {
                result.Success = true;
                result.ParseStatus = "Success";
                result.ValidationErrors = validationErrors;
                result.OrderData = orderData;
                parseReport.ParseStatus = "Success";
                result.ParseReport = parseReport;
                result.ParseFailureCategory = parseReport.ParseFailureCategory;
                result.OrderData!.ConfidenceScore = finalConfidence;
                _logger.LogInformation("✅ Excel parsed successfully: {FileName}, Type: {OrderType}, ServiceId: {ServiceId}, Confidence: {Confidence}",
                    file.FileName, orderType, orderData.ServiceId, finalConfidence);
            }

            LogParseResultInstrumentation(result, file.FileName);
            workbook.Close();
        }
        catch (Exception ex)
        {
            // Extract detailed error message using our helper
            var errorMessage = ExtractExceptionDetails(ex);
            
            // If still empty, provide a fallback
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                errorMessage = $"Unable to read Excel file ({ex.GetType().Name})";
            }
            
            _logger.LogError(ex, "Error parsing Excel file: {FileName} | Error: {ErrorMessage} | Type: {ExceptionType}", 
                file.FileName, errorMessage, ex.GetType().Name);
            
            // Log full exception details for debugging
            _logger.LogError("Full exception: {Exception}", ex.ToString());
            
            // Create a minimal draft with just filename and detected order type
            // This allows users to manually enter data even if file is corrupted
            var fileName = file.FileName;
            var orderType = DetectOrderType(fileName, (IWorksheet?)null); // Filename-only detection
            
            result.OrderData = new ParsedOrderData
            {
                SourceFileName = fileName,
                OrderTypeCode = orderType,
                OrderTypeHint = orderType.Replace("_", " "),
                PartnerCode = "TIME"
            };
            
            var errorMsg = $"⚠️ Excel Error: {errorMessage}";
            if (errorMessage.Contains("password", StringComparison.OrdinalIgnoreCase) || 
                errorMessage.Contains("password-protected", StringComparison.OrdinalIgnoreCase))
            {
                errorMsg = "⚠️ File is Password Protected. The file requires a password to open. " +
                          "Please remove the password protection from the Excel file and try again, " +
                          "or contact support if you need assistance with password-protected files.";
            }
            else if (errorMessage.Contains("File structure error", StringComparison.OrdinalIgnoreCase) ||
                     errorMessage.Contains("FAT.GetStream", StringComparison.OrdinalIgnoreCase) ||
                     errorMessage.Contains("CompoundFile", StringComparison.OrdinalIgnoreCase))
            {
                errorMsg = "⚠️ File Structure Error: The Excel file has a non-standard or corrupted internal structure. " +
                          "While Excel can open it, our parser cannot read it. " +
                          "Please try: (1) Open the file in Excel, (2) Save As a new .xls file, or (3) Convert to .xlsx format, then upload again.";
            }
            else if (errorMessage.Contains("corrupt", StringComparison.OrdinalIgnoreCase) || 
                     errorMessage.Contains("format", StringComparison.OrdinalIgnoreCase))
            {
                errorMsg = $"⚠️ File Format Error: {errorMessage}";
            }
            
            result.ValidationErrors = new List<string>
            {
                errorMsg,
                "File may be corrupted, in an unsupported format, or password protected.",
                "Please manually enter the order details using the Edit button"
            };
            
            result.ErrorMessage = errorMsg;
            result.Success = false;
            result.ParseStatus = "ParseError";
            var conversionFailed = errorMessage.Contains("FAT.GetStream", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("CompoundFile", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("File structure error", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("corrupt", StringComparison.OrdinalIgnoreCase);
            result.ParseFailureCategory = ParseFailureCategorizer.Categorize("ParseError", conversionFailed, false);
            result.ParseReport = new ParseReport
            {
                AttachmentFileName = fileName,
                ParseStatus = "ParseError",
                ParseFailureCategory = result.ParseFailureCategory
            };

            _logger.LogWarning("Created empty draft for failed file: {FileName}, Error: {Error}", 
                fileName, errorMessage);
        }

        LogParseResultInstrumentation(result, file.FileName);
        return result;
    }

    /// <summary>
    /// Log instrumentation data for parse results
    /// </summary>
    private void LogParseResultInstrumentation(TimeExcelParseResult result, string fileName)
    {
        var missingFields = new List<string>();
        if (result.OrderData != null)
        {
            if (string.IsNullOrWhiteSpace(result.OrderData.ServiceId))
                missingFields.Add("ServiceId");
            if (!result.OrderData.AppointmentDateTime.HasValue)
                missingFields.Add("AppointmentDate");
            if (string.IsNullOrWhiteSpace(result.OrderData.CustomerName))
                missingFields.Add("CustomerName");
            if (string.IsNullOrWhiteSpace(result.OrderData.ServiceAddress))
                missingFields.Add("ServiceAddress");
        }

        var validCount = result.Success && result.OrderData != null ? 1 : 0;
        var invalidCount = !result.Success || result.ValidationErrors?.Count > 0 ? 1 : 0;
        var rowCount = result.OrderData != null ? 1 : 0; // Single row per file for TIME Excel format

        _logger.LogInformation(
            "ParseResultInstrumentation: FileName={FileName}, RowCount={RowCount}, ValidCount={ValidCount}, InvalidCount={InvalidCount}, MissingCriticalFields=[{MissingFields}], ValidationErrorsCount={ErrorCount}",
            fileName, rowCount, validCount, invalidCount, string.Join(", ", missingFields), result.ValidationErrors?.Count ?? 0);
    }

    public string DetectOrderType(string fileName, IWorksheet? worksheet = null)
    {
        var fileNameUpper = fileName.ToUpperInvariant();

        // ✅ PRIORITY 1: Check for TBBN Service ID FIRST - if Service ID starts with TBBN, it's definitely a TIME order
        if (worksheet != null)
        {
            var serviceId = ExtractRightSideValueByLabels(worksheet, "Service ID", "SERVICE ID", "TBBN", "ServiceID");
            if (string.IsNullOrEmpty(serviceId))
            {
                // Fallback: Search for TBBN pattern anywhere in the sheet
                serviceId = FindPatternInSheet(worksheet, @"TBBN[A-Z]?\d{5,}[A-Z]?");
            }
            
            if (!string.IsNullOrEmpty(serviceId) && serviceId.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
            {
                // This is a TIME order (FTTH/FTTO), not Celcom/Digi
                // Check if it's a modification (would have Old Address) or activation
                var oldAddress = ExtractRightSideValueByLabels(worksheet, "Old Address", "OLD ADDRESS", "Previous Address");
                if (!string.IsNullOrEmpty(oldAddress))
                {
                    return "MODIFICATION_OUTDOOR";
                }
                // Check for indoor modification indicators
                var taskType = GetCellValue(worksheet, 4, 2); // Row 4, Column B
                if (taskType?.Contains("indoor", StringComparison.OrdinalIgnoreCase) == true ||
                    taskType?.Contains("relocate within", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return "MODIFICATION_INDOOR";
                }
                return "ACTIVATION"; // Will be handled as TIME FTTH/FTTO activation
            }
        }

        // ✅ PRIORITY 2: Check for Celcom indicators (only if no TBBN Service ID found)
        // Use specific check for PARTNER SERVICE ID, not just any "Celcom" text
        if (worksheet != null)
        {
            var partnerServiceId = ExtractRightSideValueByLabels(worksheet, 
                "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID");
            if (!string.IsNullOrEmpty(partnerServiceId) && 
                partnerServiceId.StartsWith("CELCOM", StringComparison.OrdinalIgnoreCase))
            {
                // Check if it's a modification (would have Old Address) or activation
                var oldAddress = ExtractRightSideValueByLabels(worksheet, "Old Address", "OLD ADDRESS", "Previous Address");
                if (!string.IsNullOrEmpty(oldAddress))
                {
                    return "MODIFICATION_OUTDOOR"; // Can handle Celcom modifications later
                }
                return "ACTIVATION"; // Will be handled as Celcom activation in parsing
            }
        }
        
        // ✅ Also check filename for Celcom (as fallback - only if worksheet is null or TBBN not found)
        // This is a last resort when we can't read the file content
        // Note: If worksheet is available, TBBN check above should have caught TIME orders
        if (fileNameUpper.Contains("CELCOM"))
        {
            // Double-check: If worksheet is available, verify it's not a TIME order with TBBN
            if (worksheet != null)
            {
                // Final safety check: Look for TBBN in the worksheet before assuming Celcom
                var finalServiceIdCheck = ExtractRightSideValueByLabels(worksheet, "Service ID", "SERVICE ID", "TBBN", "ServiceID");
                if (string.IsNullOrEmpty(finalServiceIdCheck))
                {
                    finalServiceIdCheck = FindPatternInSheet(worksheet, @"TBBN[A-Z]?\d{5,}[A-Z]?");
                }
                
                // If TBBN found, it's a TIME order, not Celcom
                if (!string.IsNullOrEmpty(finalServiceIdCheck) && finalServiceIdCheck.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("⚠️ Filename contains 'CELCOM' but Service ID is TBBN ({ServiceId}) - correctly identified as TIME order", finalServiceIdCheck);
                    var oldAddressInner = ExtractRightSideValueByLabels(worksheet, "Old Address", "OLD ADDRESS", "Previous Address");
                    if (!string.IsNullOrEmpty(oldAddressInner))
                    {
                        return "MODIFICATION_OUTDOOR";
                    }
                    return "ACTIVATION"; // TIME activation, not Celcom
                }
                
                // No TBBN found, check for modification
                var oldAddress = ExtractRightSideValueByLabels(worksheet, "Old Address", "OLD ADDRESS", "Previous Address");
                if (!string.IsNullOrEmpty(oldAddress))
                {
                    return "MODIFICATION_OUTDOOR";
                }
            }
            // Only return Celcom if worksheet is null (can't verify) or confirmed no TBBN
            return "ACTIVATION";
        }

        // ✅ Check for Digi indicators (before standard checks)
        if (fileNameUpper.Contains("DIGI") || 
            (worksheet != null && ContainsDigiIndicator(worksheet)))
        {
            // Check if it's a modification (would have Old Address) or activation
            if (worksheet != null)
            {
                var oldAddress = ExtractRightSideValueByLabels(worksheet, "Old Address", "OLD ADDRESS", "Previous Address");
                if (!string.IsNullOrEmpty(oldAddress))
                {
                    return "MODIFICATION_OUTDOOR"; // Can handle Digi modifications later
                }
            }
            return "ACTIVATION"; // Will be handled as Digi activation in parsing
        }

        // Filename-based detection
        if (fileNameUpper.StartsWith("M"))
        {
            if (worksheet != null)
            {
                var taskType = GetCellValue(worksheet, 4, 2); // Row 4, Column B
                if (taskType?.Contains("outdoor", StringComparison.OrdinalIgnoreCase) == true ||
                    taskType?.Contains("relocation", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return "MODIFICATION_OUTDOOR";
                }
                if (taskType?.Contains("indoor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return "MODIFICATION_INDOOR";
                }
            }
            return "MODIFICATION_OUTDOOR";
        }

        if (fileNameUpper.StartsWith("A"))
        {
            return "ACTIVATION";
        }

        if (fileNameUpper.StartsWith("C"))
        {
            return "CANCELLATION";
        }

        // Check worksheet for checkboxes in TASK section
        if (worksheet != null)
        {
            // Check MODIFICATION checkbox (typically row 5, column E-F)
            var modificationCell = GetCellValue(worksheet, 5, 5);
            if (modificationCell?.Contains("X", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "MODIFICATION_OUTDOOR";
            }

            // Check ACTIVATION checkbox (typically row 5, column C-D)
            var activationCell = GetCellValue(worksheet, 5, 3);
            if (activationCell?.Contains("X", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "ACTIVATION";
            }
        }

        return "UNKNOWN";
    }

    /// <summary>
    /// Detect installation type (FTTH or FTTO) for TIME activation orders
    /// Checks worksheet content for FTTH/FTTO indicators
    /// TIME partner orders default to FTTH if cannot determine
    /// </summary>
    private string DetectInstallationType(IWorksheet worksheet, string fileName)
    {
        // Default to FTTH for TIME partner orders if cannot determine
        var defaultType = "FTTH";
        
        try
        {
            // Strategy 1: Check filename for FTTH/FTTO
            var fileNameUpper = fileName.ToUpperInvariant();
            if (fileNameUpper.Contains("FTTO"))
            {
                _logger.LogInformation("✓ FTTO detected from filename: {FileName}", fileName);
                return "FTTO";
            }
            if (fileNameUpper.Contains("FTTH"))
            {
                _logger.LogInformation("✓ FTTH detected from filename: {FileName}", fileName);
                return "FTTH";
            }

            // Strategy 2: Search for "FTTH" or "FTTO" text in the worksheet
            int lastRow = worksheet.UsedRange.LastRow;
            int lastCol = worksheet.UsedRange.LastColumn;
            
            for (int row = 1; row <= Math.Min(30, lastRow); row++) // Check first 30 rows
            {
                for (int col = 1; col <= Math.Min(20, lastCol); col++) // Check first 20 columns
                {
                    var cellValue = GetCellValue(worksheet, row, col)?.ToString() ?? "";
                    var cellUpper = cellValue.ToUpperInvariant();
                    
                    // Check for FTTO indicators (more specific, check first)
                    if (cellUpper.Contains("FTTO") || cellUpper.Contains("FIBER TO THE OFFICE"))
                    {
                        _logger.LogInformation("✓ FTTO detected at R{Row}C{Col}: '{Value}'", row, col, cellValue);
                        return "FTTO";
                    }
                    
                    // Check for FTTH indicators
                    if (cellUpper.Contains("FTTH") || cellUpper.Contains("FIBER TO THE HOME"))
                    {
                        _logger.LogInformation("✓ FTTH detected at R{Row}C{Col}: '{Value}'", row, col, cellValue);
                        return "FTTH";
                    }
                }
            }

            // Strategy 3: Check TASK section (around row 4-6) for task type
            var taskType = GetCellValue(worksheet, 4, 2); // Row 4, Column B
            if (!string.IsNullOrEmpty(taskType))
            {
                var taskUpper = taskType.ToUpperInvariant();
                if (taskUpper.Contains("FTTO") || taskUpper.Contains("OFFICE"))
                {
                    _logger.LogInformation("✓ FTTO detected from TASK section: '{TaskType}'", taskType);
                    return "FTTO";
                }
                if (taskUpper.Contains("FTTH") || taskUpper.Contains("HOME"))
                {
                    _logger.LogInformation("✓ FTTH detected from TASK section: '{TaskType}'", taskType);
                    return "FTTH";
                }
            }

            // Strategy 4: Check package/bandwidth field - FTTO might have "Business" or "Office" keywords
            var package = ExtractRightSideValueByLabels(worksheet, "Package Name", "PACKAGE", "Package");
            if (!string.IsNullOrEmpty(package))
            {
                var packageUpper = package.ToUpperInvariant();
                if (packageUpper.Contains("BUSINESS") || packageUpper.Contains("OFFICE") || packageUpper.Contains("ENTERPRISE"))
                {
                    _logger.LogInformation("✓ FTTO inferred from package name: '{Package}'", package);
                    return "FTTO";
                }
            }

            _logger.LogInformation("⚠ Could not determine installation type for {FileName}, defaulting to {DefaultType}", fileName, defaultType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting installation type for {FileName}, defaulting to {DefaultType}", fileName, defaultType);
        }
        
        return defaultType;
    }

    /// <summary>
    /// DetectOrderType overload for ITimeExcelParserService compatibility (accepts DataTable)
    /// Note: Explicit interface implementation to avoid ambiguity
    /// </summary>
    string ITimeExcelParserService.DetectOrderType(string fileName, DataTable? worksheet)
    {
        // Just use filename-based detection when DataTable is passed
        // (DataTable version doesn't need worksheet inspection)
        return DetectOrderType(fileName, (IWorksheet?)null);
    }

    private ParsedOrderData ParseActivation(IWorksheet worksheet, string fileName)
    {
        _logger.LogInformation("📋 Parsing ACTIVATION form: {FileName}", fileName);

        // ✅ Detect FTTH vs FTTO from worksheet content
        var installationType = DetectInstallationType(worksheet, fileName);
        
        // Set partner code based on installation type: TIME-FTTH or TIME-FTTO
        var partnerCode = installationType == "FTTO" ? "TIME-FTTO" : "TIME-FTTH";
        
        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "ACTIVATION",
            OrderTypeHint = installationType, // "FTTH" or "FTTO"
            PartnerCode = partnerCode // "TIME-FTTH" or "TIME-FTTO"
        };

        // ✅ Extract all fields using generic, future-proof label-based extraction
        // Works for ANY label, regardless of column position - perfect for all partner templates
        data.CustomerName = ExtractRightSideValueByLabels(worksheet, "Customer Name", "CUSTOMER NAME", "Name")
                         ?? ExtractByLabel(worksheet, "CustomerName"); // Fallback to old method
        data.ContactPerson = ExtractRightSideValueByLabels(worksheet, "Contact Person", "CONTACT PERSON", "Contact")
                          ?? ExtractByLabel(worksheet, "ContactPerson"); // Fallback to old method
        var customerPhone = ExtractRightSideValueByLabels(worksheet, "Contact No", "CONTACT NO", "Phone", "Contact", "Mobile")
                         ?? ExtractByLabel(worksheet, "CustomerPhone"); // Fallback to old method
        data.CustomerPhone = NormalizePhone(customerPhone);
        data.CustomerEmail = ExtractRightSideValueByLabels(worksheet, "Email", "EMAIL", "E-mail", "Email Address")
                          ?? ExtractByLabel(worksheet, "CustomerEmail"); // Fallback to old method
        data.ServiceAddress = ExtractRightSideValueByLabels(worksheet, "Service Address", "SERVICE ADDRESS", "Installation Address", "New Address")
                           ?? ExtractByLabel(worksheet, "ServiceAddress"); // Fallback to old method
        data.ServiceId = ExtractRightSideValueByLabels(worksheet, "Service ID", "SERVICE ID", "TBBN", "ServiceID")
                      ?? ExtractByLabel(worksheet, "ServiceId"); // Fallback to old method
        
        // ✅ Extract AWO Number for Assurance orders (if present in Excel)
        data.AwoNumber = ExtractRightSideValueByLabels(worksheet, 
            "AWO NUMBER", "AWO NUMBER:", "AWO NO", "AWO NO.", 
            "AWO", "AWO:", "AWO Number", "AWO Number:")
            ?? ExtractByLabel(worksheet, "AwoNumber"); // Fallback to old method
        
        data.PackageName = ExtractRightSideValueByLabels(worksheet, "Package Name", "PACKAGE", "Package", "Plan Name")
                       ?? ExtractByLabel(worksheet, "PackageName"); // Fallback to old method
        data.Bandwidth = ExtractRightSideValueByLabels(worksheet, "Bandwidth", "BANDWIDTH", "Speed", "Internet Speed")
                      ?? ExtractByLabel(worksheet, "Bandwidth"); // Fallback to old method
        data.OnuSerialNumber = ExtractRightSideValueByLabels(worksheet, "Serial Number", "SERIAL NUMBER", "ONU Serial", "ONU Serial Number")
                            ?? ExtractByLabel(worksheet, "OnuSerialNumber"); // Fallback to old method
        data.OnuPassword = ExtractByLabel(worksheet, "OnuPassword");
        data.Username = ExtractByLabel(worksheet, "Username");
        data.Password = ExtractByLabel(worksheet, "Password");
        data.SplitterLocation = ExtractByLabel(worksheet, "SplitterLocation");

        // 🔧 FIX: Enhanced VOIP Service ID extraction
        data.VoipServiceId = ExtractVoipServiceId(worksheet);
        data.VoipPassword = ExtractByLabel(worksheet, "VoipPassword");

        // 🔧 FIX: Parse appointment date and start time with GMT+8 timezone awareness
        // ✅ Use ExtractRightSideValueByLabels for better label matching (like other fields)
        var dateStr = ExtractRightSideValueByLabels(worksheet, 
            "Appointment Date", "APPOINTMENT DATE", "APPOINT. DATE & TIME", "APPOINT DATE & TIME", 
            "Appointment Date & Time", "Date & Time", "DATE & TIME", "Appointment", "Date")
            ?? ExtractByLabel(worksheet, "AppointmentDate"); // Fallback to old method
        
        // ✅ Try to extract separate time field if date doesn't include time
        // This is just the start time, not a window
        var timeStr = ExtractRightSideValueByLabels(worksheet,
            "Appointment Time", "APPOINTMENT TIME", "Time", "TIME", 
            "Start Time", "START TIME", "Appointment Start Time");
        
        // Combine date and time if both are found separately
        if (!string.IsNullOrEmpty(dateStr) && !string.IsNullOrEmpty(timeStr))
        {
            // Check if date already contains time
            var hasTimeInDate = dateStr.Contains(":") || dateStr.Contains("AM") || dateStr.Contains("PM");
            
            if (!hasTimeInDate)
            {
                // Date doesn't have time, combine with time field
                dateStr = $"{dateStr} {timeStr}";
                _logger.LogInformation("✅ Combined separate date and time fields: Date='{Date}', Time='{Time}' → '{Combined}'", 
                    ExtractRightSideValueByLabels(worksheet, "Appointment Date", "APPOINTMENT DATE", "Date") ?? "(null)",
                    timeStr, dateStr);
            }
        }
        else if (!string.IsNullOrEmpty(timeStr) && string.IsNullOrEmpty(dateStr))
        {
            // Only time found, try to find date separately
            var dateOnly = ExtractRightSideValueByLabels(worksheet, "Date", "DATE", "Appointment Date", "APPOINTMENT DATE");
            if (!string.IsNullOrEmpty(dateOnly))
            {
                dateStr = $"{dateOnly} {timeStr}";
                _logger.LogInformation("✅ Combined separate date and time fields: Date='{Date}', Time='{Time}' → '{Combined}'", 
                    dateOnly, timeStr, dateStr);
            }
        }
        
        data.AppointmentDateTime = ParseAppointmentDateTimeWithTimezone(dateStr);
        
        // Log if appointment date was found
        if (string.IsNullOrEmpty(dateStr))
        {
            _logger.LogWarning("⚠️ Appointment date/time not found for {FileName}. Searched labels: Appointment Date, APPOINTMENT DATE, APPOINT. DATE & TIME, etc.", fileName);
        }
        else
        {
            _logger.LogInformation("✅ Appointment date/time extracted: '{DateStr}' → {ParsedDate}", 
                dateStr, 
                data.AppointmentDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)");
        }

        // 🔧 FIX: Extract materials (DECT phones, ONU devices, etc.)
        var materials = ParseMaterials(worksheet);
        data.Materials = materials;
        if (materials.Any())
        {
            var summary = string.Join("; ", materials.Select(m => 
                $"{m.Name}{(m.Quantity.HasValue ? $" x{m.Quantity.Value}" : "")}{(!string.IsNullOrEmpty(m.Notes) ? $" ({m.Notes})" : "")}"));
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"Materials: {summary}"
                : $"{data.Remarks}\n\nMaterials: {summary}";
        }

        // Fallback: Search for TBBN pattern anywhere
        if (string.IsNullOrEmpty(data.ServiceId))
        {
            data.ServiceId = FindPatternInSheet(worksheet, @"TBBN[A-Z]?\d{5,}[A-Z]?");
        }

        // Calculate confidence
        data.ConfidenceScore = CalculateConfidenceScore(data, "ACTIVATION");

        _logger.LogInformation("✅ {InstallationType} Activation parsed: ServiceId={ServiceId}, Customer={Customer}, Date={Date}, Materials={MaterialCount}, Confidence={Confidence}",
            data.OrderTypeHint, data.ServiceId, data.CustomerName, data.AppointmentDateTime, materials.Count, data.ConfidenceScore);

        return data;
    }

    private ParsedOrderData ParseModificationOutdoor(IWorksheet worksheet, string fileName)
    {
        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "MODIFICATION_OUTDOOR",
            OrderTypeHint = "Modification Outdoor",
            PartnerCode = "TIME"
        };

        // ✅ Extract Service ID first (for database lookup)
        // Phase 1: Normalized header detection (flexible)
        // Phase 2: Exact value extraction (strict - no normalization)
        data.ServiceId = ExtractServiceIdWithNormalizedHeader(worksheet);
        
        // ✅ Extract all fields using generic, future-proof label-based extraction
        // Works for ANY label, regardless of column position - perfect for all partner templates
        data.CustomerName = ExtractRightSideValueByLabels(worksheet, "Customer Name", "CUSTOMER NAME", "Name")
                         ?? ExtractByLabel(worksheet, "CustomerName"); // Fallback
        data.ContactPerson = ExtractRightSideValueByLabels(worksheet, "Contact Person", "CONTACT PERSON", "Contact")
                          ?? ExtractByLabel(worksheet, "ContactPerson"); // Fallback
        var customerPhone = ExtractRightSideValueByLabels(worksheet, "Contact No", "CONTACT NO", "Phone", "Contact", "Mobile")
                         ?? ExtractByLabel(worksheet, "CustomerPhone"); // Fallback
        data.CustomerPhone = NormalizePhone(customerPhone);
        data.CustomerEmail = ExtractRightSideValueByLabels(worksheet, "Email", "EMAIL", "E-mail", "Email Address")
                          ?? ExtractByLabel(worksheet, "CustomerEmail"); // Fallback
        
        // ✅ New Address (Service Address) - where customer is moving to
        data.ServiceAddress = ExtractRightSideValueByLabels(worksheet, "Service Address", "SERVICE ADDRESS", "New Address", "NEW ADDRESS", "Installation Address")
                           ?? ExtractByLabel(worksheet, "ServiceAddress"); // Fallback
        
        // ✅ Old Address - where customer is moving from
        data.OldAddress = ExtractRightSideValueByLabels(worksheet, "Old Address", "OLD ADDRESS", "Previous Address", "PREVIOUS ADDRESS", "Current Address")
                      ?? ExtractByLabel(worksheet, "OldAddress"); // Fallback
        if (string.IsNullOrEmpty(data.OldAddress))
        {
            // Fallback: search for address-related patterns near "OLD" or "PREVIOUS"
            data.OldAddress = ExtractOldAddressFallback(worksheet);
        }
        
        // ✅ New ONU Password - provided by TIME for new location (critical for modification outdoor)
        data.OnuPassword = ExtractRightSideValueByLabels(worksheet, "ONU Password", "ONU PASSWORD", "Password ONU")
                        ?? ExtractByLabel(worksheet, "OnuPassword"); // Fallback
        
        // Other network credentials
        // ✅ Username/Login ID - try USERNAME first (more common), then LOGIN ID, skip if value looks like a label (ends with ":")
        var usernameValue = ExtractRightSideValueByLabels(worksheet, "Username", "USERNAME", "User Name", "Login ID", "LOGIN ID")
                         ?? ExtractByLabel(worksheet, "Username"); // Fallback
        if (!string.IsNullOrWhiteSpace(usernameValue) && !usernameValue.TrimEnd().EndsWith(":"))
        {
            data.Username = usernameValue;
        }
        data.Password = ExtractRightSideValueByLabels(worksheet, "Password", "PASSWORD", "PWD")
                     ?? ExtractByLabel(worksheet, "Password"); // Fallback
        data.PackageName = ExtractRightSideValueByLabels(worksheet, "Package Name", "PACKAGE", "Package", "Plan Name")
                        ?? ExtractByLabel(worksheet, "PackageName"); // Fallback
        data.Bandwidth = ExtractRightSideValueByLabels(worksheet, "Bandwidth", "BANDWIDTH", "Speed", "Internet Speed")
                      ?? ExtractByLabel(worksheet, "Bandwidth"); // Fallback
        data.VoipServiceId = ExtractVoipServiceId(worksheet);
        data.SplitterLocation = ExtractRightSideValueByLabels(worksheet, "Splitter Location", "SPLITTER LOCATION", "Splitter", "SPLITTER")
                             ?? ExtractByLabel(worksheet, "SplitterLocation"); // Fallback

        // ✅ Remarks - special instructions from TIME (important for modification outdoor)
        data.Remarks = ExtractRightSideValueByLabels(worksheet, "Remarks", "REMARKS", "Note", "Notes", "Comments")
                    ?? ExtractByLabel(worksheet, "Remarks"); // Fallback

        // ✅ Parse appointment with timezone - handle various label formats
        var appointmentDateStr = ExtractRightSideValueByLabels(worksheet, 
            "APPOINT. DATE & TIME", "APPOINT DATE & TIME", "APPOINTMENT DATE & TIME",
            "Appointment Date", "APPOINTMENT DATE", "Date", "Installation Date", "Service Date")
            ?? ExtractByLabel(worksheet, "AppointmentDate"); // Fallback
        data.AppointmentDateTime = ParseAppointmentDateTimeWithTimezone(appointmentDateStr);

        // ✅ Materials: Usually empty for modification outdoor (customer brings device)
        // But still parse in case Excel explicitly indicates materials are needed
        var materials = ParseMaterials(worksheet);
        // For modification outdoor, default IsRequired to false unless Excel explicitly says YES
        foreach (var material in materials)
        {
            // Keep IsRequired as determined by ResolveMaterialIsRequired logic
            // But log that for modification outdoor, materials are typically not required
            if (material.IsRequired)
            {
                _logger.LogInformation("⚠️ Material marked as required for modification outdoor: {Material} (rare - customer may have lost device)", 
                    material.Name);
            }
        }
        data.Materials = materials;

        // Clean up Service Address (remove extra spaces and commas)
        if (!string.IsNullOrEmpty(data.ServiceAddress))
        {
            data.ServiceAddress = Regex.Replace(data.ServiceAddress, @"\s*,\s*,", ","); // Remove double commas
            data.ServiceAddress = Regex.Replace(data.ServiceAddress, @"\s+", " "); // Multiple spaces to single
            data.ServiceAddress = data.ServiceAddress.Trim().TrimEnd(',', ' '); // Remove trailing commas/spaces
        }

        data.ConfidenceScore = CalculateConfidenceScore(data, "MODIFICATION_OUTDOOR");

        _logger.LogInformation("✅ Modification Outdoor parsed: ServiceId={ServiceId}, NewAddress={NewAddr}, OldAddress={OldAddr}, OnuPassword={OnuPwd}, Materials={MaterialCount}, Confidence={Confidence}",
            data.ServiceId, data.ServiceAddress ?? "(null)", data.OldAddress ?? "(null)", 
            !string.IsNullOrEmpty(data.OnuPassword) ? "***" : "(null)", materials.Count, data.ConfidenceScore);

        return data;
    }

    private ParsedOrderData ParseModificationIndoor(IWorksheet worksheet, string fileName)
    {
        var data = ParseModificationOutdoor(worksheet, fileName);
        data.OrderTypeCode = "MODIFICATION_INDOOR";
        data.OrderTypeHint = "Modification Indoor";
        return data;
    }

    /// <summary>
    /// Detect if activation is Celcom, Digi, or standard TIME, then parse accordingly
    /// ✅ FIXED: Check for TBBN Service ID FIRST - if Service ID starts with TBBN, it's definitely a TIME order
    /// </summary>
    private ParsedOrderData DetectAndParseActivation(IWorksheet worksheet, string fileName)
    {
        // ✅ PRIORITY 1: Check for TBBN Service ID FIRST - if Service ID starts with TBBN, it's definitely a TIME order
        var serviceId = ExtractRightSideValueByLabels(worksheet, "Service ID", "SERVICE ID", "TBBN", "ServiceID");
        if (string.IsNullOrEmpty(serviceId))
        {
            // Fallback: Search for TBBN pattern anywhere in the sheet
            serviceId = FindPatternInSheet(worksheet, @"TBBN[A-Z]?\d{5,}[A-Z]?");
        }
        
        if (!string.IsNullOrEmpty(serviceId) && serviceId.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
        {
            // This is a TIME order (FTTH/FTTO), not Celcom/Digi
            _logger.LogInformation("✅ Detected TBBN Service ID ({ServiceId}) - parsing as TIME FTTH/FTTO activation", serviceId);
            return ParseActivation(worksheet, fileName);
        }
        
        // ✅ PRIORITY 2: Check if this is a Celcom activation (only if no TBBN Service ID found)
        var partnerServiceId = ExtractRightSideValueByLabels(worksheet, 
            "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID",
            "DIGI ORDER ID", "Digi Order ID");
        
        if (!string.IsNullOrEmpty(partnerServiceId))
        {
            if (partnerServiceId.StartsWith("CELCOM", StringComparison.OrdinalIgnoreCase))
            {
                // It's a TIME-Celcom activation
                _logger.LogInformation("✅ Detected PARTNER SERVICE ID starting with CELCOM ({PartnerServiceId}) - parsing as TIME-Celcom activation", partnerServiceId);
                return ParseCelcomActivation(worksheet, fileName);
            }
            
            if (partnerServiceId.StartsWith("DIGI", StringComparison.OrdinalIgnoreCase) ||
                partnerServiceId.StartsWith("DIGI00", StringComparison.OrdinalIgnoreCase))
            {
                // It's a TIME-Digi activation
                _logger.LogInformation("✅ Detected PARTNER SERVICE ID starting with DIGI ({PartnerServiceId}) - parsing as TIME-Digi activation", partnerServiceId);
                return ParseDigiActivation(worksheet, fileName);
            }
        }
        
        // Also check for Digi Order ID field specifically
        var digiOrderId = ExtractRightSideValueByLabels(worksheet,
            "DIGI ORDER ID", "Digi Order ID", "Digi Order ID:", "DIGI ORDERID");
        if (!string.IsNullOrEmpty(digiOrderId) && 
            digiOrderId.StartsWith("DIGI", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("✅ Detected DIGI ORDER ID ({DigiOrderId}) - parsing as TIME-Digi activation", digiOrderId);
            return ParseDigiActivation(worksheet, fileName);
        }
        
        // Standard TIME activation (fallback)
        _logger.LogInformation("No specific partner indicators found - parsing as standard TIME activation");
        return ParseActivation(worksheet, fileName);
    }

    /// <summary>
    /// Parse TIME-Celcom activation order from Excel
    /// Handles Celcom-specific fields: PARTNER SERVICE ID, REFERENCE NO, NIS CODE, etc.
    /// </summary>
    private ParsedOrderData ParseCelcomActivation(IWorksheet worksheet, string fileName)
    {
        _logger.LogInformation("📋 Parsing TIME-CELCOM ACTIVATION form: {FileName}", fileName);

        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "ACTIVATION",
            OrderTypeHint = "TIME-Celcom Activation",
            PartnerCode = "TIME-CELCOM"
        };

        // ✅ Extract PARTNER SERVICE ID (CELCOM0016996) - this is the Service ID for Celcom
        data.ServiceId = ExtractRightSideValueByLabels(worksheet, 
            "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID", "SERVICE ID")
            ?? ExtractByLabel(worksheet, "ServiceId");
        
        // ✅ Extract REFERENCE NO (mandatory for payment processing)
        var referenceNo = ExtractRightSideValueByLabels(worksheet, 
            "REFERENCE NO", "REFERENCE NO:", "REFERENCE NUMBER", "REF NO");
        if (!string.IsNullOrEmpty(referenceNo))
        {
            // Store in Remarks
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"Reference No: {referenceNo}"
                : $"{data.Remarks}\nReference No: {referenceNo}";
        }

        // ✅ Customer details
        data.CustomerName = ExtractRightSideValueByLabels(worksheet, 
            "CUSTOMER NAME", "Customer Name", "Name")
            ?? ExtractByLabel(worksheet, "CustomerName");
        
        data.ContactPerson = ExtractRightSideValueByLabels(worksheet, 
            "CONTACT PERSON", "Contact Person", "Contact")
            ?? ExtractByLabel(worksheet, "ContactPerson");
        
        // Primary contact
        var customerPhone = ExtractRightSideValueByLabels(worksheet, 
            "CONTACT NO", "CONTACT NO.", "Contact No", "Phone", "Contact", "Mobile");
        data.CustomerPhone = NormalizePhone(customerPhone);
        
        // ✅ Secondary contact (store in remarks)
        var secondaryContact = ExtractRightSideValueByLabels(worksheet, 
            "SECONDARY CONTACT NO", "SECONDARY CONTACT NO.", "Secondary Contact No", "Secondary Contact");
        if (!string.IsNullOrEmpty(secondaryContact))
        {
            var normalizedSecondary = NormalizePhone(secondaryContact);
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"Secondary Contact: {normalizedSecondary}"
                : $"{data.Remarks}\nSecondary Contact: {normalizedSecondary}";
        }
        
        data.CustomerEmail = ExtractRightSideValueByLabels(worksheet, 
            "EMAIL", "Email", "E-mail", "Email Address")
            ?? ExtractByLabel(worksheet, "CustomerEmail");
        
        // ✅ Service Address
        data.ServiceAddress = ExtractRightSideValueByLabels(worksheet, 
            "SERVICE ADDRESS", "Service Address", "Installation Address", "Address")
            ?? ExtractByLabel(worksheet, "ServiceAddress");

        // ✅ NIS CODE
        var nisCode = ExtractRightSideValueByLabels(worksheet, 
            "NIS CODE", "NIS CODE:", "NIS", "NIS Code");
        if (!string.IsNullOrEmpty(nisCode))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"NIS Code: {nisCode}"
                : $"{data.Remarks}\nNIS Code: {nisCode}";
        }

        // ✅ Appointment Date & Time - Parse "18 Nov 2025 10:00:00" format
        var appointmentDateTimeStr = ExtractRightSideValueByLabels(worksheet, 
            "APPOINT. DATE & TIME", "APPOINT DATE & TIME", "APPOINTMENT DATE & TIME",
            "Appointment Date", "APPOINTMENT DATE", "Date & Time", "Date");
        
        // Parse date/time string like "18 Nov 2025 10:00:00"
        data.AppointmentDateTime = ParseCelcomAppointmentDateTime(appointmentDateTimeStr);

        // ✅ Package / Bandwidth - Combined field: "Celcom Wholesale Broadband Residential 100Mbps - 24 Months"
        var packageBandwidth = ExtractRightSideValueByLabels(worksheet, 
            "PACKAGE / BANDWIDTH", "PACKAGE / BANDWIDTH:", "PACKAGE", "Package / Bandwidth");
        
        if (!string.IsNullOrEmpty(packageBandwidth))
        {
            ParseCelcomPackageBandwidth(packageBandwidth, data);
        }

        // ✅ LOGIN ID - Format: "CELCOM0016996@celcomhome"
        data.Username = ExtractRightSideValueByLabels(worksheet, 
            "LOGIN ID", "LOGIN ID:", "Login ID", "Username")
            ?? ExtractByLabel(worksheet, "Username");
        
        // ✅ PASSWORD
        data.Password = ExtractRightSideValueByLabels(worksheet, 
            "PASSWORD", "Password", "PWD")
            ?? ExtractByLabel(worksheet, "Password");

        // ✅ ONU Password
        data.OnuPassword = ExtractRightSideValueByLabels(worksheet, 
            "ONU PASSWORD", "ONU PASSWORD:", "ONU Password", "Password ONU")
            ?? ExtractByLabel(worksheet, "OnuPassword");

        // ✅ Network IP addresses (may be blank)
        data.InternetWanIp = ExtractRightSideValueByLabels(worksheet, 
            "WAN IP", "WAN IP:", "Wan IP");
        data.InternetLanIp = ExtractRightSideValueByLabels(worksheet, 
            "LAN IP", "LAN IP:", "Lan IP");
        data.InternetGateway = ExtractRightSideValueByLabels(worksheet, 
            "GATEWAY", "Gateway", "GATEWAY:");
        data.InternetSubnetMask = ExtractRightSideValueByLabels(worksheet, 
            "SUBNET MASK", "SUBNET MASK:", "Subnet Mask");

        // ✅ Equipment (Router and ONU)
        var equipmentInfo = ExtractCelcomEquipmentInfo(worksheet);
        if (!string.IsNullOrEmpty(equipmentInfo))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? equipmentInfo
                : $"{data.Remarks}\n{equipmentInfo}";
        }

        // ✅ Splitter Location (complex structured text)
        data.SplitterLocation = ExtractRightSideValueByLabels(worksheet, 
            "SPLITTER LOCATION", "Splitter Location", "SPLITTER", "Splitter");

        // ✅ Remarks section
        var remarks = ExtractRightSideValueByLabels(worksheet, 
            "REMARKS", "Remarks", "Note", "Notes", "Comments");
        if (!string.IsNullOrEmpty(remarks))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? remarks
                : $"{data.Remarks}\n{remarks}";
        }

        // Calculate confidence
        data.ConfidenceScore = CalculateConfidenceScore(data, "ACTIVATION");

        _logger.LogInformation("✅ TIME-Celcom Activation parsed: ServiceId={ServiceId}, Customer={Customer}, Date={Date}, Reference={Ref}, Confidence={Confidence}",
            data.ServiceId, data.CustomerName, data.AppointmentDateTime, referenceNo, data.ConfidenceScore);

        return data;
    }

    /// <summary>
    /// Parse Modification Outdoor from DataTable (ExcelDataReader format)
    /// Uses generic label-based extraction for future-proof parsing
    /// </summary>
    private ParsedOrderData ParseModificationOutdoorFromDataTable(DataTable dataTable, string fileName)
    {
        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "MODIFICATION_OUTDOOR",
            OrderTypeHint = "Modification Outdoor",
            PartnerCode = "TIME"
        };

        _logger.LogInformation("🔍 Parsing Modification Outdoor from DataTable: {Rows} rows, {Cols} columns", 
            dataTable.Rows.Count, dataTable.Columns.Count);

        // ✅ Extract Service ID first (for database lookup) - with normalized header detection
        data.ServiceId = ExtractServiceIdWithNormalizedHeader(dataTable);
        _logger.LogInformation("ServiceId extracted: '{ServiceId}'", data.ServiceId ?? "(null)");

        // ✅ Extract all fields using generic, future-proof label-based extraction
        data.CustomerName = ExtractRightSideValueByLabels(dataTable, "Customer Name", "CUSTOMER NAME", "Name");
        data.ContactPerson = ExtractRightSideValueByLabels(dataTable, "Contact Person", "CONTACT PERSON", "Contact");
        var customerPhone = ExtractRightSideValueByLabels(dataTable, "Contact No", "CONTACT NO", "Phone", "Contact", "Mobile");
        data.CustomerPhone = NormalizePhone(customerPhone);
        data.CustomerEmail = ExtractRightSideValueByLabels(dataTable, "Email", "EMAIL", "E-mail", "Email Address");

        // ✅ New Address (Service Address) - where customer is moving to
        data.ServiceAddress = ExtractRightSideValueByLabels(dataTable, "Service Address", "SERVICE ADDRESS", "New Address", "NEW ADDRESS", "Installation Address");

        // ✅ Old Address - where customer is moving from
        data.OldAddress = ExtractRightSideValueByLabels(dataTable, "Old Address", "OLD ADDRESS", "Previous Address", "PREVIOUS ADDRESS", "Current Address");

        // ✅ New ONU Password - provided by TIME for new location (critical for modification outdoor)
        data.OnuPassword = ExtractRightSideValueByLabels(dataTable, "ONU Password", "ONU PASSWORD", "Password ONU");

        // Other network credentials
        // ✅ Username/Login ID - try USERNAME first (more common), then LOGIN ID, skip if value looks like a label (ends with ":")
        var usernameValue = ExtractRightSideValueByLabels(dataTable, "Username", "USERNAME", "User Name", "Login ID", "LOGIN ID");
        if (!string.IsNullOrWhiteSpace(usernameValue) && !usernameValue.TrimEnd().EndsWith(":"))
        {
            data.Username = usernameValue;
        }
        data.Password = ExtractRightSideValueByLabels(dataTable, "Password", "PASSWORD", "PWD");
        data.PackageName = ExtractRightSideValueByLabels(dataTable, "Package Name", "PACKAGE", "Package", "Plan Name");
        data.Bandwidth = ExtractRightSideValueByLabels(dataTable, "Bandwidth", "BANDWIDTH", "Speed", "Internet Speed");
        data.VoipServiceId = ExtractVoipServiceIdFromDataTable(dataTable);
        data.SplitterLocation = ExtractRightSideValueByLabels(dataTable, "Splitter Location", "SPLITTER LOCATION", "Splitter", "SPLITTER");

        // ✅ Remarks - special instructions from TIME (important for modification outdoor)
        data.Remarks = ExtractRightSideValueByLabels(dataTable, "Remarks", "REMARKS", "Note", "Notes", "Comments");

        // ✅ Parse appointment date with timezone - handle various label formats
        var appointmentDateStr = ExtractRightSideValueByLabels(dataTable, 
            "APPOINT. DATE & TIME", "APPOINT DATE & TIME", "APPOINTMENT DATE & TIME",
            "Appointment Date", "APPOINTMENT DATE", "Date", "Installation Date", "Service Date");
        data.AppointmentDateTime = ParseAppointmentDateTimeWithTimezone(appointmentDateStr);

        // Clean up Service Address (remove extra spaces and commas)
        if (!string.IsNullOrEmpty(data.ServiceAddress))
        {
            data.ServiceAddress = Regex.Replace(data.ServiceAddress, @"\s*,\s*,", ","); // Remove double commas
            data.ServiceAddress = Regex.Replace(data.ServiceAddress, @"\s+", " "); // Multiple spaces to single
            data.ServiceAddress = data.ServiceAddress.Trim().TrimEnd(',', ' '); // Remove trailing commas/spaces
        }

        // ✅ Materials: Usually empty for modification outdoor (customer brings device)
        // But still parse in case Excel explicitly indicates materials are needed
        var materials = ParseMaterialsFromDataTable(dataTable);
        // For modification outdoor, default IsRequired to false unless Excel explicitly says YES
        foreach (var material in materials)
        {
            // Keep IsRequired as determined by ResolveMaterialIsRequired logic
            // But log that for modification outdoor, materials are typically not required
            if (material.IsRequired)
            {
                _logger.LogInformation("⚠️ Material marked as required for modification outdoor: {Material} (rare - customer may have lost device)", 
                    material.Name);
            }
        }
        data.Materials = materials;

        data.ConfidenceScore = CalculateConfidenceScore(data, "MODIFICATION_OUTDOOR");

        _logger.LogInformation("✅ Modification Outdoor parsed from DataTable: ServiceId={ServiceId}, NewAddress={NewAddr}, OldAddress={OldAddr}, OnuPassword={OnuPwd}, Materials={MaterialCount}, Confidence={Confidence}",
            data.ServiceId, data.ServiceAddress ?? "(null)", data.OldAddress ?? "(null)", 
            !string.IsNullOrEmpty(data.OnuPassword) ? "***" : "(null)", materials.Count, data.ConfidenceScore);

        return data;
    }

    private ParsedOrderData ParseGenericTimeOrder(IWorksheet worksheet, string fileName, string orderType)
    {
        var data = ParseModificationOutdoor(worksheet, fileName);
        data.OrderTypeCode = orderType;
        data.OrderTypeHint = orderType.Replace("_", " ");
        return data;
    }

    #region Field Extraction with Label Mappings

    private static readonly Dictionary<string, string[]> LabelMappings = new()
    {
        ["CustomerName"] = new[] { "CUSTOMER NAME", "NAMA PELANGGAN", "NAME", "SUBSCRIBER NAME" },
        ["ContactPerson"] = new[] { "CONTACT PERSON", "CONTACT NAME", "PIC" },
        ["CustomerPhone"] = new[] { "CONTACT NUMBER", "CONTACT NO", "PHONE", "TEL", "MOBILE", "H/P", "HANDPHONE" },
        ["CustomerEmail"] = new[] { "EMAIL", "E-MAIL", "EMAIL ADDRESS" },
        ["ServiceAddress"] = new[] { "SERVICE ADDRESS", "NEW ADDRESS", "INSTALLATION ADDRESS", "ADDRESS" },
        ["OldAddress"] = new[] { "OLD ADDRESS", "PREVIOUS ADDRESS", "CURRENT ADDRESS", "OLD ADD", "PREV ADD", "CURRENT ADD", "FROM ADDRESS", "ORIGINAL ADDRESS", "EXISTING ADDRESS" },
        ["AppointmentDate"] = new[] { "APPOINT. DATE & TIME", "APPOINT DATE & TIME", "APPOINTMENT DATE", "DATE & TIME", "DATE" },
        ["ServiceId"] = new[] { "SERVICE ID", "TBBN", "SERVICE NO", "SID" },
        ["PackageName"] = new[] { "PACKAGE", "PLAN", "PRODUCT" },
        ["Bandwidth"] = new[] { "BANDWIDTH", "SPEED", "MBPS" },
        ["OnuSerialNumber"] = new[] { "SERIAL NUMBER", "S/N", "SERIAL NO" },
        ["OnuPassword"] = new[] { "ONU PASSWORD", "PASSWORD ONU" },
        ["Username"] = new[] { "LOGIN ID", "USERNAME", "USER NAME" },
        ["Password"] = new[] { "PASSWORD", "PWD" },
        ["VoipPassword"] = new[] { "VOIP PASSWORD", "VOICE PASSWORD" },
        ["SplitterLocation"] = new[] { "SPLITTER", "SPLITTER LOCATION" }
    };

    private string? ExtractByLabel(IWorksheet worksheet, string fieldName)
    {
        if (LabelMappings.TryGetValue(fieldName, out var labels))
        {
            foreach (var label in labels)
            {
                var result = SearchLabelAndGetValue(worksheet, label);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogDebug("✓ {Field} = '{Value}' (via label '{Label}')", fieldName, result, label);
                    return result;
                }
            }
        }

        _logger.LogWarning("✗ {Field} = NOT FOUND", fieldName);
        return null;
    }

    private string? SearchLabelAndGetValue(IWorksheet worksheet, string label)
    {
        var labelUpper = label.ToUpperInvariant();

        // Search entire used range
        for (int row = 1; row <= worksheet.UsedRange.LastRow; row++)
        {
            for (int col = 1; col <= worksheet.UsedRange.LastColumn; col++)
            {
                var cell = worksheet.Range[row, col];
                var cellValue = cell.Text?.Trim();
                
                if (string.IsNullOrEmpty(cellValue)) continue;

                var cellUpper = cellValue.ToUpperInvariant();
                var cleanCell = cellUpper.TrimEnd(':', ' ');

                if (cleanCell == labelUpper || cleanCell.Contains(labelUpper))
                {
                    // Check if value is in same cell after colon
                    var colonIndex = cellValue.IndexOf(':');
                    if (colonIndex >= 0 && colonIndex < cellValue.Length - 1)
                    {
                        var afterColon = cellValue.Substring(colonIndex + 1).Trim();
                        if (!string.IsNullOrWhiteSpace(afterColon) && afterColon.Length > 1)
                        {
                            return afterColon;
                        }
                    }

                    // Check cells to the right (up to 6 columns)
                    for (int offset = 1; offset <= 6 && col + offset <= worksheet.UsedRange.LastColumn; offset++)
                    {
                        var rightCell = worksheet.Range[row, col + offset];
                        var rightValue = rightCell.Text?.Trim();
                        if (IsValidValue(rightValue))
                        {
                            return rightValue;
                        }
                    }

                    // Check cell below
                    if (row + 1 <= worksheet.UsedRange.LastRow)
                    {
                        var belowCell = worksheet.Range[row + 1, col];
                        var belowValue = belowCell.Text?.Trim();
                        if (IsValidValue(belowValue))
                        {
                            return belowValue;
                        }
                    }
                }
            }
        }

        return null;
    }

    private bool IsValidValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (trimmed.EndsWith(":")) return false;
        if (trimmed == ":" || trimmed == "-" || trimmed == "N/A" || trimmed == "NA") return false;
        return true;
    }

    #endregion

    #region VOIP Service ID Extraction (FIXED)

    /// <summary>
    /// Enhanced VOIP Service ID extraction - properly distinguishes phone numbers from bandwidth
    /// </summary>
    private string? ExtractVoipServiceId(IWorksheet worksheet)
    {
        // Strategy 1: Look for SERVICE ID under VOIP section
        // Find "VOIP" section header first
        for (int row = 1; row <= worksheet.UsedRange.LastRow; row++)
        {
            for (int col = 1; col <= worksheet.UsedRange.LastColumn; col++)
            {
                var cellValue = GetCellValue(worksheet, row, col)?.ToUpperInvariant();
                
                // Found VOIP section header
                if (cellValue?.Contains("VOIP") == true && !cellValue.Contains("PASSWORD"))
                {
                    // Search next 10 rows for SERVICE ID or phone number pattern
                    for (int searchRow = row; searchRow <= Math.Min(row + 10, worksheet.UsedRange.LastRow); searchRow++)
                    {
                        for (int searchCol = 1; searchCol <= worksheet.UsedRange.LastColumn; searchCol++)
                        {
                            var value = GetCellValue(worksheet, searchRow, searchCol);
                            
                            // Match phone number pattern (Malaysian format)
                            if (IsPhoneNumber(value))
                            {
                                _logger.LogInformation("✓ Found VOIP phone number: {Value} at R{Row}C{Col}", value, searchRow, searchCol);
                                return NormalizePhone(value);
                            }
                        }
                    }
                }
            }
        }

        // Strategy 2: Look for specific label "SERVICE ID / PASSWORD" in VOIP section
        var voipIdLabels = new[] { "SERVICE ID / PASSWORD", "SERVICE ID", "VOIP ID", "PHONE NUMBER", "VOICE SERVICE" };
        foreach (var label in voipIdLabels)
        {
            var result = SearchLabelAndGetValue(worksheet, label);
            if (!string.IsNullOrWhiteSpace(result) && IsPhoneNumber(result))
            {
                return NormalizePhone(result);
            }
        }

        _logger.LogWarning("✗ VOIP Service ID not found");
        return null;
    }

    private bool IsPhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        
        // Malaysian phone number patterns
        var phonePattern = @"^(?:\+?60?)?0?1[0-9]{8,9}$|^0[0-9]{9,10}$|^03[0-9]{8}$";
        var cleaned = value.Trim().Replace(" ", "").Replace("-", "").Replace("/", "");
        
        return Regex.IsMatch(cleaned, phonePattern);
    }

    #endregion

    #region Appointment Date/Time Parsing with Timezone (FIXED)

    /// <summary>
    /// Parse appointment date/time with GMT+8 timezone awareness
    /// Fixes issue where 10:00 AM Malaysia time was showing as 6:00 PM
    /// </summary>
    private DateTime? ParseAppointmentDateTimeWithTimezone(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;

        var trimmed = dateStr.Trim();

        // Try various formats
        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",    // 2026-01-24 10:00:00 (TIME common format)
            "yyyy-MM-dd HH:mm",       // 2026-01-24 10:00
            "dd/MM/yyyy HH:mm:ss",    // 24/01/2026 10:00:00
            "dd/MM/yyyy HH:mm",       // 24/01/2026 10:00
            "dd/MM/yyyy h:mm tt",     // 24/01/2026 10:00 AM
            "dd-MM-yyyy HH:mm",
            "yyyy-MM-dd",
            "dd/MM/yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(trimmed, format, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var localDateTime))
            {
                // ✅ FIX: Treat parsed datetime as Malaysia time (GMT+8), then convert to UTC for storage
                var malaysiaDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
                var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(malaysiaDateTime, MalaysiaTimeZone);
                
                _logger.LogInformation("✓ Parsed appointment: Local={Local} → UTC={Utc} (GMT+8 conversion applied)",
                    localDateTime, utcDateTime);
                
                return utcDateTime;
            }
        }

        // Try general parse
        if (DateTime.TryParse(trimmed, out var generalResult))
        {
            var malaysiaDateTime = DateTime.SpecifyKind(generalResult, DateTimeKind.Unspecified);
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(malaysiaDateTime, MalaysiaTimeZone);
            return utcDateTime;
        }

        _logger.LogWarning("✗ Could not parse appointment date: {DateString}", trimmed);
        return null;
    }

    #endregion

    /// <summary>
    /// Parse TIME-Celcom activation order from DataTable (ExcelDataReader format for .xls files)
    /// Handles Celcom-specific fields: PARTNER SERVICE ID, REFERENCE NO, NIS CODE, etc.
    /// </summary>
    private ParsedOrderData ParseCelcomActivationFromDataTable(DataTable dataTable, string fileName)
    {
        _logger.LogInformation("📋 Parsing TIME-CELCOM ACTIVATION from DataTable: {FileName}", fileName);

        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "ACTIVATION",
            OrderTypeHint = "TIME-Celcom Activation",
            PartnerCode = "TIME-CELCOM"
        };

        // ✅ Extract PARTNER SERVICE ID (CELCOM0016996) - this is the Service ID for Celcom
        data.ServiceId = ExtractRightSideValueByLabels(dataTable, 
            "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID", "SERVICE ID");
        
        // ✅ Extract REFERENCE NO (mandatory for payment processing)
        var referenceNo = ExtractRightSideValueByLabels(dataTable, 
            "REFERENCE NO", "REFERENCE NO:", "REFERENCE NUMBER", "REF NO");
        if (!string.IsNullOrEmpty(referenceNo))
        {
            // Store in Remarks
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"Reference No: {referenceNo}"
                : $"{data.Remarks}\nReference No: {referenceNo}";
        }

        // ✅ Customer details
        data.CustomerName = ExtractRightSideValueByLabels(dataTable, 
            "CUSTOMER NAME", "Customer Name", "Name");
        
        data.ContactPerson = ExtractRightSideValueByLabels(dataTable, 
            "CONTACT PERSON", "Contact Person", "Contact");
        
        // Primary contact
        var customerPhone = ExtractRightSideValueByLabels(dataTable, 
            "CONTACT NO", "CONTACT NO.", "Contact No", "Phone", "Contact", "Mobile");
        data.CustomerPhone = NormalizePhone(customerPhone);
        
        // ✅ Secondary contact (store in remarks)
        var secondaryContact = ExtractRightSideValueByLabels(dataTable, 
            "SECONDARY CONTACT NO", "SECONDARY CONTACT NO.", "Secondary Contact No", "Secondary Contact");
        if (!string.IsNullOrEmpty(secondaryContact))
        {
            var normalizedSecondary = NormalizePhone(secondaryContact);
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"Secondary Contact: {normalizedSecondary}"
                : $"{data.Remarks}\nSecondary Contact: {normalizedSecondary}";
        }
        
        data.CustomerEmail = ExtractRightSideValueByLabels(dataTable, 
            "EMAIL", "Email", "E-mail", "Email Address");
        
        // ✅ Service Address
        data.ServiceAddress = ExtractRightSideValueByLabels(dataTable, 
            "SERVICE ADDRESS", "Service Address", "Installation Address", "Address");

        // ✅ NIS CODE
        var nisCode = ExtractRightSideValueByLabels(dataTable, 
            "NIS CODE", "NIS CODE:", "NIS", "NIS Code");
        if (!string.IsNullOrEmpty(nisCode))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"NIS Code: {nisCode}"
                : $"{data.Remarks}\nNIS Code: {nisCode}";
        }

        // ✅ Appointment Date & Time - Parse "18 Nov 2025 10:00:00" format
        // Try multiple label variations including with/without colons and periods
        var appointmentDateTimeStr = ExtractRightSideValueByLabels(dataTable, 
            "APPOINT. DATE & TIME", "APPOINT. DATE & TIME:", "APPOINT DATE & TIME", "APPOINT DATE & TIME:",
            "APPOINTMENT DATE & TIME", "APPOINTMENT DATE & TIME:", "APPOINTMENT DATE", "APPOINTMENT DATE:",
            "Appointment Date", "Appointment Date & Time", "Date & Time", "Date");
        
        // Parse date/time string like "18 Nov 2025 10:00:00"
        data.AppointmentDateTime = ParseCelcomAppointmentDateTime(appointmentDateTimeStr);
        
        if (!data.AppointmentDateTime.HasValue && !string.IsNullOrEmpty(appointmentDateTimeStr))
        {
            _logger.LogWarning("⚠️ Failed to parse appointment date/time: '{DateTimeStr}' for Celcom order", appointmentDateTimeStr);
        }

        // ✅ Package / Bandwidth - Combined field: "Celcom Wholesale Broadband Residential 100Mbps - 24 Months"
        var packageBandwidth = ExtractRightSideValueByLabels(dataTable, 
            "PACKAGE / BANDWIDTH", "PACKAGE / BANDWIDTH:", "PACKAGE", "Package / Bandwidth");
        
        if (!string.IsNullOrEmpty(packageBandwidth))
        {
            ParseCelcomPackageBandwidth(packageBandwidth, data);
        }

        // ✅ LOGIN ID - Format: "CELCOM0016996@celcomhome"
        // Try multiple label variations
        data.Username = ExtractRightSideValueByLabels(dataTable, 
            "LOGIN ID", "LOGIN ID:", "Login ID", "Login ID:", "Username", "USERNAME", "USER NAME");
        
        if (string.IsNullOrEmpty(data.Username))
        {
            _logger.LogWarning("⚠️ LOGIN ID not found for Celcom order");
        }
        
        // ✅ PASSWORD
        data.Password = ExtractRightSideValueByLabels(dataTable, 
            "PASSWORD", "PASSWORD:", "Password", "Password:", "PWD");
        
        if (string.IsNullOrEmpty(data.Password))
        {
            _logger.LogWarning("⚠️ PASSWORD not found for Celcom order");
        }

        // ✅ ONU Password - try multiple label variations including lowercase
        data.OnuPassword = ExtractRightSideValueByLabels(dataTable, 
            "ONU PASSWORD", "ONU PASSWORD:", "onu PASSWORD", "onu PASSWORD:", "ONU Password", "ONU Password:",
            "Password ONU", "Password ONU:", "ONU PWD", "ONU PWD:");
        
        if (string.IsNullOrEmpty(data.OnuPassword))
        {
            _logger.LogWarning("⚠️ ONU PASSWORD not found for Celcom order");
        }

        // ✅ Network IP addresses (may be blank)
        data.InternetWanIp = ExtractRightSideValueByLabels(dataTable, 
            "WAN IP", "WAN IP:", "Wan IP");
        data.InternetLanIp = ExtractRightSideValueByLabels(dataTable, 
            "LAN IP", "LAN IP:", "Lan IP");
        data.InternetGateway = ExtractRightSideValueByLabels(dataTable, 
            "GATEWAY", "Gateway", "GATEWAY:");
        data.InternetSubnetMask = ExtractRightSideValueByLabels(dataTable, 
            "SUBNET MASK", "SUBNET MASK:", "Subnet Mask");

        // ✅ Splitter Location (complex structured text)
        data.SplitterLocation = ExtractRightSideValueByLabels(dataTable, 
            "SPLITTER LOCATION", "Splitter Location", "SPLITTER", "Splitter");

        // ✅ Remarks section - extract FIRST (before equipment extraction) to get material info
        // REMARKS can span multiple cells/rows, so use special extraction
        var remarks = ExtractRemarksFromDataTable(dataTable);
        
        // ✅ Equipment (Router and ONU) - extract from DataTable AND from REMARKS
        var equipmentInfo = ExtractCelcomEquipmentInfoFromDataTable(dataTable);
        
        // Also extract equipment from REMARKS if present (e.g., "TP Link EX510", "ONU Huawei HG8140H5")
        if (!string.IsNullOrEmpty(remarks))
        {
            var equipmentFromRemarks = ExtractEquipmentFromRemarks(remarks);
            if (!string.IsNullOrEmpty(equipmentFromRemarks))
            {
                equipmentInfo = string.IsNullOrEmpty(equipmentInfo)
                    ? equipmentFromRemarks
                    : $"{equipmentInfo}, {equipmentFromRemarks}";
            }
        }
        
        if (!string.IsNullOrEmpty(equipmentInfo))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? equipmentInfo
                : $"{data.Remarks}\n{equipmentInfo}";
        }
        
        // Add REMARKS content after equipment info
        if (!string.IsNullOrEmpty(remarks))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? remarks
                : $"{data.Remarks}\n{remarks}";
        }

        // Extract materials from DataTable
        var materials = ParseMaterialsFromDataTable(dataTable);
        
        // ✅ Also extract materials from REMARKS section (e.g., "TP Link EX510", "ONU Huawei HG8140H5")
        if (!string.IsNullOrEmpty(remarks))
        {
            var materialsFromRemarks = ExtractMaterialsFromRemarks(remarks);
            if (materialsFromRemarks.Any())
            {
                // Merge with existing materials (avoid duplicates)
                foreach (var mat in materialsFromRemarks)
                {
                    if (!materials.Any(m => m.Name?.Equals(mat.Name, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        materials.Add(mat);
                    }
                }
            }
        }
        
        data.Materials = materials;

        // Calculate confidence - use Celcom-specific calculation that checks for critical fields
        data.ConfidenceScore = CalculateCelcomConfidenceScore(data);

        _logger.LogInformation("✅ TIME-Celcom Activation parsed from DataTable: ServiceId={ServiceId}, Customer={Customer}, Date={Date}, Reference={Ref}, Confidence={Confidence}",
            data.ServiceId, data.CustomerName, data.AppointmentDateTime, referenceNo, data.ConfidenceScore);

        return data;
    }

    /// <summary>
    /// Parse TIME-Digi activation order from DataTable (ExcelDataReader format for .xls files)
    /// Handles Digi-specific fields: DIGI ORDER ID, etc.
    /// </summary>
    private ParsedOrderData ParseDigiActivationFromDataTable(DataTable dataTable, string fileName)
    {
        _logger.LogInformation("📋 Parsing TIME-DIGI ACTIVATION from DataTable: {FileName}", fileName);

        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "ACTIVATION",
            OrderTypeHint = "TIME-Digi Activation",
            PartnerCode = "TIME-DIGI"
        };

        // ✅ Extract DIGI ORDER ID (DIGI00xxxx) - this is the Service ID for Digi
        data.ServiceId = ExtractRightSideValueByLabels(dataTable, 
            "DIGI ORDER ID", "Digi Order ID", "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "SERVICE ID");
        
        // ✅ Customer details
        data.CustomerName = ExtractRightSideValueByLabels(dataTable, 
            "CUSTOMER NAME", "Customer Name", "Name");
        
        // Primary contact
        var customerPhone = ExtractRightSideValueByLabels(dataTable, 
            "CONTACT", "CONTACT NO", "CONTACT NO.", "Contact No", "Phone", "Contact", "Mobile");
        data.CustomerPhone = NormalizePhone(customerPhone);
        
        // ✅ Service Address (Installation Address)
        data.ServiceAddress = ExtractRightSideValueByLabels(dataTable, 
            "INSTALLATION ADDRESS", "Installation Address", "SERVICE ADDRESS", "Service Address", "Address");

        // ✅ Preferred Date & Preferred Slot - try multiple label variations
        // For reschedule files, might be "APPOINT. DATE & TIME" or "APPOINTMENT DATE & TIME"
        var appointmentDateTimeStr = ExtractRightSideValueByLabels(dataTable, 
            "APPOINT. DATE & TIME", "APPOINT. DATE & TIME:", "APPOINT DATE & TIME", "APPOINT DATE & TIME:",
            "APPOINTMENT DATE & TIME", "APPOINTMENT DATE & TIME:", "APPOINTMENT DATE", "Appointment Date & Time");
        
        if (string.IsNullOrEmpty(appointmentDateTimeStr))
        {
            // Try separate fields
            var preferredDateStr = ExtractRightSideValueByLabels(dataTable, 
                "PREFERRED DATE", "Preferred Date", "PREFERRED DATE:", "Preferred Date:",
                "APPOINTMENT DATE", "Appointment Date", "APPOINTMENT DATE:", "Appointment Date:",
                "Date", "DATE");
            var preferredSlotStr = ExtractRightSideValueByLabels(dataTable, 
                "PREFERRED SLOT", "Preferred Slot", "PREFERRED SLOT:", "Preferred Slot:",
                "APPOINTMENT TIME", "Appointment Time", "APPOINTMENT TIME:", "Appointment Time:",
                "Time", "TIME", "SLOT", "Slot");
            
            // Combine date and slot if both present
            if (!string.IsNullOrEmpty(preferredDateStr) && !string.IsNullOrEmpty(preferredSlotStr))
            {
                appointmentDateTimeStr = $"{preferredDateStr} {preferredSlotStr}";
            }
            else if (!string.IsNullOrEmpty(preferredDateStr))
            {
                appointmentDateTimeStr = preferredDateStr;
            }
        }
        
        // Parse using Celcom-style date format parser (same format: "26 Nov 2025 10:00:00")
        data.AppointmentDateTime = ParseCelcomAppointmentDateTime(appointmentDateTimeStr);

        // ✅ Bandwidth
        data.Bandwidth = ExtractRightSideValueByLabels(dataTable, 
            "BANDWIDTH", "Bandwidth", "Speed", "Internet Speed");

        // ✅ Package / Bandwidth - Combined field: "DigiWholesale Broadband Residential 300Mbps - 24 Months"
        var packageBandwidth = ExtractRightSideValueByLabels(dataTable, 
            "PACKAGE / BANDWIDTH", "PACKAGE / BANDWIDTH:", "PACKAGE", "Package / Bandwidth");
        
        if (!string.IsNullOrEmpty(packageBandwidth))
        {
            ParseDigiPackageBandwidth(packageBandwidth, data);
        }

        // ✅ ONU Username
        data.Username = ExtractRightSideValueByLabels(dataTable, 
            "ONU USERNAME", "ONU USERNAME:", "Username", "User Name", "Login ID", "LOGIN ID");
        
        // ✅ Password
        data.Password = ExtractRightSideValueByLabels(dataTable, 
            "PASSWORD", "PASSWORD:", "Password", "Password:", "PWD");
        
        // ✅ ONU Password (if present - Digi may not always have this)
        data.OnuPassword = ExtractRightSideValueByLabels(dataTable, 
            "ONU PASSWORD", "ONU PASSWORD:", "onu PASSWORD", "onu PASSWORD:", "ONU Password", "ONU Password:",
            "Password ONU", "Password ONU:", "ONU PWD", "ONU PWD:");

        // ✅ Splitter Location
        data.SplitterLocation = ExtractRightSideValueByLabels(dataTable, 
            "SPLITTER LOCATION", "Splitter Location", "SPLITTER", "Splitter");

        // ✅ Remarks section - extract using multi-cell extraction (like Celcom)
        var remarks = ExtractRemarksFromDataTable(dataTable);
        
        // ✅ Equipment (Router and ONU) - extract from REMARKS if present
        if (!string.IsNullOrEmpty(remarks))
        {
            var equipmentFromRemarks = ExtractEquipmentFromRemarks(remarks);
            if (!string.IsNullOrEmpty(equipmentFromRemarks))
            {
                data.Remarks = string.IsNullOrEmpty(data.Remarks)
                    ? equipmentFromRemarks
                    : $"{data.Remarks}\n{equipmentFromRemarks}";
            }
        }
        
        if (!string.IsNullOrEmpty(remarks))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? remarks
                : $"{data.Remarks}\n{remarks}";
        }

        // Extract materials from DataTable
        var materials = ParseMaterialsFromDataTable(dataTable);
        
        // ✅ Also extract materials from REMARKS section (e.g., "TP Link EX510", "ONU Huawei HG8140H5")
        if (!string.IsNullOrEmpty(remarks))
        {
            var materialsFromRemarks = ExtractMaterialsFromRemarks(remarks);
            if (materialsFromRemarks.Any())
            {
                // Merge with existing materials (avoid duplicates)
                foreach (var mat in materialsFromRemarks)
                {
                    if (!materials.Any(m => m.Name?.Equals(mat.Name, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        materials.Add(mat);
                    }
                }
            }
        }
        
        data.Materials = materials;

        // Calculate confidence - use Digi-specific calculation
        data.ConfidenceScore = CalculateDigiConfidenceScore(data);

        _logger.LogInformation("✅ TIME-Digi Activation parsed from DataTable: ServiceId={ServiceId}, Customer={Customer}, Date={Date}, Confidence={Confidence}",
            data.ServiceId, data.CustomerName, data.AppointmentDateTime, data.ConfidenceScore);

        return data;
    }

    /// <summary>
    /// Extract equipment information (Router and ONU models) from Celcom Excel DataTable
    /// </summary>
    private string? ExtractCelcomEquipmentInfoFromDataTable(DataTable dataTable)
    {
        try
        {
            var equipmentInfo = new List<string>();
            
            // Look for Equipment section - typically has Router and ONU with YES/NO markers
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                var cellValue = dataTable.Rows[row][0]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(cellValue))
                {
                    cellValue = dataTable.Rows[row][1]?.ToString()?.Trim() ?? "";
                }
                
                if (cellValue.Contains("Equipment", StringComparison.OrdinalIgnoreCase))
                {
                    // Look for Router and ONU in subsequent rows
                    for (int nextRow = row + 1; nextRow < Math.Min(row + 10, dataTable.Rows.Count); nextRow++)
                    {
                        var label = dataTable.Rows[nextRow][0]?.ToString()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(label))
                        {
                            label = dataTable.Rows[nextRow][1]?.ToString()?.Trim() ?? "";
                        }
                        
                        var value = dataTable.Rows[nextRow][2]?.ToString()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(value) && dataTable.Columns.Count > 3)
                        {
                            value = dataTable.Rows[nextRow][3]?.ToString()?.Trim() ?? "";
                        }
                        
                        if (label.Contains("Router", StringComparison.OrdinalIgnoreCase) && 
                            !string.IsNullOrEmpty(value) && 
                            !value.Equals("YES", StringComparison.OrdinalIgnoreCase) &&
                            !value.Equals("NO", StringComparison.OrdinalIgnoreCase))
                        {
                            equipmentInfo.Add($"Router: {value}");
                        }
                        else if (label.Contains("ONU", StringComparison.OrdinalIgnoreCase) && 
                                 !string.IsNullOrEmpty(value) &&
                                 !value.Equals("YES", StringComparison.OrdinalIgnoreCase) &&
                                 !value.Equals("NO", StringComparison.OrdinalIgnoreCase))
                        {
                            equipmentInfo.Add($"ONU: {value}");
                        }
                    }
                    break;
                }
            }
            
            return equipmentInfo.Count > 0 ? string.Join(", ", equipmentInfo) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting Celcom equipment info from DataTable");
            return null;
        }
    }

    #region Celcom-Specific Helper Methods

    /// <summary>
    /// Check if worksheet contains Celcom indicators
    /// ✅ FIXED: Only check for PARTNER SERVICE ID starting with CELCOM, not just any "Celcom" text
    /// This prevents false positives when "Celcom" appears in unrelated text
    /// </summary>
    private bool ContainsCelcomIndicator(IWorksheet worksheet)
    {
        try
        {
            // ✅ SPECIFIC CHECK: Only check for "PARTNER SERVICE ID" with CELCOM pattern
            // Do NOT check for "Celcom" text anywhere in the file - that's too broad and causes false positives
            var partnerServiceId = ExtractRightSideValueByLabels(worksheet, 
                "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID");
            if (!string.IsNullOrEmpty(partnerServiceId) && 
                partnerServiceId.StartsWith("CELCOM", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // ✅ REMOVED: Generic "Celcom" text search - this was causing false positives
            // Only rely on PARTNER SERVICE ID field for accurate detection
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking for Celcom indicator");
        }
        
        return false;
    }

    /// <summary>
    /// Check if worksheet contains Digi indicators
    /// </summary>
    private bool ContainsDigiIndicator(IWorksheet worksheet)
    {
        try
        {
            // Check for "DIGI ORDER ID" or "PARTNER SERVICE ID" with DIGI pattern
            var digiOrderId = ExtractRightSideValueByLabels(worksheet, 
                "DIGI ORDER ID", "Digi Order ID", "Digi Order ID:", "DIGI ORDERID",
                "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID");
            if (!string.IsNullOrEmpty(digiOrderId) && 
                (digiOrderId.StartsWith("DIGI", StringComparison.OrdinalIgnoreCase) ||
                 digiOrderId.StartsWith("DIGI00", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            
            // Check for "TIME-Digi" or "Digi" in title/header
            for (int row = 1; row <= Math.Min(10, worksheet.UsedRange.LastRow); row++)
            {
                for (int col = 1; col <= Math.Min(10, worksheet.UsedRange.LastColumn); col++)
                {
                    var cellValue = GetCellValue(worksheet, row, col)?.ToString() ?? "";
                    if (cellValue.Contains("Digi", StringComparison.OrdinalIgnoreCase) ||
                        cellValue.Contains("DIGI", StringComparison.OrdinalIgnoreCase) ||
                        cellValue.Contains("TIME-Digi", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking for Digi indicator");
        }
        
        return false;
    }

    /// <summary>
    /// Parse Celcom appointment date/time format: "18 Nov 2025 10:00:00"
    /// </summary>
    private DateTime? ParseCelcomAppointmentDateTime(string? dateTimeStr)
    {
        if (string.IsNullOrWhiteSpace(dateTimeStr))
            return null;

        try
        {
            var trimmed = dateTimeStr.Trim();

            // Try with specific format patterns for Celcom format
            var formats = new[]
            {
                "dd MMM yyyy HH:mm:ss",     // "18 Nov 2025 10:00:00"
                "dd MMMM yyyy HH:mm:ss",    // "18 November 2025 10:00:00"
                "dd/MM/yyyy HH:mm:ss",      // "18/11/2025 10:00:00"
                "dd-MM-yyyy HH:mm:ss",      // "18-11-2025 10:00:00"
                "dd MMM yyyy HH:mm",        // "18 Nov 2025 10:00"
                "dd MMMM yyyy HH:mm",       // "18 November 2025 10:00"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(trimmed, format, 
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, 
                    out var parsed))
                {
                    // Convert to UTC with GMT+8 timezone awareness
                    var malaysiaDateTime = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
                    var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(malaysiaDateTime, MalaysiaTimeZone);
                    
                    _logger.LogInformation("✓ Parsed Celcom appointment: Local={Local} → UTC={Utc} (GMT+8 conversion applied)",
                        parsed, utcDateTime);
                    
                    return utcDateTime;
                }
            }

            // Try general parse as fallback
            if (DateTime.TryParse(trimmed, out var generalResult))
            {
                var malaysiaDateTime = DateTime.SpecifyKind(generalResult, DateTimeKind.Unspecified);
                var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(malaysiaDateTime, MalaysiaTimeZone);
                return utcDateTime;
            }

            _logger.LogWarning("✗ Could not parse Celcom appointment date/time: {DateTimeStr}", trimmed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Celcom appointment date/time: {DateTimeStr}", dateTimeStr);
        }

        return null;
    }

    /// <summary>
    /// Parse combined Package/Bandwidth field and extract package name and bandwidth
    /// Format: "Celcom Wholesale Broadband Residential 100Mbps - 24 Months"
    /// </summary>
    private void ParseCelcomPackageBandwidth(string packageBandwidth, ParsedOrderData data)
    {
        try
        {
            // Try to split on " - " delimiter
            var parts = packageBandwidth.Split(new[] { " - " }, StringSplitOptions.None);
            
            if (parts.Length >= 2)
            {
                // First part is package name
                data.PackageName = parts[0].Trim();
                
                // Extract bandwidth from package name (e.g., "100Mbps")
                var bandwidthMatch = Regex.Match(
                    data.PackageName, @"(\d+)\s*MBPS?", 
                    RegexOptions.IgnoreCase);
                if (bandwidthMatch.Success)
                {
                    data.Bandwidth = $"{bandwidthMatch.Groups[1].Value} Mbps";
                }
                
                // Store full package info in remarks if needed
                data.Remarks = string.IsNullOrEmpty(data.Remarks)
                    ? $"Package: {packageBandwidth}"
                    : $"{data.Remarks}\nPackage: {packageBandwidth}";
            }
            else
            {
                // Use full string as package name
                data.PackageName = packageBandwidth;
                
                // Try to extract bandwidth
                var bandwidthMatch = Regex.Match(
                    packageBandwidth, @"(\d+)\s*MBPS?", 
                    RegexOptions.IgnoreCase);
                if (bandwidthMatch.Success)
                {
                    data.Bandwidth = $"{bandwidthMatch.Groups[1].Value} Mbps";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Celcom package/bandwidth: {PackageBandwidth}", packageBandwidth);
            // Fallback: use full string as package name
            data.PackageName = packageBandwidth;
        }
    }

    /// <summary>
    /// Parse combined Package/Bandwidth field for Digi and extract package name and bandwidth
    /// Format: "DigiWholesale Broadband Residential 300Mbps - 24 Months"
    /// </summary>
    private void ParseDigiPackageBandwidth(string packageBandwidth, ParsedOrderData data)
    {
        try
        {
            // Try to split on " - " delimiter
            var parts = packageBandwidth.Split(new[] { " - " }, StringSplitOptions.None);
            
            if (parts.Length >= 2)
            {
                // First part is package name
                data.PackageName = parts[0].Trim();
                
                // Extract bandwidth from package name (e.g., "300Mbps")
                var bandwidthMatch = Regex.Match(
                    data.PackageName, @"(\d+)\s*MBPS?", 
                    RegexOptions.IgnoreCase);
                if (bandwidthMatch.Success)
                {
                    data.Bandwidth = $"{bandwidthMatch.Groups[1].Value} Mbps";
                }
            }
            else
            {
                // Use full string as package name
                data.PackageName = packageBandwidth;
                
                // Try to extract bandwidth
                var bandwidthMatch = Regex.Match(
                    packageBandwidth, @"(\d+)\s*MBPS?", 
                    RegexOptions.IgnoreCase);
                if (bandwidthMatch.Success)
                {
                    data.Bandwidth = $"{bandwidthMatch.Groups[1].Value} Mbps";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Digi package/bandwidth: {PackageBandwidth}", packageBandwidth);
            // Fallback: use full string as package name
            data.PackageName = packageBandwidth;
        }
    }

    /// <summary>
    /// Extract equipment information (Router and ONU models) from Celcom Excel
    /// </summary>
    private string? ExtractCelcomEquipmentInfo(IWorksheet worksheet)
    {
        try
        {
            var equipmentInfo = new List<string>();
            bool foundEquipmentSection = false;
            
            // Look for Equipment section - typically has Router and ONU with YES/NO markers
            for (int row = 1; row <= worksheet.UsedRange.LastRow; row++)
            {
                var cellValue = GetCellValue(worksheet, row, 1)?.ToString() ?? "";
                if (cellValue.Contains("Equipment", StringComparison.OrdinalIgnoreCase) ||
                    cellValue.Contains("Router", StringComparison.OrdinalIgnoreCase) ||
                    cellValue.Contains("ONU", StringComparison.OrdinalIgnoreCase))
                {
                    foundEquipmentSection = true;
                    
                    // Look for equipment details in next few rows
                    for (int i = row; i <= Math.Min(row + 10, worksheet.UsedRange.LastRow); i++)
                    {
                        var equipmentType = GetCellValue(worksheet, i, 1)?.ToString()?.Trim() ?? "";
                        var model = GetCellValue(worksheet, i, 2)?.ToString()?.Trim() ?? "";
                        var yesMarker = GetCellValue(worksheet, i, 3)?.ToString()?.Trim() ?? "";
                        
                        // Check if this row has equipment info
                        if ((equipmentType.Contains("Router", StringComparison.OrdinalIgnoreCase) ||
                             equipmentType.Contains("ONU", StringComparison.OrdinalIgnoreCase)) &&
                            !string.IsNullOrEmpty(model) &&
                            (yesMarker.Contains("X", StringComparison.OrdinalIgnoreCase) ||
                             yesMarker.Contains("YES", StringComparison.OrdinalIgnoreCase)))
                        {
                            equipmentInfo.Add($"{equipmentType}: {model}");
                        }
                    }
                    
                    if (foundEquipmentSection) break;
                }
            }
            
            return equipmentInfo.Any() ? string.Join(", ", equipmentInfo) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting Celcom equipment info");
            return null;
        }
    }

    /// <summary>
    /// Parse TIME-Digi activation order from Excel
    /// Handles Digi-specific fields: DIGI Order ID, Preferred Date, Preferred Slot, etc.
    /// </summary>
    private ParsedOrderData ParseDigiActivation(IWorksheet worksheet, string fileName)
    {
        _logger.LogInformation("📋 Parsing TIME-DIGI ACTIVATION form: {FileName}", fileName);

        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = "ACTIVATION",
            OrderTypeHint = "TIME-Digi Activation",
            PartnerCode = "TIME-DIGI"
        };

        // ✅ Extract DIGI Order ID (DIGI00xxxx) - this is the Service ID for Digi
        // Digi files use "PARTNER SERVICE ID" field (similar to Celcom)
        data.ServiceId = ExtractRightSideValueByLabels(worksheet, 
            "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID",
            "DIGI ORDER ID", "Digi Order ID", "Digi Order ID:", "DIGI ORDERID",
            "SERVICE ID")
            ?? ExtractByLabel(worksheet, "ServiceId");

        // ✅ Customer details
        data.CustomerName = ExtractRightSideValueByLabels(worksheet, 
            "CUSTOMER NAME", "Customer Name", "Name")
            ?? ExtractByLabel(worksheet, "CustomerName");
        
        data.ContactPerson = ExtractRightSideValueByLabels(worksheet, 
            "CONTACT PERSON", "Contact Person", "Contact")
            ?? ExtractByLabel(worksheet, "ContactPerson");
        
        // Contact (primary)
        var customerPhone = ExtractRightSideValueByLabels(worksheet, 
            "CONTACT NO", "CONTACT NO.", "CONTACT", "Contact No", "Phone", "Contact", "Mobile");
        data.CustomerPhone = NormalizePhone(customerPhone);
        
        // ✅ Secondary contact (store in remarks if present)
        var secondaryContact = ExtractRightSideValueByLabels(worksheet, 
            "SECONDARY CONTACT NO", "SECONDARY CONTACT NO.", "Secondary Contact No", "Secondary Contact");
        if (!string.IsNullOrEmpty(secondaryContact))
        {
            var normalizedSecondary = NormalizePhone(secondaryContact);
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"Secondary Contact: {normalizedSecondary}"
                : $"{data.Remarks}\nSecondary Contact: {normalizedSecondary}";
        }
        
        data.CustomerEmail = ExtractRightSideValueByLabels(worksheet, 
            "EMAIL", "Email", "E-mail", "Email Address")
            ?? ExtractByLabel(worksheet, "CustomerEmail");
        
        // ✅ Installation Address (Service Address)
        data.ServiceAddress = ExtractRightSideValueByLabels(worksheet, 
            "SERVICE ADDRESS", "SERVICE ADD...", "Service Address", 
            "INSTALLATION ADDRESS", "Installation Address", "Address")
            ?? ExtractByLabel(worksheet, "ServiceAddress");

        // ✅ NIS CODE (store in remarks if present)
        var nisCode = ExtractRightSideValueByLabels(worksheet, 
            "NIS CODE", "NIS CODE:", "NIS", "NIS Code");
        if (!string.IsNullOrEmpty(nisCode))
        {
            data.Remarks = string.IsNullOrEmpty(data.Remarks)
                ? $"NIS Code: {nisCode}"
                : $"{data.Remarks}\nNIS Code: {nisCode}";
        }

        // ✅ Appointment Date & Time - Parse "26 Nov 2025 10:00:00" format (similar to Celcom)
        var appointmentDateTimeStr = ExtractRightSideValueByLabels(worksheet, 
            "APPOINT. DATE & TIME", "APPOINT DATE & TIME", "APPOINTMENT DATE & TIME",
            "PREFERRED DATE", "Preferred Date", "APPOINTMENT DATE", "Appointment Date", "Date");
        
        // Try to get Preferred Slot separately if appointment date/time not found
        if (string.IsNullOrEmpty(appointmentDateTimeStr))
        {
            var preferredDate = ExtractRightSideValueByLabels(worksheet, 
                "PREFERRED DATE", "Preferred Date", "APPOINTMENT DATE", "Appointment Date", "Date");
            
            var preferredSlot = ExtractRightSideValueByLabels(worksheet, 
                "PREFERRED SLOT", "Preferred Slot", "APPOINTMENT TIME", "Appointment Time", "Time");
            
            // Combine date and slot if both present
            if (!string.IsNullOrEmpty(preferredDate) && !string.IsNullOrEmpty(preferredSlot))
            {
                appointmentDateTimeStr = $"{preferredDate} {preferredSlot}";
            }
            else if (!string.IsNullOrEmpty(preferredDate))
            {
                appointmentDateTimeStr = preferredDate;
            }
        }
        
        // Parse using Celcom-style date format parser (same format: "26 Nov 2025 10:00:00")
        data.AppointmentDateTime = ParseCelcomAppointmentDateTime(appointmentDateTimeStr);

        // ✅ Package / Bandwidth - Combined field (similar to Celcom): "DigiWholesale Broadband Residential 300Mbps - 24 Months"
        var packageBandwidth = ExtractRightSideValueByLabels(worksheet, 
            "PACKAGE / BANDWIDTH", "PACKAGE / BANDWIDTH:", "PACKAGE", "Package / Bandwidth");
        
        if (!string.IsNullOrEmpty(packageBandwidth))
        {
            ParseDigiPackageBandwidth(packageBandwidth, data);
        }
        else
        {
            // Fallback to separate fields if combined field not found
            data.Bandwidth = ExtractRightSideValueByLabels(worksheet, 
                "BANDWIDTH", "Bandwidth", "Speed", "Internet Speed")
                ?? ExtractByLabel(worksheet, "Bandwidth");

            data.PackageName = ExtractRightSideValueByLabels(worksheet, 
                "PACKAGE", "Package", "Package Name", "PACKAGE NAME")
                ?? ExtractByLabel(worksheet, "PackageName");
        }

        // ✅ LOGIN ID - Format: "owszeyun@digi.home"
        data.Username = ExtractRightSideValueByLabels(worksheet, 
            "LOGIN ID", "LOGIN ID:", "Login ID", "ONU USERNAME", "Onu Username", "Username")
            ?? ExtractByLabel(worksheet, "Username");
        
        // ✅ Password
        data.Password = ExtractRightSideValueByLabels(worksheet, 
            "PASSWORD", "Password", "PWD")
            ?? ExtractByLabel(worksheet, "Password");

        // ✅ ONU Password (if present)
        data.OnuPassword = ExtractRightSideValueByLabels(worksheet, 
            "ONU PASSWORD", "ONU PASSWORD:", "ONU Password", "Password ONU")
            ?? ExtractByLabel(worksheet, "OnuPassword");

        // ✅ Network IP addresses (may be blank)
        data.InternetWanIp = ExtractRightSideValueByLabels(worksheet, 
            "WAN IP", "WAN IP:", "Wan IP");
        data.InternetLanIp = ExtractRightSideValueByLabels(worksheet, 
            "LAN IP", "LAN IP:", "Lan IP");
        data.InternetGateway = ExtractRightSideValueByLabels(worksheet, 
            "GATEWAY", "Gateway", "GATEWAY:");
        data.InternetSubnetMask = ExtractRightSideValueByLabels(worksheet, 
            "SUBNET MASK", "SUBNET MASK:", "Subnet Mask");

        // ✅ Splitter Location (if present)
        data.SplitterLocation = ExtractRightSideValueByLabels(worksheet, 
            "SPLITTER LOCATION", "Splitter Location", "SPLITTER", "Splitter");

        // ✅ Remarks (if present)
        data.Remarks = ExtractRightSideValueByLabels(worksheet, 
            "REMARKS", "Remarks", "Note", "Notes", "Comments");

        // Calculate confidence
        data.ConfidenceScore = CalculateConfidenceScore(data, "ACTIVATION");

        _logger.LogInformation("✅ TIME-Digi Activation parsed: ServiceId={ServiceId}, Customer={Customer}, Date={Date}, Confidence={Confidence}",
            data.ServiceId, data.CustomerName, data.AppointmentDateTime, data.ConfidenceScore);

        return data;
    }

    #endregion

    #region Material Extraction (FIXED)

    /// <summary>
    /// Extract materials from TIME Excel format
    /// Captures DECT phones, ONU devices, WiFi boosters, and asset codes (CAE-xxx-xxxx)
    /// </summary>
    private List<ParsedOrderMaterialLine> ParseMaterials(IWorksheet worksheet)
    {
        var materials = new List<ParsedOrderMaterialLine>();

        // Column K (11) = Material names
        // Column M (13) = ADD checkbox or Quantity
        // Column O (15) = NOT PROVIDED checkbox
        const int colK = 11;
        const int colM = 13;
        const int colO = 15;

        // Parse materials with ADD checkbox (rows 33-51)
        for (int row = 33; row <= 51 && row <= worksheet.UsedRange.LastRow; row++)
        {
            var materialName = GetCellValue(worksheet, row, colK);

            if (!string.IsNullOrWhiteSpace(materialName))
            {
                // Extract asset code if present (e.g., "DECT PHONE CAE-000-0210")
                var assetCodeMatch = Regex.Match(materialName, @"CAE-\d{3}-\d{4}");
                var cleanName = materialName.Trim();
                var assetCode = assetCodeMatch.Success ? assetCodeMatch.Value : null;

                // NEW: Resolve IsRequired based on X, YES, No positions
                bool isRequired = ResolveMaterialIsRequired(worksheet, row, cleanName);
                
                // Determine ActionTag based on IsRequired
                string? actionTag = null;
                if (isRequired)
                {
                    actionTag = "ADD"; // Material must be provided
                }
                else
                {
                    // Check if there's an explicit NOT PROVIDED marker
                    var notProvidedMarker = GetCellValue(worksheet, row, colO);
                    if (notProvidedMarker?.Trim().Equals("X", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        actionTag = "NOT_PROVIDED";
                    }
                }

                // Only add material if we have a clear action or if it's required
                if (actionTag != null || isRequired)
                {
                    materials.Add(new ParsedOrderMaterialLine
                    {
                        Name = cleanName,
                        ActionTag = actionTag,
                        Notes = assetCode,
                        IsRequired = isRequired
                    });

                    _logger.LogInformation("✓ Material: {Name} [{Action}] IsRequired={IsRequired} {AssetCode}",
                        cleanName, actionTag ?? "NONE", isRequired, assetCode ?? "");
                }
            }
        }

        // Parse materials with quantities (rows 54-66)
        for (int row = 54; row <= 66 && row <= worksheet.UsedRange.LastRow; row++)
        {
            var materialName = GetCellValue(worksheet, row, colK);
            var quantityText = GetCellValue(worksheet, row, colM);

            if (!string.IsNullOrWhiteSpace(materialName) && !string.IsNullOrWhiteSpace(quantityText))
            {
                // NEW: Resolve IsRequired for quantity-based materials too
                bool isRequired = ResolveMaterialIsRequired(worksheet, row, materialName.Trim());
                
                var line = new ParsedOrderMaterialLine
                {
                    Name = materialName.Trim(),
                    Quantity = ParseQuantity(quantityText),
                    UnitOfMeasure = InferUnit(quantityText),
                    Notes = quantityText,
                    IsRequired = isRequired
                };
                materials.Add(line);
                
                _logger.LogInformation("✓ Material with quantity: {Name} x{Quantity} {Unit} IsRequired={IsRequired}",
                    materialName, line.Quantity, line.UnitOfMeasure ?? "", isRequired);
            }
        }

        _logger.LogInformation("✓ Total materials extracted: {Count}", materials.Count);
        return materials;
    }

    private decimal? ParseQuantity(string quantityText)
    {
        if (string.IsNullOrWhiteSpace(quantityText)) return null;
        
        // Extract first number from text
        var match = Regex.Match(quantityText, @"\d+(\.\d+)?");
        if (match.Success && decimal.TryParse(match.Value, out var quantity))
        {
            return quantity;
        }
        
        return null;
    }

    private string? InferUnit(string quantityText)
    {
        if (quantityText?.Contains("unit", StringComparison.OrdinalIgnoreCase) == true) return "unit";
        if (quantityText?.Contains("pc", StringComparison.OrdinalIgnoreCase) == true) return "pc";
        if (quantityText?.Contains("set", StringComparison.OrdinalIgnoreCase) == true) return "set";
        if (quantityText?.Contains("meter", StringComparison.OrdinalIgnoreCase) == true) return "m";
        return "unit"; // default
    }

    /// <summary>
    /// Extract materials from DataTable (ExcelDataReader format)
    /// </summary>
    private List<ParsedOrderMaterialLine> ParseMaterialsFromDataTable(DataTable dataTable)
    {
        var materials = new List<ParsedOrderMaterialLine>();

        // Column K (index 9) = Material names (0-indexed: Col9)
        // Column M (index 12) = ADD checkbox or Quantity (0-indexed: Col12)
        // Column O (index 14) = NOT PROVIDED checkbox (0-indexed: Col14)
        // Note: ExcelDataReader maps Excel columns differently - materials are in Col9, not Col10
        const int colK = 9; // Column K (0-indexed) - actually Col9 in DataTable
        const int colM = 12; // Column M (0-indexed)
        const int colO = 14; // Column O (0-indexed)

        // Parse materials with ADD checkbox (rows 31-51, 0-indexed = 30-50)
        // Note: Materials start at Excel row 31 (0-indexed = row 30) based on actual Excel structure
        for (int row = 30; row <= 50 && row < dataTable.Rows.Count; row++)
        {
            if (colK >= dataTable.Columns.Count) continue;
            
            var materialName = dataTable.Rows[row][colK]?.ToString()?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(materialName))
            {
                // Extract asset code if present (e.g., "DECT PHONE CAE-000-0210")
                var assetCodeMatch = Regex.Match(materialName, @"CAE-\d{3}-\d{4}");
                var cleanName = materialName.Trim();
                var assetCode = assetCodeMatch.Success ? assetCodeMatch.Value : null;

                // NEW: Resolve IsRequired based on X, YES, No positions
                bool isRequired = ResolveMaterialIsRequired(dataTable, row, cleanName);
                
                // Determine ActionTag based on IsRequired
                string? actionTag = null;
                if (isRequired)
                {
                    actionTag = "ADD"; // Material must be provided
                }
                else
                {
                    // Check if there's an explicit NOT PROVIDED marker
                    if (colO < dataTable.Columns.Count)
                    {
                        var notProvidedMarker = dataTable.Rows[row][colO]?.ToString()?.Trim();
                        if (notProvidedMarker?.Equals("X", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            actionTag = "NOT_PROVIDED";
                        }
                    }
                }

                // Add all materials found (even if not required) - let the IsRequired flag indicate status
                materials.Add(new ParsedOrderMaterialLine
                {
                    Name = cleanName,
                    ActionTag = actionTag,
                    Notes = assetCode,
                    IsRequired = isRequired
                });

                _logger.LogInformation("✓ Material: {Name} [{Action}] IsRequired={IsRequired} {AssetCode}",
                    cleanName, actionTag ?? "NONE", isRequired, assetCode ?? "");
            }
        }

        // Parse materials with quantities (rows 54-66, 0-indexed = 53-65)
        for (int row = 53; row <= 65 && row < dataTable.Rows.Count; row++)
        {
            if (colK >= dataTable.Columns.Count || colM >= dataTable.Columns.Count) continue;
            
            var materialName = dataTable.Rows[row][colK]?.ToString()?.Trim() ?? "";
            var quantityText = dataTable.Rows[row][colM]?.ToString()?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(materialName) && !string.IsNullOrWhiteSpace(quantityText))
            {
                // NEW: Resolve IsRequired for quantity-based materials too
                bool isRequired = ResolveMaterialIsRequired(dataTable, row, materialName.Trim());
                
                var line = new ParsedOrderMaterialLine
                {
                    Name = materialName.Trim(),
                    Quantity = ParseQuantity(quantityText),
                    UnitOfMeasure = InferUnit(quantityText),
                    Notes = quantityText,
                    IsRequired = isRequired
                };
                materials.Add(line);
                
                _logger.LogInformation("✓ Material with quantity: {Name} x{Quantity} {Unit} IsRequired={IsRequired}",
                    materialName, line.Quantity, line.UnitOfMeasure ?? "", isRequired);
            }
        }

        _logger.LogInformation("✓ Total materials extracted from DataTable: {Count}", materials.Count);
        return materials;
    }

    /// <summary>
    /// Resolves whether a material should be provided based on X, YES, and No positions
    /// Logic: Find leftmost X, scan right, first YES/No determines IsRequired
    /// </summary>
    private bool ResolveMaterialIsRequired(IWorksheet worksheet, int row, string materialName)
    {
        try
        {
            if (row < 1 || row > worksheet.UsedRange.LastRow) return false;
            
            int lastCol = worksheet.UsedRange.LastColumn;
            
            // Step 1: Find the leftmost X in the row
            int? xColumn = null;
            for (int col = 1; col <= lastCol; col++)
            {
                var cellValue = GetCellValue(worksheet, row, col);
                if (cellValue?.Trim().Equals("X", StringComparison.OrdinalIgnoreCase) == true)
                {
                    xColumn = col;
                    break; // Use leftmost X
                }
            }
            
            // Step 2: If no X found, material is not required
            if (!xColumn.HasValue)
            {
                _logger.LogWarning("No X marker found for material '{Material}' at row {Row}. Treating as NOT required.", 
                    materialName, row);
                return false;
            }
            
            // Step 3: Scan to the right from X column, find first YES or No
            for (int col = xColumn.Value + 1; col <= lastCol; col++)
            {
                var cellValue = GetCellValue(worksheet, row, col);
                if (string.IsNullOrWhiteSpace(cellValue)) continue;
                
                var trimmed = cellValue.Trim();
                var upper = trimmed.ToUpperInvariant();
                
                // Check for YES
                if (upper == "YES")
                {
                    _logger.LogInformation("✓ Material '{Material}' at R{Row}: X at C{XCol}, YES at C{YesCol} → IsRequired=true", 
                        materialName, row, xColumn.Value, col);
                    return true;
                }
                
                // Check for No (case-insensitive, but must be exact match)
                if (upper == "NO" || trimmed.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("✓ Material '{Material}' at R{Row}: X at C{XCol}, No at C{NoCol} → IsRequired=false", 
                        materialName, row, xColumn.Value, col);
                    return false;
                }
            }
            
            // Step 4: X found but no YES/No encountered → default to false
            _logger.LogWarning("X found for material '{Material}' at R{Row}C{XCol}, but no YES/No found to the right. Treating as NOT required.", 
                materialName, row, xColumn.Value);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving IsRequired for material '{Material}' at row {Row}", materialName, row);
            return false; // Default to false on error
        }
    }

    /// <summary>
    /// Resolves whether a material should be provided based on X, YES, and No positions (DataTable version)
    /// Logic: Find leftmost X, scan right, first YES/No determines IsRequired
    /// </summary>
    private bool ResolveMaterialIsRequired(DataTable dataTable, int rowIndex, string materialName)
    {
        try
        {
            if (rowIndex < 0 || rowIndex >= dataTable.Rows.Count) return false;
            
            int lastCol = dataTable.Columns.Count - 1;
            
            // Step 1: Find the leftmost X in the row (0-indexed)
            // Based on Excel structure: X can be in Column L (index 11) or Column O (index 14)
            int? xColumn = null;
            for (int col = 0; col <= lastCol; col++)
            {
                var cellValue = dataTable.Rows[rowIndex][col]?.ToString()?.Trim();
                if (cellValue?.Equals("X", StringComparison.OrdinalIgnoreCase) == true)
                {
                    xColumn = col;
                    _logger.LogDebug("Found X marker at column {Col} (1-indexed: C{Col1Indexed}) for material '{Material}' at row {Row}", 
                        col, col + 1, materialName, rowIndex + 1);
                    break; // Use leftmost X
                }
            }
            
            // Step 2: If no X found, material is not required
            if (!xColumn.HasValue)
            {
                _logger.LogWarning("No X marker found for material '{Material}' at row {Row}. Treating as NOT required.", 
                    materialName, rowIndex + 1);
                return false;
            }
            
            // Step 3: Scan to the right from X column, find first YES or No
            for (int col = xColumn.Value + 1; col <= lastCol; col++)
            {
                var cellValue = dataTable.Rows[rowIndex][col]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(cellValue)) continue;
                
                var upper = cellValue.ToUpperInvariant();
                
                // Check for YES
                if (upper == "YES")
                {
                    _logger.LogInformation("✓ Material '{Material}' at R{Row}: X at C{XCol}, YES at C{YesCol} → IsRequired=true", 
                        materialName, rowIndex + 1, xColumn.Value + 1, col + 1);
                    return true;
                }
                
                // Check for No (case-insensitive, but must be exact match)
                if (upper == "NO" || cellValue.Equals("No", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("✓ Material '{Material}' at R{Row}: X at C{XCol}, No at C{NoCol} → IsRequired=false", 
                        materialName, rowIndex + 1, xColumn.Value + 1, col + 1);
                    return false;
                }
            }
            
            // Step 4: X found but no YES/No encountered → default to false
            _logger.LogWarning("X found for material '{Material}' at R{Row}C{XCol}, but no YES/No found to the right. Treating as NOT required.", 
                materialName, rowIndex + 1, xColumn.Value + 1);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving IsRequired for material '{Material}' at row {Row}", materialName, rowIndex + 1);
            return false; // Default to false on error
        }
    }

    #endregion

    #region Generic Label-Based Extraction Helpers

    /// <summary>
    /// Generic helper to extract value from Excel based on label cell (IWorksheet version).
    /// Finds the label cell (case-insensitive, trimmed), then scans rightward on the same row
    /// to find the first non-empty cell value.
    /// 
    /// This is a future-proof, column-agnostic method that works for ANY label text,
    /// regardless of where columns are positioned in the Excel template.
    /// 
    /// Usage examples:
    ///   var address = ExtractRightSideValueByLabel(worksheet, "Service Address");
    ///   var customerName = ExtractRightSideValueByLabel(worksheet, "Customer Name");
    ///   var billingAddress = ExtractRightSideValueByLabel(worksheet, "Billing Address");
    ///   var installationDate = ExtractRightSideValueByLabel(worksheet, "Installation Date");
    /// </summary>
    /// <param name="worksheet">Syncfusion IWorksheet to search</param>
    /// <param name="labelText">The label text to find (case-insensitive, trimmed)</param>
    /// <returns>First non-empty value to the right of the label, or null if not found</returns>
    private string? ExtractRightSideValueByLabel(IWorksheet worksheet, string labelText)
    {
        if (worksheet == null || string.IsNullOrWhiteSpace(labelText))
            return null;

        int lastRow = worksheet.UsedRange.LastRow;
        int lastCol = worksheet.UsedRange.LastColumn;

        // Step 1: Search entire worksheet for the label (normalized exact or contains match)
        for (int row = 1; row <= lastRow; row++)
        {
            for (int col = 1; col <= lastCol; col++)
            {
                try
                {
                    var cell = worksheet.Range[row, col];
                    var cellValue = cell?.Text?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    // Normalized match: exact or safe contains (e.g. "Customer Name (as per IC)" matches "Customer Name")
                    if (ExcelLabelNormalizer.MatchesAny(cellValue, new[] { labelText }))
                    {
                        // Step 2: Found label at [row, col], now scan rightward on the same row
                        for (int rightCol = col + 1; rightCol <= lastCol; rightCol++)
                        {
                            try
                            {
                                var rightCell = worksheet.Range[row, rightCol];
                                var rightValue = rightCell?.Text?.Trim();
                                
                                // Skip empty/whitespace-only cells
                                if (string.IsNullOrWhiteSpace(rightValue)) continue;
                                
                                // Found first non-empty cell to the right - return it
                                _logger.LogDebug("✓ Found label '{Label}' at R{Row}C{Col}, value at C{ValueCol}: '{Value}'", 
                                    labelText, row, col, rightCol, rightValue);
                                return rightValue;
                            }
                            catch (Exception ex)
                            {
                                // Handle merged cells or out-of-range gracefully
                                _logger.LogDebug("Error reading cell R{Row}C{Col}: {Error}", row, rightCol, ex.Message);
                                continue;
                            }
                        }
                        
                        // Label found but all cells to the right are empty
                        _logger.LogWarning("Label '{Label}' found at R{Row}C{Col}, but no value found to the right on the same row", 
                            labelText, row, col);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Handle merged cells or other issues gracefully
                    _logger.LogDebug("Error reading cell R{Row}C{Col}: {Error}", row, col, ex.Message);
                    continue;
                }
            }
        }

        // Label not found anywhere
        _logger.LogDebug("Label '{Label}' not found in worksheet", labelText);
        return null;
    }

    /// <summary>
    /// Extract Service ID with normalized header detection (flexible) but strict value matching.
    /// Phase 1: Normalized header detection - finds "Service ID", "Service-ID", "SERVICE ID", etc.
    /// Phase 2: Exact value extraction - uses the value exactly as found (no normalization).
    /// 
    /// This follows the documented strategy:
    /// - Header detection = flexible (normalized matching)
    /// - Value matching = strict (exact match for database lookup)
    /// </summary>
    private string? ExtractServiceIdWithNormalizedHeader(IWorksheet worksheet)
    {
        if (worksheet == null)
            return null;

        // Phase 1: Normalized header detection (flexible)
        // Normalize the label variations we're looking for
        var normalizedLabels = new[]
        {
            "Service ID",
            "Service-ID",
            "SERVICE ID",
            "Service  ID", // Multiple spaces
            "Service_ID",
            "service id",
            "SERVICE-ID",
            "ServiceID"
        };

        int lastRow = worksheet.UsedRange.LastRow;
        int lastCol = worksheet.UsedRange.LastColumn;

        // Normalize function: lowercase, trim, collapse spaces, convert -/_ to spaces
        string NormalizeLabel(string label)
        {
            return label.ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ")
                .Trim();
        }

        // Search for normalized label match
        for (int row = 1; row <= lastRow; row++)
        {
            for (int col = 1; col <= lastCol; col++)
            {
                try
                {
                    var cell = worksheet.Range[row, col];
                    var cellValue = cell?.Text?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    // Normalize the cell value for comparison
                    var normalizedCell = NormalizeLabel(cellValue);

                    // Check if normalized cell matches any normalized label
                    foreach (var label in normalizedLabels)
                    {
                        var normalizedLabel = NormalizeLabel(label);
                        
                        if (normalizedCell == normalizedLabel)
                        {
                            // Phase 2: Extract value exactly as-is (strict, no normalization)
                            // Scan rightward for the first non-empty value
                            for (int rightCol = col + 1; rightCol <= lastCol; rightCol++)
                            {
                                try
                                {
                                    var rightCell = worksheet.Range[row, rightCol];
                                    var rightValue = rightCell?.Text?.Trim();
                                    
                                    if (string.IsNullOrWhiteSpace(rightValue)) continue;
                                    
                                    // Return value exactly as found (strict - no normalization)
                                    _logger.LogInformation("✓ Service ID header found at R{Row}C{Col} (normalized match: '{Label}'), value extracted (strict): '{Value}'", 
                                        row, col, cellValue, rightValue);
                                    return rightValue; // Exact value, no normalization
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug("Error reading Service ID value cell R{Row}C{Col}: {Error}", row, rightCol, ex.Message);
                                    continue;
                                }
                            }
                            
                            _logger.LogWarning("Service ID header found at R{Row}C{Col}, but no value found to the right", row, col);
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Error reading cell R{Row}C{Col}: {Error}", row, col, ex.Message);
                    continue;
                }
            }
        }

        _logger.LogDebug("Service ID header not found in worksheet (tried normalized matching)");
        return null;
    }

    /// <summary>
    /// Generic helper to extract value from DataTable based on label cell (DataTable version).
    /// Finds the label cell (case-insensitive, trimmed), then scans rightward on the same row
    /// to find the first non-empty cell value.
    /// 
    /// This is a future-proof, column-agnostic method that works for ANY label text,
    /// regardless of where columns are positioned in the Excel template.
    /// 
    /// Usage examples:
    ///   var address = ExtractRightSideValueByLabel(dataTable, "Service Address");
    ///   var customerName = ExtractRightSideValueByLabel(dataTable, "Customer Name");
    ///   var billingAddress = ExtractRightSideValueByLabel(dataTable, "Billing Address");
    ///   var installationDate = ExtractRightSideValueByLabel(dataTable, "Installation Date");
    /// </summary>
    /// <param name="dataTable">DataTable to search (from ExcelDataReader)</param>
    /// <param name="labelText">The label text to find (case-insensitive, trimmed)</param>
    /// <returns>First non-empty value to the right of the label, or null if not found</returns>
    private string? ExtractRightSideValueByLabel(DataTable dataTable, string labelText)
    {
        if (dataTable == null || string.IsNullOrWhiteSpace(labelText))
            return null;

        var labelUpper = labelText.Trim().ToUpperInvariant();
        int lastRow = dataTable.Rows.Count;
        int lastCol = dataTable.Columns.Count;

        // Step 1: Search entire DataTable for the label (exact match, case-insensitive, trimmed)
        for (int row = 0; row < lastRow; row++)
        {
            for (int col = 0; col < lastCol; col++)
            {
                try
                {
                    var cellValue = dataTable.Rows[row][col]?.ToString()?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    // Clean cell value: remove trailing colons, periods, spaces
                    var cellUpper = cellValue.ToUpperInvariant();
                    var cleanCell = cellUpper.TrimEnd(':', ' ', '.');

                    // Exact match (case-insensitive, trimmed)
                    if (cleanCell == labelUpper)
                    {
                        // Step 2: Found label at [row, col], now scan rightward on the same row
                        for (int rightCol = col + 1; rightCol < lastCol; rightCol++)
                        {
                            try
                            {
                                var rightValue = dataTable.Rows[row][rightCol]?.ToString()?.Trim();
                                
                                // Skip empty/whitespace-only cells
                                if (string.IsNullOrWhiteSpace(rightValue)) continue;
                                
                                // Found first non-empty cell to the right - return it
                                _logger.LogDebug("✓ Found label '{Label}' at R{Row}C{Col}, value at C{ValueCol}: '{Value}'", 
                                    labelText, row + 1, col + 1, rightCol + 1, rightValue);
                                return rightValue;
                            }
                            catch (Exception ex)
                            {
                                // Handle out-of-range gracefully
                                _logger.LogDebug("Error reading cell R{Row}C{Col}: {Error}", row + 1, rightCol + 1, ex.Message);
                                continue;
                            }
                        }
                        
                        // Label found but all cells to the right are empty
                        _logger.LogWarning("Label '{Label}' found at R{Row}C{Col}, but no value found to the right on the same row", 
                            labelText, row + 1, col + 1);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    // Handle errors gracefully
                    _logger.LogDebug("Error reading cell R{Row}C{Col}: {Error}", row + 1, col + 1, ex.Message);
                    continue;
                }
            }
        }

        // Label not found anywhere
        _logger.LogDebug("Label '{Label}' not found in DataTable", labelText);
        return null;
    }

    /// <summary>
    /// Convenience wrapper: Tries multiple label variations and returns first match.
    /// Useful when the same field might appear with different label text in different templates.
    /// 
    /// Usage:
    ///   data.CustomerName = ExtractRightSideValueByLabels(dataTable, "Customer Name", "CUSTOMER NAME", "Name");
    ///   data.ServiceAddress = ExtractRightSideValueByLabels(dataTable, "Service Address", "SERVICE ADDRESS", "Installation Address");
    /// </summary>
    /// <param name="dataTable">DataTable to search</param>
    /// <param name="labelVariations">Array of label text variations to try</param>
    /// <returns>First non-empty value found, or null if none match</returns>
    private string? ExtractRightSideValueByLabels(DataTable dataTable, params string[] labelVariations)
    {
        if (labelVariations == null || labelVariations.Length == 0)
            return null;

        foreach (var label in labelVariations)
        {
            var value = ExtractRightSideValueByLabel(dataTable, label);
            if (!string.IsNullOrWhiteSpace(value))
            {
                _logger.LogDebug("✓ Found value using label variation '{Label}': '{Value}'", label, value);
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Service ID with normalized header detection (flexible) but strict value matching (DataTable version).
    /// Phase 1: Normalized header detection - finds "Service ID", "Service-ID", "SERVICE ID", etc.
    /// Phase 2: Exact value extraction - uses the value exactly as found (no normalization).
    /// 
    /// This follows the documented strategy:
    /// - Header detection = flexible (normalized matching)
    /// - Value matching = strict (exact match for database lookup)
    /// </summary>
    private string? ExtractServiceIdWithNormalizedHeader(DataTable dataTable)
    {
        if (dataTable == null)
            return null;

        // Phase 1: Normalized header detection (flexible)
        // Normalize the label variations we're looking for
        var normalizedLabels = new[]
        {
            "Service ID",
            "Service-ID",
            "SERVICE ID",
            "Service  ID", // Multiple spaces
            "Service_ID",
            "service id",
            "SERVICE-ID",
            "ServiceID"
        };

        int lastRow = dataTable.Rows.Count;
        int lastCol = dataTable.Columns.Count;

        // Normalize function: lowercase, trim, collapse spaces, convert -/_ to spaces
        string NormalizeLabel(string label)
        {
            return label.ToLowerInvariant()
                .Replace("-", " ")
                .Replace("_", " ")
                .Trim();
        }

        // Search for normalized label match
        for (int row = 0; row < lastRow; row++)
        {
            for (int col = 0; col < lastCol; col++)
            {
                try
                {
                    var cellValue = dataTable.Rows[row][col]?.ToString()?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                    // Normalize the cell value for comparison
                    var normalizedCell = NormalizeLabel(cellValue);

                    // Check if normalized cell matches any normalized label
                    foreach (var label in normalizedLabels)
                    {
                        var normalizedLabel = NormalizeLabel(label);
                        
                        if (normalizedCell == normalizedLabel)
                        {
                            // Phase 2: Extract value exactly as-is (strict, no normalization)
                            // Scan rightward for the first non-empty value
                            for (int rightCol = col + 1; rightCol < lastCol; rightCol++)
                            {
                                try
                                {
                                    var rightValue = dataTable.Rows[row][rightCol]?.ToString()?.Trim();
                                    
                                    if (string.IsNullOrWhiteSpace(rightValue)) continue;
                                    
                                    // Return value exactly as found (strict - no normalization)
                                    _logger.LogInformation("✓ Service ID header found at R{Row}C{Col} (normalized match: '{Label}'), value extracted (strict): '{Value}'", 
                                        row + 1, col + 1, cellValue, rightValue);
                                    return rightValue; // Exact value, no normalization
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug("Error reading Service ID value cell R{Row}C{Col}: {Error}", row + 1, rightCol + 1, ex.Message);
                                    continue;
                                }
                            }
                            
                            _logger.LogWarning("Service ID header found at R{Row}C{Col}, but no value found to the right", row + 1, col + 1);
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Error reading cell R{Row}C{Col}: {Error}", row + 1, col + 1, ex.Message);
                    continue;
                }
            }
        }

        _logger.LogDebug("Service ID header not found in DataTable (tried normalized matching)");
        return null;
    }

    /// <summary>
    /// Convenience wrapper: Tries multiple label variations and returns first match (IWorksheet version).
    /// </summary>
    private string? ExtractRightSideValueByLabels(IWorksheet worksheet, params string[] labelVariations)
    {
        if (labelVariations == null || labelVariations.Length == 0)
            return null;

        foreach (var label in labelVariations)
        {
            var value = ExtractRightSideValueByLabel(worksheet, label);
            if (!string.IsNullOrWhiteSpace(value))
            {
                _logger.LogDebug("✓ Found value using label variation '{Label}': '{Value}'", label, value);
                return value;
            }
        }

        return null;
    }

    #endregion

    #region Exception Handling Helpers

    /// <summary>
    /// Extract detailed error information from exceptions, including Syncfusion-specific details
    /// Enhanced with diagnostic mode to identify root causes
    /// </summary>
    private string ExtractExceptionDetails(Exception ex)
    {
        var details = new List<string>();
        
        // Get main exception message
        if (!string.IsNullOrWhiteSpace(ex.Message) && ex.Message != "Exception of type 'System.Exception' was thrown.")
        {
            details.Add(ex.Message);
        }
        else
        {
            details.Add($"{ex.GetType().FullName} occurred");
        }
        
        // DIAGNOSTIC: Check for common error patterns
        var fullExceptionText = ex.ToString().ToUpperInvariant();
        var diagnosticHints = new List<string>();
        
        // More specific password detection - exclude false positives from FAT/CompoundFile errors
        if ((fullExceptionText.Contains("PASSWORD REQUIRED") || 
             fullExceptionText.Contains("PASSWORD PROTECTED") ||
             fullExceptionText.Contains("FILE IS PROTECTED")) &&
            !fullExceptionText.Contains("FAT.GETSTREAM") &&
            !fullExceptionText.Contains("COMPOUNDFILE"))
        {
            diagnosticHints.Add("🔒 PASSWORD PROTECTED: File is password protected");
        }
        
        // Detect file structure errors
        if (fullExceptionText.Contains("FAT.GETSTREAM") || 
            fullExceptionText.Contains("COMPOUNDFILE") ||
            fullExceptionText.Contains("ENTRYSTREAM"))
        {
            diagnosticHints.Add("💥 FILE STRUCTURE ERROR: File has corrupted or non-standard OLE2 structure (FAT/CompoundFile error)");
        }
        
        if (fullExceptionText.Contains("CORRUPT") || fullExceptionText.Contains("INVALID") || fullExceptionText.Contains("FORMAT"))
        {
            diagnosticHints.Add("💥 FILE CORRUPTION: File may be corrupted or in unsupported format");
        }
        
        if (fullExceptionText.Contains("ENCODING") || fullExceptionText.Contains("CODEPAGE") || fullExceptionText.Contains("CHARACTER"))
        {
            diagnosticHints.Add("🔤 ENCODING ISSUE: Encoding problem detected - CodePagesEncodingProvider may not be registered");
        }
        
        if (fullExceptionText.Contains("STREAM") && (fullExceptionText.Contains("EMPTY") || fullExceptionText.Contains("NULL") || fullExceptionText.Contains("ZERO")))
        {
            diagnosticHints.Add("📦 STREAM ISSUE: File stream is empty or unreadable");
        }
        
        if (fullExceptionText.Contains("NOT SUPPORTED") || fullExceptionText.Contains("UNSUPPORTED"))
        {
            diagnosticHints.Add("❌ UNSUPPORTED FORMAT: File format is not supported by Syncfusion");
        }
        
        if (diagnosticHints.Any())
        {
            details.AddRange(diagnosticHints);
        }
        
        // Try to extract Syncfusion-specific error information
        try
        {
            var props = ex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (prop.Name.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Contains("Code", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Contains("HResult", StringComparison.OrdinalIgnoreCase))
                {
                    var value = prop.GetValue(ex);
                    if (value != null)
                    {
                        details.Add($"{prop.Name}: {value}");
                    }
                }
            }
        }
        catch
        {
            // Ignore reflection errors
        }
        
        // Get inner exception details with full stack trace
        if (ex.InnerException != null)
        {
            var innerDetails = ExtractExceptionDetails(ex.InnerException);
            if (!string.IsNullOrWhiteSpace(innerDetails))
            {
                details.Add($"Inner Exception: {innerDetails}");
            }
            
            // Also log inner exception stack trace if available
            if (!string.IsNullOrWhiteSpace(ex.InnerException.StackTrace))
            {
                var firstStackTraceLine = ex.InnerException.StackTrace.Split('\n').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(firstStackTraceLine))
                {
                    details.Add($"Inner Stack: {firstStackTraceLine}");
                }
            }
        }
        
        // Add stack trace information
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            var relevantStackTrace = ex.StackTrace.Split('\n')
                .Where(line => line.Contains("Syncfusion") || line.Contains("Workbook") || line.Contains("Open"))
                .Take(3)
                .Select(line => line.Trim());
            
            if (relevantStackTrace.Any())
            {
                details.Add($"Stack: {string.Join(" → ", relevantStackTrace)}");
            }
        }
        
        return string.Join(" | ", details);
    }
    
    /// <summary>
    /// Diagnostic method to validate file before parsing
    /// </summary>
    private DiagnosticResult PerformFileDiagnostics(byte[] fileBytes, string fileName, string extension)
    {
        var result = new DiagnosticResult();
        
        // Check 1: File size
        if (fileBytes == null || fileBytes.Length == 0)
        {
            result.Issues.Add("❌ File is empty (0 bytes)");
            result.IsValid = false;
            return result;
        }
        
        result.FileSize = fileBytes.Length;
        _logger.LogInformation("File diagnostic: Size = {Size} bytes", fileBytes.Length);
        
        // Check 2: File header (magic bytes)
        if (fileBytes.Length >= 8)
        {
            var header = BitConverter.ToString(fileBytes, 0, 8);
            result.FileHeader = header;
            
            // Excel 97-2003 (.xls) should start with D0 CF 11 E0 A1 B1 1A E1 (OLE2 format)
            // Excel 2007+ (.xlsx) should start with 50 4B 03 04 (ZIP format)
            if (extension == ".xls")
            {
                var expectedHeader = "D0-CF-11-E0";
                if (!header.StartsWith(expectedHeader))
                {
                    result.Issues.Add($"⚠️ File header mismatch: Expected OLE2 format (starts with {expectedHeader}), got {header}");
                    result.IsValid = false;
                }
                else
                {
                    result.Issues.Add("✅ File header matches Excel 97-2003 format");
                }
            }
            else if (extension == ".xlsx" || extension == ".xlsm")
            {
                var expectedHeader = "50-4B-03-04";
                if (!header.StartsWith(expectedHeader))
                {
                    result.Issues.Add($"⚠️ File header mismatch: Expected ZIP format (starts with {expectedHeader}), got {header}");
                    result.IsValid = false;
                }
                else
                {
                    result.Issues.Add("✅ File header matches Excel 2007+ format");
                }
            }
        }
        
        // Check 3: Encoding provider registration
        try
        {
            var encoding = Encoding.GetEncoding(1252); // Windows-1252 (common for .xls)
            result.Issues.Add("✅ CodePagesEncodingProvider is registered");
        }
        catch (NotSupportedException)
        {
            result.Issues.Add("❌ CodePagesEncodingProvider is NOT registered - .xls files will fail!");
            result.IsValid = false;
        }
        
        return result;
    }
    
    /// <summary>
    /// Attempts to repair corrupted Excel file using Excel COM Interop (if Excel is installed)
    /// This is a fallback when Syncfusion fails with FAT/CompoundFile errors
    /// </summary>
    private async Task<byte[]?> TryRepairWithExcelComInteropAsync(byte[] fileBytes, string fileName, string extension)
    {
        try
        {
            // Check if we're on Windows (COM Interop requires Windows)
            if (!OperatingSystem.IsWindows())
            {
                _logger.LogDebug("Excel COM Interop repair skipped: Not running on Windows");
                return null;
            }
            
            // Create temporary file for COM Interop
            var tempInputPath = Path.Combine(Path.GetTempPath(), $"excel_repair_input_{Guid.NewGuid()}{extension}");
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"excel_repair_output_{Guid.NewGuid()}{extension}");
            
            try
            {
                // Write input file
                await File.WriteAllBytesAsync(tempInputPath, fileBytes);
                
                // Try to use Excel COM Interop to repair and save
                // This requires Microsoft.Office.Interop.Excel, which we'll add conditionally
                // For now, we'll use dynamic COM interop to avoid adding a dependency
                _logger.LogInformation("Attempting Excel COM Interop repair for {FileName}", fileName);
                
                Type? excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    _logger.LogDebug("Excel COM Interop not available: Excel.Application not found (Excel may not be installed)");
                    return null;
                }
                
                dynamic? excelApp = Activator.CreateInstance(excelType);
                if (excelApp == null)
                {
                    _logger.LogDebug("Failed to create Excel.Application COM object");
                    return null;
                }
                
                try
                {
                    excelApp.DisplayAlerts = false; // Suppress alerts
                    excelApp.Visible = false; // Run in background
                    
                    // Try to open with repair
                    dynamic? workbook = null;
                    try
                    {
                        // Attempt to open with repair option
                        workbook = excelApp.Workbooks.Open(
                            Filename: tempInputPath,
                            UpdateLinks: 0, // Don't update links
                            ReadOnly: false,
                            Format: 1, // Delimited
                            Delimiter: "",
                            Editable: true,
                            Notify: false,
                            Converter: 0,
                            AddToMru: false,
                            Local: false,
                            CorruptLoad: 2 // xlCorruptLoadRepairFile = 2 (attempt repair)
                        );
                    }
                    catch (Exception openEx)
                    {
                        _logger.LogWarning(openEx, "Excel COM Interop failed to open file for repair: {Error}", openEx.Message);
                        return null;
                    }
                    
                    if (workbook == null)
                    {
                        _logger.LogWarning("Excel COM Interop opened file but workbook is null");
                        return null;
                    }
                    
                    try
                    {
                        // Save as new file (this repairs the structure)
                        workbook.SaveAs(
                            Filename: tempOutputPath,
                            FileFormat: extension == ".xls" ? 43 : 51, // xlExcel8 = 43 (.xls), xlOpenXMLWorkbook = 51 (.xlsx)
                            Password: Type.Missing,
                            WriteResPassword: Type.Missing,
                            ReadOnlyRecommended: false,
                            CreateBackup: false,
                            AccessMode: 1, // xlShared
                            ConflictResolution: 2, // xlLocalSessionChanges
                            AddToMru: false,
                            TextCodepage: Type.Missing,
                            TextVisualLayout: Type.Missing,
                            Local: false
                        );
                        
                        // Read repaired file
                        var repairedBytes = await File.ReadAllBytesAsync(tempOutputPath);
                        _logger.LogInformation("✅ Excel COM Interop repair succeeded: {OriginalSize} → {RepairedSize} bytes", 
                            fileBytes.Length, repairedBytes.Length);
                        
                        return repairedBytes;
                    }
                    finally
                    {
                        try { workbook.Close(SaveChanges: false); } catch { }
                    }
                }
                finally
                {
                    try 
                    { 
                        excelApp.Quit();
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                    } 
                    catch { }
                }
            }
            finally
            {
                // Cleanup temp files
                try { if (File.Exists(tempInputPath)) File.Delete(tempInputPath); } catch { }
                try { if (File.Exists(tempOutputPath)) File.Delete(tempOutputPath); } catch { }
            }
        }
        catch (Exception ex)
        {
            // COM Interop is optional - log but don't fail
            _logger.LogDebug(ex, "Excel COM Interop repair failed (this is optional): {Error}", ex.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Fallback parser using ExcelDataReader (more tolerant of corrupted files)
    /// This is the original parser that worked well before Syncfusion
    /// </summary>
    private async Task<TimeExcelParseResult?> TryParseWithExcelDataReaderAsync(
        byte[] fileBytes, 
        string fileName, 
        string extension)
    {
        try
        {
            _logger.LogInformation("Attempting fallback parse with ExcelDataReader for {FileName}", fileName);
            
            using var stream = new MemoryStream(fileBytes, writable: false);
            
            // ExcelDataReader requires CodePages encoding for .xls files
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            IExcelDataReader? reader = null;
            
            // Create reader based on file extension
            if (extension == ".xls")
            {
                reader = ExcelReaderFactory.CreateBinaryReader(stream);
            }
            else if (extension == ".xlsx" || extension == ".xlsm")
            {
                reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            }
            else
            {
                _logger.LogWarning("ExcelDataReader: Unsupported extension {Extension} for {FileName}", extension, fileName);
                return null;
            }
            
            if (reader == null)
            {
                _logger.LogWarning("ExcelDataReader: Failed to create reader for {FileName}", fileName);
                return null;
            }
            
            try
            {
                // Configure reader
                var configuration = new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false // We'll parse headers manually
                    }
                };
                
                // Read into DataSet
                var dataSet = reader.AsDataSet(configuration);
                
                if (dataSet.Tables.Count == 0)
                {
                    _logger.LogWarning("ExcelDataReader: No worksheets found in {FileName}", fileName);
                    return null;
                }
                
                // Get first worksheet as DataTable
                var dataTable = dataSet.Tables[0];
                
                _logger.LogInformation("✅ ExcelDataReader successfully read {FileName}: {Rows} rows, {Cols} columns", 
                    fileName, dataTable.Rows.Count, dataTable.Columns.Count);
                
                // Convert DataTable to IWorksheet-like structure for parsing
                var result = await ParseFromDataTableAsync(dataTable, fileName);
                
                _logger.LogInformation("✅ ExcelDataReader fallback parse completed for {FileName}", fileName);
                return result;
            }
            finally
            {
                reader?.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ExcelDataReader fallback also failed for {FileName}: {Error}", fileName, ex.Message);
            return null;
        }
    }
    
    /// <summary>
    /// Parse order data from DataTable (ExcelDataReader format)
    /// Adapts the existing parsing logic to work with DataTable instead of IWorksheet
    /// </summary>
    private async Task<TimeExcelParseResult> ParseFromDataTableAsync(DataTable dataTable, string fileName)
    {
        var result = new TimeExcelParseResult();
        
        try
        {
            // Detect order type (use the ITimeExcelParserService interface method that accepts DataTable)
            var orderType = ((ITimeExcelParserService)this).DetectOrderType(fileName, dataTable);
            
            // Parse based on order type
            ParsedOrderData orderData = ParseFromDataTable(dataTable, fileName, orderType);
            
            // Validate
            var validationErrors = ValidateOrderData(orderData, orderType);
            
            result.Success = validationErrors.Count == 0;
            result.ValidationErrors = validationErrors;
            result.OrderData = orderData;
            
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("ExcelDataReader parse completed with {ErrorCount} validation errors: {Errors}",
                    validationErrors.Count, string.Join(", ", validationErrors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing DataTable for {FileName}: {Error}", fileName, ex.Message);
            result.Success = false;
            result.ValidationErrors = new List<string> { $"ExcelDataReader parse error: {ex.Message}" };
        }
        
        return result;
    }
    
    /// <summary>
    /// Simplified parser that extracts data from DataTable
    /// This adapts the existing parsing logic to work with DataTable
    /// </summary>
    private ParsedOrderData ParseFromDataTable(DataTable dataTable, string fileName, string orderType)
    {
        // Handle MODIFICATION_OUTDOOR with specific logic
        if (orderType == "MODIFICATION_OUTDOOR")
        {
            return ParseModificationOutdoorFromDataTable(dataTable, fileName);
        }
        
        // ✅ PRIORITY 1: Check for TBBN Service ID FIRST (for .xls files via ExcelDataReader)
        var serviceId = ExtractRightSideValueByLabels(dataTable, 
            "Service ID", "SERVICE ID", "TBBN", "ServiceID");
        
        // Fallback: Search for TBBN pattern in all cells
        if (string.IsNullOrEmpty(serviceId) && dataTable != null)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                foreach (DataColumn col in dataTable.Columns)
                {
                    var cellValue = row[col]?.ToString() ?? "";
                    var match = System.Text.RegularExpressions.Regex.Match(cellValue, @"TBBN[A-Z]?\d{5,}[A-Z]?");
                    if (match.Success)
                    {
                        serviceId = match.Value;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(serviceId)) break;
            }
        }
        
        if (!string.IsNullOrEmpty(serviceId) && serviceId.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
        {
            // This is a TIME order (FTTH/FTTO), not Celcom/Digi
            _logger.LogInformation("✅ Detected TBBN Service ID ({ServiceId}) in DataTable - parsing as TIME FTTH/FTTO activation", serviceId);
            // Set order type to ACTIVATION and let generic parsing continue (will detect FTTH/FTTO)
            orderType = "ACTIVATION";
        }
        
        // ✅ PRIORITY 2: Check if this is a Celcom activation (only if no TBBN Service ID found)
        var partnerServiceId = dataTable != null 
            ? ExtractRightSideValueByLabels(dataTable, 
                "PARTNER SERVICE ID", "PARTNER SERVICE ID:", "PARTNER SERVICEID",
                "DIGI ORDER ID", "Digi Order ID", "Digi Order ID:", "DIGI ORDERID")
            : null;
        
        if (!string.IsNullOrEmpty(partnerServiceId) && 
            partnerServiceId.StartsWith("CELCOM", StringComparison.OrdinalIgnoreCase))
        {
            // It's a TIME-Celcom activation
            _logger.LogInformation("✅ Detected TIME-Celcom activation in DataTable: ServiceId={ServiceId}", partnerServiceId);
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null when parsing Celcom activation");
            }
            return ParseCelcomActivationFromDataTable(dataTable, fileName);
        }
        
        // ✅ Check if this is a Digi activation (for .xls files via ExcelDataReader)
        // Also try "DIGI ORDER ID" label explicitly (some Digi .xls use this instead of PARTNER SERVICE ID)
        var digiOrderId = dataTable != null
            ? ExtractRightSideValueByLabels(dataTable, "DIGI ORDER ID", "Digi Order ID", "Digi Order ID:", "DIGI ORDERID")
            : null;
        var digiServiceId = !string.IsNullOrEmpty(partnerServiceId) && (partnerServiceId.StartsWith("DIGI", StringComparison.OrdinalIgnoreCase) || partnerServiceId.StartsWith("DIGI00", StringComparison.OrdinalIgnoreCase))
            ? partnerServiceId
            : (!string.IsNullOrEmpty(digiOrderId) && (digiOrderId.StartsWith("DIGI", StringComparison.OrdinalIgnoreCase) || digiOrderId.StartsWith("DIGI00", StringComparison.OrdinalIgnoreCase)) ? digiOrderId : null);
        
        if (!string.IsNullOrEmpty(digiServiceId))
        {
            // It's a TIME-Digi activation
            _logger.LogInformation("✅ Detected TIME-Digi activation in DataTable: ServiceId={ServiceId}", digiServiceId);
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null when parsing Digi activation");
            }
            return ParseDigiActivationFromDataTable(dataTable, fileName);
        }
        
        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            OrderTypeCode = orderType,
            OrderTypeHint = orderType == "ACTIVATION" ? "FTTH" : orderType // Default to FTTH, will be updated if FTTO detected
        };
        
        // ✅ For TIME activations (TBBN Service ID), detect FTTH vs FTTO
        if (orderType == "ACTIVATION" && !string.IsNullOrEmpty(serviceId) && serviceId.StartsWith("TBBN", StringComparison.OrdinalIgnoreCase))
        {
            // Detect installation type (FTTH or FTTO) from filename or data
            var fileNameUpper = fileName.ToUpperInvariant();
            string installationType = "FTTH"; // Default
            
            if (fileNameUpper.Contains("FTTO"))
            {
                installationType = "FTTO";
            }
            else if (fileNameUpper.Contains("FTTH"))
            {
                installationType = "FTTH";
            }
            else
            {
                // Check data table for FTTH/FTTO indicators
                if (dataTable != null)
                {
                    var taskType = ExtractValueFromDataTable(dataTable, new[] { "TASK", "Task", "Task Type", "TASK TYPE" });
                    if (!string.IsNullOrEmpty(taskType))
                    {
                        var taskUpper = taskType.ToUpperInvariant();
                        if (taskUpper.Contains("FTTO") || taskUpper.Contains("OFFICE"))
                        {
                            installationType = "FTTO";
                        }
                        else if (taskUpper.Contains("FTTH") || taskUpper.Contains("HOME"))
                        {
                            installationType = "FTTH";
                        }
                    }
                }
            }
            
            data.OrderTypeHint = installationType;
            data.PartnerCode = installationType == "FTTO" ? "TIME-FTTO" : "TIME-FTTH";
            _logger.LogInformation("✅ Detected installation type: {InstallationType} for TBBN Service ID {ServiceId}", installationType, serviceId);
        }
        
        // Build a cell lookup dictionary for easier access (similar to worksheet cells)
        var cellLookup = new Dictionary<string, string>();
        
        if (dataTable == null)
        {
            throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null when building cell lookup");
        }
        
        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            for (int col = 0; col < dataTable.Columns.Count; col++)
            {
                var cellValue = dataTable.Rows[row][col]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    // Store by Excel-style address (e.g., "A1", "B2")
                    var cellAddress = $"{GetColumnLetter(col)}{row + 1}";
                    cellLookup[cellAddress] = cellValue;
                    
                    // Also store by row/col index for easier lookup
                    cellLookup[$"R{row + 1}C{col + 1}"] = cellValue;
                }
            }
        }
        
        // Use the existing label mapping logic but adapted for DataTable
        // TIME Excel format: Labels in Column 1 (index 1), Values in Column 2 (index 2)
        // Based on inspection: Col0 is often empty, Col1 has labels, Col2 has values
        _logger.LogInformation("🔍 Parsing DataTable: {Rows} rows, {Cols} columns", dataTable.Rows.Count, dataTable.Columns.Count);
        
        // ✅ Service ID extraction with normalized header detection (for all order types)
        data.ServiceId = ExtractServiceIdWithNormalizedHeader(dataTable);
        _logger.LogInformation("ServiceId extracted: '{ServiceId}'", data.ServiceId ?? "(null)");
        
        data.CustomerName = ExtractValueFromDataTable(dataTable, new[] { "CUSTOMER NAME", "Customer Name", "Name", "Customer", "CustomerName" });
        _logger.LogInformation("CustomerName extracted: '{CustomerName}'", data.CustomerName ?? "(null)");
        data.ContactPerson = ExtractValueFromDataTable(dataTable, new[] { "CONTACT PERSON", "Contact Person", "Contact", "PIC" });
        // Customer Phone: D13 = Row 13 (index 12), Column D (index 3)
        // But also check Col2 (index 1) which is where ExcelDataReader typically puts it
        var phoneValue = ExtractValueFromDataTable(dataTable, new[] { "CONTACT NO", "CONTACT NO.", "Contact No", "Phone", "Contact", "Mobile", "Phone No", "Phone Number", "H/P", "HANDPHONE" });
        
        // Validate and fix if needed
        if (string.IsNullOrEmpty(phoneValue) || !IsPhoneNumber(phoneValue))
        {
            // Fallback: Look for CONTACT NO label and check columns 2, 3, 4, 5
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                var label = dataTable.Rows[row][1]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
                if (label.Contains("CONTACT NO") || label == "CONTACT NO.")
                {
                    // Check columns 2, 3, 4, 5 for phone value
                    for (int col = 2; col <= 5 && col < dataTable.Columns.Count; col++)
                    {
                        var phone = dataTable.Rows[row][col]?.ToString()?.Trim();
                        if (IsValidValue(phone))
                        {
                            // Extract phone number if it contains "/" (format: "0167170171 / 0167170171")
                            if (phone != null && phone.Contains("/"))
                            {
                                var phonePart = phone.Split('/')[0].Trim();
                                if (IsPhoneNumber(phonePart))
                                {
                                    phoneValue = phonePart;
                                    _logger.LogInformation("✓ Found Customer Phone (with /) at R{Row}C{Col}: '{Value}'", row + 1, col + 1, phonePart);
                                    break;
                                }
                            }
                            // Check if the whole value is a phone number
                            else if (IsPhoneNumber(phone))
                            {
                                phoneValue = phone;
                                _logger.LogInformation("✓ Found Customer Phone at R{Row}C{Col}: '{Value}'", row + 1, col + 1, phone);
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(phoneValue) && IsPhoneNumber(phoneValue)) break;
                }
            }
        }
        data.CustomerPhone = NormalizePhone(phoneValue);
        data.CustomerEmail = ExtractValueFromDataTable(dataTable, new[] { "EMAIL", "Email", "E-mail", "Email Address", "E-MAIL" });
        // Service Address: Can be in multiple columns (E11 = Col4, row 10)
        data.ServiceAddress = ExtractValueFromDataTable(dataTable, new[] { "SERVICE ADDRESS", "Service Address", "Address", "Installation Address", "New Address" });
        if (string.IsNullOrEmpty(data.ServiceAddress))
        {
            // Fallback: Look for address pattern in row 11, columns 2-5
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                var label = dataTable.Rows[row][1]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
                if (label.Contains("SERVICE ADDRESS") || label.Contains("SERVICE ADD"))
                {
                    // Check columns 2, 3, 4, 5 for address value
                    for (int col = 2; col <= 5 && col < dataTable.Columns.Count; col++)
                    {
                        var addr = dataTable.Rows[row][col]?.ToString()?.Trim();
                        if (IsValidValue(addr) && addr != null && addr.Length > 10) // Address should be reasonably long
                        {
                            data.ServiceAddress = addr;
                            _logger.LogInformation("✓ Found Service Address in fallback at R{Row}C{Col}: '{Value}'", row + 1, col + 1, addr);
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(data.ServiceAddress)) break;
                }
            }
        }
        // ✅ Generic helpers: All fields use the same column-agnostic, future-proof method
        data.OldAddress = ExtractRightSideValueByLabels(dataTable, "Old Address", "OLD ADDRESS", "Previous Address", "PREVIOUS ADDRESS");
        
        // ✅ Extract AWO Number for Assurance orders (if present in Excel)
        data.AwoNumber = ExtractRightSideValueByLabels(dataTable, 
            "AWO NUMBER", "AWO NUMBER:", "AWO NO", "AWO NO.", 
            "AWO", "AWO:", "AWO Number", "AWO Number:");
        
        // ✅ Extract appointment date and start time (enhanced label matching)
        var appointmentDateStr = ExtractRightSideValueByLabels(dataTable, 
            "Appointment Date", "APPOINTMENT DATE", "APPOINT. DATE & TIME", "APPOINT DATE & TIME", 
            "Appointment Date & Time", "Date & Time", "DATE & TIME", "Appointment", "Date");
        
        // ✅ Try to extract separate time field if date doesn't include time
        // This is just the start time, not a window
        var timeStr = ExtractRightSideValueByLabels(dataTable,
            "Appointment Time", "APPOINTMENT TIME", "Time", "TIME", 
            "Start Time", "START TIME", "Appointment Start Time");
        
        // Combine date and time if both are found separately
        if (!string.IsNullOrEmpty(appointmentDateStr) && !string.IsNullOrEmpty(timeStr))
        {
            // Check if date already contains time
            var hasTimeInDate = appointmentDateStr.Contains(":") || appointmentDateStr.Contains("AM") || appointmentDateStr.Contains("PM");
            
            if (!hasTimeInDate)
            {
                // Date doesn't have time, combine with time field
                appointmentDateStr = $"{appointmentDateStr} {timeStr}";
                _logger.LogInformation("✅ Combined separate date and time fields in DataTable: Time='{Time}' → '{Combined}'", 
                    timeStr, appointmentDateStr);
            }
        }
        else if (!string.IsNullOrEmpty(timeStr) && string.IsNullOrEmpty(appointmentDateStr))
        {
            // Only time found, try to find date separately
            var dateOnly = ExtractRightSideValueByLabels(dataTable, "Date", "DATE", "Appointment Date", "APPOINTMENT DATE");
            if (!string.IsNullOrEmpty(dateOnly))
            {
                appointmentDateStr = $"{dateOnly} {timeStr}";
                _logger.LogInformation("✅ Combined separate date and time fields in DataTable: Date='{Date}', Time='{Time}' → '{Combined}'", 
                    dateOnly, timeStr, appointmentDateStr);
            }
        }
        
        data.AppointmentDateTime = ParseAppointmentDateTimeWithTimezone(appointmentDateStr);
        
        // Log if appointment date was found
        if (string.IsNullOrEmpty(appointmentDateStr))
        {
            _logger.LogWarning("⚠️ Appointment date/time not found in DataTable for {FileName}. Searched labels: Appointment Date, APPOINTMENT DATE, APPOINT. DATE & TIME, etc.", fileName);
        }
        else
        {
            _logger.LogInformation("✅ Appointment date/time extracted from DataTable: '{DateStr}' → {ParsedDate}", 
                appointmentDateStr, 
                data.AppointmentDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "(null)");
        }
        
        // Note: BuildingName, UnitNo, City, State, Postcode are not properties of ParsedOrderData
        // They would be part of ServiceAddress or handled separately in the mapping layer
        data.PackageName = ExtractRightSideValueByLabels(dataTable, "Package Name", "PACKAGE", "Package", "Plan Name", "Plan");
        data.Bandwidth = ExtractRightSideValueByLabels(dataTable, "Bandwidth", "BANDWIDTH", "Speed", "Internet Speed", "MBPS");
        data.Username = ExtractRightSideValueByLabels(dataTable, "Login ID", "LOGIN ID", "Username", "User Name", "USERNAME");
        data.Password = ExtractRightSideValueByLabels(dataTable, "Password", "PASSWORD", "PWD");
        data.OnuPassword = ExtractRightSideValueByLabels(dataTable, "ONU Password", "ONU PASSWORD", "Password ONU");
        data.OnuSerialNumber = ExtractRightSideValueByLabels(dataTable, "Serial Number", "SERIAL NUMBER", "ONU Serial", "ONU Serial Number", "Serial", "S/N", "SERIAL NO");
        data.VoipServiceId = ExtractVoipServiceIdFromDataTable(dataTable);
        data.VoipPassword = ExtractRightSideValueByLabels(dataTable, "VOIP Password", "VOIP PASSWORD", "Voice Password");
        data.SplitterLocation = ExtractRightSideValueByLabels(dataTable, "Splitter Location", "SPLITTER LOCATION", "Splitter", "SPLITTER");
        data.Remarks = ExtractRightSideValueByLabels(dataTable, "Remarks", "REMARKS", "Note", "Notes", "Comments");
        
        // ✅ Generic helper: Appointment date with timezone - handle various label formats (re-extract if not already found)
        if (string.IsNullOrEmpty(appointmentDateStr))
        {
            appointmentDateStr = ExtractRightSideValueByLabels(dataTable, 
                "APPOINT. DATE & TIME", "APPOINT DATE & TIME", "APPOINTMENT DATE & TIME",
                "Appointment Date", "APPOINTMENT DATE", "Date", "Installation Date", "Service Date");
            data.AppointmentDateTime = ParseAppointmentDateTimeWithTimezone(appointmentDateStr);
        }
        
        // Clean up Service Address (remove extra spaces and commas)
        if (!string.IsNullOrEmpty(data.ServiceAddress))
        {
            data.ServiceAddress = Regex.Replace(data.ServiceAddress, @"\s*,\s*,", ","); // Remove double commas
            data.ServiceAddress = Regex.Replace(data.ServiceAddress, @"\s+", " "); // Multiple spaces to single
            data.ServiceAddress = data.ServiceAddress.Trim().TrimEnd(',', ' '); // Remove trailing commas/spaces
        }
        
        // Extract materials
        var materials = ParseMaterialsFromDataTable(dataTable);
        data.Materials = materials;

        // Extra/unmapped parser info: rows 66+ (0-based) that have content - store for display as AdditionalInformation
        data.AdditionalInformation = BuildUnhandledSectionsFromDataTable(dataTable);

        // ✅ Calculate confidence score (same as MODIFICATION_OUTDOOR path)
        data.ConfidenceScore = CalculateConfidenceScore(data, orderType);

        return data;
    }

    /// <summary>
    /// Build a single string from rows that were not mapped to structured fields (e.g. REMARKS, SPLITTER LOCATION, footer).
    /// Used for draft.AdditionalInformation (read-only display on Create Order page).
    /// </summary>
    private static string? BuildUnhandledSectionsFromDataTable(DataTable dataTable)
    {
        const int unhandledStartRow = 66; // 0-based; rows 0-65 are used for customer/materials etc.
        var lines = new List<string>();
        for (int r = unhandledStartRow; r < dataTable.Rows.Count; r++)
        {
            var row = dataTable.Rows[r];
            var parts = new List<string>();
            for (int c = 0; c < dataTable.Columns.Count; c++)
            {
                var v = row[c];
                if (v != null && v != DBNull.Value)
                {
                    var s = v.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                        parts.Add(s);
                }
            }
            if (parts.Count > 0)
                lines.Add($"Row {r + 1}: " + string.Join(" | ", parts));
        }
        return lines.Count > 0 ? string.Join("\n", lines) : null;
    }

    private string? ExtractValueFromDataTable(DataTable dataTable, string[] labels)
    {
        // TIME Excel format: Labels are in Column 1 (index 1), Values in Column 2 (index 2)
        // Based on inspection: Col0 is often empty, Col1 has labels, Col2 has values
        // Try both Col0 and Col1 for labels, Col1 and Col2 for values
        const int labelColumnIndex1 = 0; // Try Col0 first
        const int labelColumnIndex2 = 1; // Then Col1
        const int valueColumnIndex1 = 1;  // Try Col1 for value
        const int valueColumnIndex2 = 2;  // Then Col2 for value
        
        if (dataTable.Columns.Count < 2)
        {
            return null;
        }
        
        // Try both column combinations
        var columnPairs = new[]
        {
            (labelCol: labelColumnIndex1, valueCol: valueColumnIndex2), // Col0 label, Col2 value
            (labelCol: labelColumnIndex2, valueCol: valueColumnIndex2), // Col1 label, Col2 value (most common)
            (labelCol: labelColumnIndex1, valueCol: valueColumnIndex1), // Col0 label, Col1 value
        };
        
        foreach (var (labelCol, valueCol) in columnPairs)
        {
            if (labelCol >= dataTable.Columns.Count || valueCol >= dataTable.Columns.Count) continue;
            
            for (int row = 0; row < dataTable.Rows.Count; row++)
            {
                var labelCell = dataTable.Rows[row][labelCol]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(labelCell)) continue;
                
                var labelCellUpper = labelCell.ToUpperInvariant();
                var cleanLabel = labelCellUpper.TrimEnd(':', ' ', '.');
                
                // Try each label variant
                foreach (var label in labels)
                {
                    var labelUpper = label.ToUpperInvariant();
                    
                    // Debug: Log when we find a potential match
                    if (cleanLabel.Contains(labelUpper) || labelCellUpper.Contains(labelUpper))
                    {
                        var value = dataTable.Rows[row][valueCol]?.ToString()?.Trim();
                        // Also check adjacent columns for debugging
                        var valueCol1 = valueCol > 0 ? dataTable.Rows[row][valueCol - 1]?.ToString()?.Trim() : "(N/A)";
                        var valueCol2 = valueCol < dataTable.Columns.Count - 1 ? dataTable.Rows[row][valueCol + 1]?.ToString()?.Trim() : "(N/A)";
                        _logger.LogInformation("🔍 Label '{Label}'='{Cell}' at R{Row}C{LabelCol}, checking C{ValueCol}='{Value}' (C{PrevCol}='{Prev}', C{NextCol}='{Next}'), IsValid={IsValid}", 
                            labelUpper, cleanLabel, row + 1, labelCol + 1, valueCol + 1, value ?? "(empty)", 
                            valueCol, valueCol1, valueCol + 2, valueCol2, IsValidValue(value));
                    }
                    
                    // Helper to get value, checking multiple columns if needed (sometimes there's an empty column)
                    // TIME Excel format: Labels in Col1, values can be in Col2, Col3, Col4, or Col5
                    string? GetValueFromRow(int valueColumn)
                    {
                        // Check the specified column and up to 3 columns to the right
                        for (int offset = 0; offset <= 3; offset++)
                        {
                            int checkCol = valueColumn + offset;
                            if (checkCol >= dataTable.Columns.Count) break;
                            
                            var val = dataTable.Rows[row][checkCol]?.ToString()?.Trim();
                            if (IsValidValue(val))
                            {
                                _logger.LogDebug("Found value in column {Col} (offset {Offset} from base {BaseCol}): '{Value}'", 
                                    checkCol + 1, offset, valueColumn + 1, val);
                                return val;
                            }
                        }
                        return null;
                    }
                    
                    // Exact match (after cleaning)
                    if (cleanLabel == labelUpper)
                    {
                        var value = GetValueFromRow(valueCol);
                        if (value != null)
                        {
                            _logger.LogInformation("✓ Found exact label '{Label}' at R{Row}C{LabelCol}, value: '{Value}'", label, row + 1, labelCol + 1, value);
                            return value;
                        }
                    }
                    // Starts with match
                    else if (cleanLabel.StartsWith(labelUpper) || labelCellUpper.StartsWith(labelUpper))
                    {
                        var value = GetValueFromRow(valueCol);
                        if (value != null)
                        {
                            _logger.LogInformation("✓ Found label '{Label}' (starts with) at R{Row}C{LabelCol}, value: '{Value}'", label, row + 1, labelCol + 1, value);
                            return value;
                        }
                    }
                    // Contains match (but only if label is significant)
                    else if (labelUpper.Length >= 4 && cleanLabel.Contains(labelUpper))
                    {
                        var value = GetValueFromRow(valueCol);
                        if (value != null)
                        {
                            _logger.LogInformation("✓ Found label '{Label}' (contains) at R{Row}C{LabelCol}, value: '{Value}'", label, row + 1, labelCol + 1, value);
                            return value;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    private string? ExtractVoipServiceIdFromDataTable(DataTable dataTable)
    {
        // VOIP Service ID extraction with phone number detection
        // VOIP Service ID is typically at D31 (Row 30, Col 3) or in VOIP section
        // Look for "SERVICE ID. / PASSWORD" label in VOIP section
        
        bool inVoipSection = false;
        
        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            var labelCol1 = dataTable.Rows[row][1]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
            
            // Check if we're entering VOIP section
            if (labelCol1 == "VOIP" || labelCol1.Contains("VOIP"))
            {
                inVoipSection = true;
            }
            
            // Look for "SERVICE ID" in VOIP section (can be "SERVICE ID. / PASSWORD")
            if (inVoipSection && (labelCol1.Contains("SERVICE ID") || labelCol1.Contains("SERVICEID")))
            {
                // Check columns 2, 3, 4 for phone number value
                for (int col = 2; col <= 4 && col < dataTable.Columns.Count; col++)
                {
                    var value = dataTable.Rows[row][col]?.ToString()?.Trim();
                    if (IsValidValue(value))
                    {
                        // Extract phone number if it contains "/" (format: "0330506349/")
                        if (value != null && value.Contains("/"))
                        {
                            var phonePart = value.Split('/')[0].Trim();
                            if (IsPhoneNumber(phonePart))
                            {
                                _logger.LogInformation("✓ Found VOIP Service ID at R{Row}C{Col}: '{Value}'", row + 1, col + 1, phonePart);
                                return phonePart;
                            }
                        }
                        // Or check if the whole value is a phone number
                        else if (IsPhoneNumber(value))
                        {
                            _logger.LogInformation("✓ Found VOIP Service ID at R{Row}C{Col}: '{Value}'", row + 1, col + 1, value);
                            return value;
                        }
                    }
                }
            }
            
            // Also check row 31 specifically (D31 = Row 30, Col 3) for VOIP Service ID
            if (row == 30) // Row 31 (0-indexed = 30)
            {
                for (int col = 2; col <= 4 && col < dataTable.Columns.Count; col++)
                {
                    var value = dataTable.Rows[row][col]?.ToString()?.Trim();
                    if (IsValidValue(value))
                    {
                        // Extract phone number if it contains "/"
                        if (value != null && value.Contains("/"))
                        {
                            var phonePart = value.Split('/')[0].Trim();
                            if (IsPhoneNumber(phonePart))
                            {
                                _logger.LogInformation("✓ Found VOIP Service ID at R31C{Col}: '{Value}'", col + 1, phonePart);
                                return phonePart;
                            }
                        }
                        else if (IsPhoneNumber(value))
                        {
                            _logger.LogInformation("✓ Found VOIP Service ID at R31C{Col}: '{Value}'", col + 1, value);
                            return value;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Extract REMARKS from DataTable - handles multi-cell/multi-row values
    /// REMARKS can span multiple cells, so we need to collect all text from the row and subsequent rows
    /// </summary>
    private string? ExtractRemarksFromDataTable(DataTable dataTable)
    {
        // Look for REMARKS label
        for (int row = 0; row < dataTable.Rows.Count; row++)
        {
            var labelCol1 = dataTable.Rows[row][1]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
            var labelCol0 = dataTable.Rows[row][0]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
            
            // Check if this is the REMARKS row
            if (labelCol1.Contains("REMARKS") || labelCol0.Contains("REMARKS"))
            {
                var remarksParts = new List<string>();
                
                // Check columns 2-10 for values (REMARKS can span multiple columns)
                for (int col = 2; col < Math.Min(10, dataTable.Columns.Count); col++)
                {
                    var cellValue = dataTable.Rows[row][col]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(cellValue))
                    {
                        remarksParts.Add(cellValue);
                    }
                }
                
                // Also check subsequent rows (REMARKS can span multiple rows)
                for (int nextRow = row + 1; nextRow < Math.Min(row + 5, dataTable.Rows.Count); nextRow++)
                {
                    // Check if next row has content (not a new label)
                    var nextRowLabel = dataTable.Rows[nextRow][1]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
                    var nextRowLabel0 = dataTable.Rows[nextRow][0]?.ToString()?.Trim()?.ToUpperInvariant() ?? "";
                    
                    // If next row starts with a new label (like "SPLITTER LOCATION"), stop
                    if (!string.IsNullOrWhiteSpace(nextRowLabel) && 
                        (nextRowLabel.Contains("SPLITTER") || nextRowLabel.Contains("EQUIPMENT") || 
                         (nextRowLabel.Length > 10 && !nextRowLabel.Contains("TP") && !nextRowLabel.Contains("Huawei"))))
                    {
                        break;
                    }
                    
                    // Collect values from next row
                    for (int col = 2; col < Math.Min(10, dataTable.Columns.Count); col++)
                    {
                        var cellValue = dataTable.Rows[nextRow][col]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            remarksParts.Add(cellValue);
                        }
                    }
                }
                
                if (remarksParts.Any())
                {
                    var remarksText = string.Join(" ", remarksParts).Trim();
                    _logger.LogInformation("✓ Found REMARKS (multi-cell) at R{Row}: '{Value}'", row + 1, remarksText.Length > 100 ? remarksText.Substring(0, 100) + "..." : remarksText);
                    return remarksText;
                }
            }
        }
        
        // Fallback: Try standard extraction
        return ExtractRightSideValueByLabels(dataTable, 
            "REMARKS", "Remarks", "REMARKS:", "Remarks:", "Note", "Notes", "Comments");
    }
    
    private string GetColumnLetter(int columnIndex)
    {
        string columnLetter = "";
        while (columnIndex >= 0)
        {
            columnLetter = (char)('A' + (columnIndex % 26)) + columnLetter;
            columnIndex = columnIndex / 26 - 1;
        }
        return columnLetter;
    }
    
    private class DiagnosticResult
    {
        public bool IsValid { get; set; } = true;
        public long FileSize { get; set; }
        public string? FileHeader { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    #endregion

    #region OldAddress Fallback Extraction

    /// <summary>
    /// Fallback method to extract Old Address when standard label search fails
    /// Looks for address patterns near "OLD", "PREVIOUS", "CURRENT" keywords
    /// </summary>
    private string? ExtractOldAddressFallback(IWorksheet worksheet)
    {
        var keywords = new[] { "OLD", "PREVIOUS", "CURRENT", "EXISTING", "ORIGINAL", "FROM" };
        
        for (int row = 1; row <= worksheet.UsedRange.LastRow; row++)
        {
            for (int col = 1; col <= worksheet.UsedRange.LastColumn; col++)
            {
                var cellValue = GetCellValue(worksheet, row, col)?.ToUpperInvariant();
                if (string.IsNullOrEmpty(cellValue)) continue;
                
                // Check if cell contains any keyword
                foreach (var keyword in keywords)
                {
                    if (cellValue.Contains(keyword) && 
                        (cellValue.Contains("ADDRESS") || cellValue.Contains("ADD") || cellValue.Contains("LOCATION")))
                    {
                        // Found keyword, try to get value from adjacent cells
                        // Check right (up to 3 columns)
                        for (int offset = 1; offset <= 3 && col + offset <= worksheet.UsedRange.LastColumn; offset++)
                        {
                            var rightValue = GetCellValue(worksheet, row, col + offset);
                            if (IsValidAddress(rightValue))
                            {
                                _logger.LogInformation("✓ Found Old Address via fallback: {Address}", rightValue);
                                return rightValue;
                            }
                        }
                        
                        // Check below (up to 2 rows)
                        for (int offset = 1; offset <= 2 && row + offset <= worksheet.UsedRange.LastRow; offset++)
                        {
                            var belowValue = GetCellValue(worksheet, row + offset, col);
                            if (IsValidAddress(belowValue))
                            {
                                _logger.LogInformation("✓ Found Old Address via fallback: {Address}", belowValue);
                                return belowValue;
                            }
                        }
                    }
                }
            }
        }
        
        return null;
    }

    private bool IsValidAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        
        // Must be longer than 10 characters (addresses are usually longer)
        if (trimmed.Length < 10) return false;
        
        // Should contain common address indicators
        var addressIndicators = new[] { "JALAN", "JLN", "STREET", "ROAD", "RD", "NO", "LOT", "FLOOR", "LEVEL", "BLOCK", "TAMAN", "KAMPUNG", "KG" };
        var upperValue = trimmed.ToUpperInvariant();
        
        return addressIndicators.Any(indicator => upperValue.Contains(indicator));
    }

    #endregion

    #region Helper Methods

    private string? GetCellValue(IWorksheet worksheet, int row, int col)
    {
        try
        {
            if (row < 1 || row > worksheet.UsedRange.LastRow) return null;
            if (col < 1 || col > worksheet.UsedRange.LastColumn) return null;

            var cell = worksheet.Range[row, col];
            return cell?.Text?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string? FindPatternInSheet(IWorksheet worksheet, string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        
        for (int row = 1; row <= Math.Min(worksheet.UsedRange.LastRow, 100); row++)
        {
            for (int col = 1; col <= Math.Min(worksheet.UsedRange.LastColumn, 20); col++)
            {
                var cellValue = GetCellValue(worksheet, row, col);
                if (!string.IsNullOrEmpty(cellValue))
                {
                    var match = regex.Match(cellValue);
                    if (match.Success) return match.Value;
                }
            }
        }
        
        return null;
    }

    private string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        // Remove spaces, dashes, and other separators
        var cleaned = phone.Trim()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("/", "");

        // Remove +60 or 60 prefix, add 0 if needed
        if (cleaned.StartsWith("+60"))
            cleaned = "0" + cleaned.Substring(3);
        else if (cleaned.StartsWith("60"))
            cleaned = "0" + cleaned.Substring(2);
        else if (!cleaned.StartsWith("0") && cleaned.Length >= 9)
            cleaned = "0" + cleaned;

        return cleaned;
    }

    /// <summary>
    /// Calculate confidence score for Celcom orders - includes critical Celcom-specific fields
    /// </summary>
    private decimal CalculateCelcomConfidenceScore(ParsedOrderData data)
    {
        decimal score = 0.4m; // Lower base score for Celcom (stricter)

        // Required fields for all types
        if (!string.IsNullOrEmpty(data.ServiceId)) score += 0.12m;
        if (!string.IsNullOrEmpty(data.CustomerName)) score += 0.10m;
        if (!string.IsNullOrEmpty(data.CustomerPhone)) score += 0.08m;
        if (!string.IsNullOrEmpty(data.ServiceAddress)) score += 0.10m;
        if (data.AppointmentDateTime.HasValue) score += 0.10m;

        // Celcom-specific critical fields (reduce score if missing)
        if (!string.IsNullOrEmpty(data.Username)) score += 0.08m;
        else score -= 0.10m; // Penalty for missing LOGIN ID
        
        if (!string.IsNullOrEmpty(data.Password)) score += 0.08m;
        else score -= 0.10m; // Penalty for missing PASSWORD
        
        if (!string.IsNullOrEmpty(data.OnuPassword)) score += 0.08m;
        else score -= 0.10m; // Penalty for missing ONU PASSWORD

        // Optional but valuable fields
        if (!string.IsNullOrEmpty(data.PackageName)) score += 0.04m;
        if (data.Materials.Any()) score += 0.06m; // Materials boost confidence

        return Math.Max(0.0m, Math.Min(score, 1.0m)); // Ensure between 0 and 1
    }

    /// <summary>
    /// Calculate confidence score for Digi orders - includes critical Digi-specific fields
    /// </summary>
    private decimal CalculateDigiConfidenceScore(ParsedOrderData data)
    {
        decimal score = 0.4m; // Lower base score for Digi (stricter)

        // Required fields for all types
        if (!string.IsNullOrEmpty(data.ServiceId)) score += 0.12m;
        if (!string.IsNullOrEmpty(data.CustomerName)) score += 0.10m;
        if (!string.IsNullOrEmpty(data.CustomerPhone)) score += 0.08m;
        if (!string.IsNullOrEmpty(data.ServiceAddress)) score += 0.10m;
        if (data.AppointmentDateTime.HasValue) score += 0.10m;

        // Digi-specific critical fields (reduce score if missing)
        if (!string.IsNullOrEmpty(data.Username)) score += 0.08m;
        else score -= 0.10m; // Penalty for missing LOGIN ID
        
        if (!string.IsNullOrEmpty(data.Password)) score += 0.08m;
        else score -= 0.10m; // Penalty for missing PASSWORD
        
        // ONU Password is optional for Digi (not always present)
        if (!string.IsNullOrEmpty(data.OnuPassword)) score += 0.04m;

        // Optional but valuable fields
        if (!string.IsNullOrEmpty(data.PackageName)) score += 0.04m;
        if (data.Materials.Any()) score += 0.06m; // Materials boost confidence

        return Math.Max(0.0m, Math.Min(score, 1.0m)); // Ensure between 0 and 1
    }

    private decimal CalculateConfidenceScore(ParsedOrderData data, string orderType)
    {
        decimal score = 0.5m; // Base score

        // Required fields for all types
        if (!string.IsNullOrEmpty(data.ServiceId)) score += 0.15m;
        if (!string.IsNullOrEmpty(data.CustomerName)) score += 0.10m;
        if (!string.IsNullOrEmpty(data.CustomerPhone)) score += 0.08m;
        if (!string.IsNullOrEmpty(data.ServiceAddress)) score += 0.10m;
        if (data.AppointmentDateTime.HasValue) score += 0.10m;

        // Order-type specific
        if (orderType == "ACTIVATION")
        {
            if (!string.IsNullOrEmpty(data.PackageName)) score += 0.05m;
            if (!string.IsNullOrEmpty(data.Username)) score += 0.05m;
            if (data.Materials.Any()) score += 0.07m; // Materials boost confidence
        }
        else if (orderType == "MODIFICATION_OUTDOOR")
        {
            if (!string.IsNullOrEmpty(data.OldAddress)) score += 0.10m;
        }

        return Math.Min(score, 1.0m);
    }

    /// <summary>
    /// Structure Gate: required fields per business rules. Non-Assurance: ServiceId, CustomerName, ServiceAddress, CustomerPhone. Assurance: TicketId, CustomerName, ServiceAddress, CustomerPhone.
    /// </summary>
    private static List<string> GetMissingRequiredFields(ParsedOrderData? data, string orderType)
    {
        var missing = new List<string>();
        if (data == null) return missing;

        bool isAssurance = IsAssuranceOrder(orderType, data.OrderTypeHint);

        if (string.IsNullOrWhiteSpace(data.CustomerName))
            missing.Add("CustomerName");
        if (string.IsNullOrWhiteSpace(data.ServiceAddress))
            missing.Add("ServiceAddress");
        if (string.IsNullOrWhiteSpace(data.CustomerPhone))
            missing.Add("CustomerPhone");

        if (isAssurance)
        {
            if (string.IsNullOrWhiteSpace(data.TicketId))
                missing.Add("TicketId");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(data.ServiceId))
                missing.Add("ServiceId");
        }

        return missing;
    }

    private static bool IsAssuranceOrder(string orderType, string? orderTypeHint)
    {
        if (string.Equals(orderType, "Assurance", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrEmpty(orderTypeHint) &&
            (orderTypeHint.Equals("Assurance", StringComparison.OrdinalIgnoreCase) ||
             orderTypeHint.Contains("TTKT", StringComparison.OrdinalIgnoreCase)))
            return true;
        return false;
    }

    private List<string> ValidateOrderData(ParsedOrderData data, string orderType)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.CustomerName))
            errors.Add("Customer Name is required");
        if (string.IsNullOrWhiteSpace(data.ServiceAddress))
            errors.Add("Service Address is required");
        if (string.IsNullOrWhiteSpace(data.CustomerPhone))
            errors.Add("Customer Phone is required");

        bool isAssurance = IsAssuranceOrder(orderType, data.OrderTypeHint);
        if (isAssurance)
        {
            if (string.IsNullOrWhiteSpace(data.TicketId))
                errors.Add("Ticket ID is required for assurance orders");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(data.ServiceId))
                errors.Add("Service ID is required");
        }

        if (orderType == "MODIFICATION_OUTDOOR" && string.IsNullOrEmpty(data.OldAddress))
            _logger.LogWarning("Old Address not found for Modification order: {FileName}. User can add manually.", data.SourceFileName);

        return errors;
    }

    private static ParseReport BuildParseReport(
        string attachmentFileName,
        string? detectedFileSignature,
        long fileSizeBytes,
        string? fileHash,
        string engineUsed,
        bool conversionPerformed,
        string? convertedFilePath,
        long? convertedFileSizeBytes,
        string? selectedSheetName,
        Dictionary<string, int> sheetScores,
        int? detectedHeaderRow,
        int? headerScore,
        ParsedOrderData orderData,
        string orderType,
        List<string> missingRequiredFields,
        List<string> validationErrors,
        List<FieldDiagnosticEntry>? fieldDiagnostics = null,
        string? parseFailureCategory = null,
        int? sheetScoreBest = null,
        int? sheetScoreSecondBest = null,
        int? totalLabelHitsForRequiredFields = null)
    {
        var summary = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["ServiceId"] = !string.IsNullOrWhiteSpace(orderData.ServiceId),
            ["TicketId"] = !string.IsNullOrWhiteSpace(orderData.TicketId),
            ["CustomerName"] = !string.IsNullOrWhiteSpace(orderData.CustomerName),
            ["ServiceAddress"] = !string.IsNullOrWhiteSpace(orderData.ServiceAddress),
            ["CustomerPhone"] = !string.IsNullOrWhiteSpace(orderData.CustomerPhone),
            ["AppointmentDate"] = orderData.AppointmentDateTime.HasValue,
            ["PackageName"] = !string.IsNullOrWhiteSpace(orderData.PackageName),
            ["OnuSerialNumber"] = !string.IsNullOrWhiteSpace(orderData.OnuSerialNumber)
        };
        return new ParseReport
        {
            AttachmentFileName = attachmentFileName,
            DetectedFileSignatureType = detectedFileSignature,
            FileSizeBytes = fileSizeBytes,
            FileHash = fileHash,
            EngineUsed = engineUsed,
            ConversionPerformed = conversionPerformed,
            ConvertedFilePath = convertedFilePath,
            ConvertedFileSizeBytes = convertedFileSizeBytes,
            SelectedSheetName = selectedSheetName,
            SheetScores = sheetScores.Count > 0 ? new Dictionary<string, int>(sheetScores) : null,
            DetectedHeaderRow = detectedHeaderRow,
            HeaderScore = headerScore,
            ExtractedFieldsSummary = summary,
            MissingRequiredFields = new List<string>(missingRequiredFields),
            FieldDiagnostics = fieldDiagnostics,
            ParseFailureCategory = parseFailureCategory ?? ParseFailureCategory.None,
            SheetScoreBest = sheetScoreBest,
            SheetScoreSecondBest = sheetScoreSecondBest,
            TotalLabelHitsForRequiredFields = totalLabelHitsForRequiredFields
        };
    }

    private static (decimal FinalConfidence, ConfidenceBreakdown? Breakdown) ComputeConfidenceBreakdown(
        ParsedOrderData data,
        string orderType,
        List<string> missingRequiredFields)
    {
        if (missingRequiredFields.Count > 0)
        {
            return (0m, new ConfidenceBreakdown
            {
                RequiredCoveragePercent = 0m,
                ValidityPercent = 0m,
                OrderTypeCertaintyPercent = 0m,
                EnrichmentPercent = 0m
            });
        }

        decimal requiredCoverage = 1m;
        decimal orderTypeCertainty = string.IsNullOrEmpty(orderType) || orderType == "Unknown" ? 0.5m : 1m;
        decimal validity = 0m;
        if (!string.IsNullOrWhiteSpace(data.CustomerPhone)) validity += 0.5m;
        if (!string.IsNullOrWhiteSpace(data.ServiceAddress)) validity += 0.25m;
        if (!string.IsNullOrWhiteSpace(data.ServiceId) || !string.IsNullOrWhiteSpace(data.TicketId)) validity += 0.25m;
        decimal enrichment = 0m;
        if (data.AppointmentDateTime.HasValue) enrichment += 0.25m;
        if (!string.IsNullOrWhiteSpace(data.PackageName)) enrichment += 0.25m;
        if (!string.IsNullOrWhiteSpace(data.OnuSerialNumber)) enrichment += 0.25m;
        if (data.Materials.Any()) enrichment += 0.25m;
        enrichment = Math.Min(enrichment, 1m);

        var breakdown = new ConfidenceBreakdown
        {
            RequiredCoveragePercent = requiredCoverage,
            OrderTypeCertaintyPercent = orderTypeCertainty,
            ValidityPercent = validity,
            EnrichmentPercent = enrichment
        };
        decimal finalConfidence = 0.50m * requiredCoverage + 0.20m * orderTypeCertainty + 0.20m * validity + 0.10m * enrichment;
        return (Math.Min(1m, finalConfidence), breakdown);
    }

    /// <summary>
    /// Extract equipment information (Router and ONU models) from REMARKS text
    /// Looks for patterns like "TP Link EX510", "ONU Huawei HG8140H5"
    /// </summary>
    private string? ExtractEquipmentFromRemarks(string remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks)) return null;
        
        var equipmentList = new List<string>();
        
        // Pattern 1: "TP Link EX510" or "Router: TP Link EX510"
        var routerMatch = Regex.Match(remarks, @"(?:Router[:\s]*)?(TP\s*Link\s+EX\d+|TP-Link\s+EX\d+)", RegexOptions.IgnoreCase);
        if (routerMatch.Success)
        {
            equipmentList.Add($"Router: {routerMatch.Groups[1].Value.Trim()}");
        }
        
        // Pattern 2: "ONU Huawei HG8140H5" or "ONU: Huawei HG8140H5"
        var onuMatch = Regex.Match(remarks, @"(?:ONU[:\s]*)?(Huawei\s+HG\d+[A-Z]?\d*|Huawei\s+\w+)", RegexOptions.IgnoreCase);
        if (onuMatch.Success)
        {
            equipmentList.Add($"ONU: {onuMatch.Groups[1].Value.Trim()}");
        }
        
        // Pattern 3: Generic "Router" or "ONU" followed by model
        var genericRouter = Regex.Match(remarks, @"Router[:\s]+([A-Za-z0-9\s\-]+?)(?:\n|,|$)", RegexOptions.IgnoreCase);
        if (genericRouter.Success && !equipmentList.Any(e => e.Contains("Router", StringComparison.OrdinalIgnoreCase)))
        {
            equipmentList.Add($"Router: {genericRouter.Groups[1].Value.Trim()}");
        }
        
        var genericOnu = Regex.Match(remarks, @"ONU[:\s]+([A-Za-z0-9\s\-]+?)(?:\n|,|$)", RegexOptions.IgnoreCase);
        if (genericOnu.Success && !equipmentList.Any(e => e.Contains("ONU", StringComparison.OrdinalIgnoreCase)))
        {
            equipmentList.Add($"ONU: {genericOnu.Groups[1].Value.Trim()}");
        }
        
        return equipmentList.Any() ? string.Join(", ", equipmentList) : null;
    }

    /// <summary>
    /// Extract materials from REMARKS text
    /// Looks for equipment models that should be added as materials
    /// </summary>
    private List<ParsedOrderMaterialLine> ExtractMaterialsFromRemarks(string remarks)
    {
        var materials = new List<ParsedOrderMaterialLine>();
        
        if (string.IsNullOrWhiteSpace(remarks)) return materials;
        
        // Extract Router model
        var routerMatch = Regex.Match(remarks, @"(?:Router[:\s]*)?(TP\s*Link\s+EX\d+|TP-Link\s+EX\d+)", RegexOptions.IgnoreCase);
        if (routerMatch.Success)
        {
            materials.Add(new ParsedOrderMaterialLine
            {
                Name = routerMatch.Groups[1].Value.Trim(),
                ActionTag = "ADD",
                Quantity = 1,
                IsRequired = false,
                Notes = "Extracted from REMARKS"
            });
        }
        
        // Extract ONU model
        var onuMatch = Regex.Match(remarks, @"(?:ONU[:\s]*)?(Huawei\s+HG\d+[A-Z]?\d*|Huawei\s+\w+)", RegexOptions.IgnoreCase);
        if (onuMatch.Success)
        {
            materials.Add(new ParsedOrderMaterialLine
            {
                Name = onuMatch.Groups[1].Value.Trim(),
                ActionTag = "ADD",
                Quantity = 1,
                IsRequired = false,
                Notes = "Extracted from REMARKS"
            });
        }
        
        return materials;
    }

    #endregion
}

