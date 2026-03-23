using System.Text;
using System.Text.Json;
using CephasOps.Application.Parser.Services;
using ExcelDataReader;
using Microsoft.Extensions.Logging;

// Check command line argument to determine which test to run
if (args.Length > 0 && args[0].Equals("celcom", StringComparison.OrdinalIgnoreCase))
{
    // Run Celcom parser test
    await RunCelcomTestAsync();
}
else if (args.Length > 0 && args[0].Equals("digi", StringComparison.OrdinalIgnoreCase))
{
    // Run Digi parser test
    await RunDigiTestAsync();
}
else if (args.Length > 0 && args[0].Equals("excel", StringComparison.OrdinalIgnoreCase))
{
    RunExcelParseReport();
}
else if (args.Length > 0 && args[0].Equals("excel-full", StringComparison.OrdinalIgnoreCase))
{
    RunExcelFullReport();
}
else
{
    // Default: Run assurance parser test
    await RunAssuranceTestAsync();
}

static void RunExcelParseReport()
{
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    var possibleTestDataPaths = new[]
    {
        Path.Combine(@"C:\Projects\CephasOps\backend\test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "test-data"),
        Path.Combine(Directory.GetCurrentDirectory(), "test-data"),
    };

    string? testDataPath = null;
    foreach (var path in possibleTestDataPaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            testDataPath = fullPath;
            break;
        }
    }

    if (testDataPath == null)
    {
        Console.WriteLine("{\"error\": \"Test-data directory not found\"}");
        return;
    }

    var filePath = Path.Combine(testDataPath, "A1862604.xls");
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"{{\"error\": \"File not found: {filePath}\"}}");
        return;
    }

    var bytes = File.ReadAllBytes(filePath);
    using var stream = new MemoryStream(bytes);
    using var reader = ExcelReaderFactory.CreateBinaryReader(stream);
    var dataSet = reader.AsDataSet();

    if (dataSet.Tables.Count == 0)
    {
        Console.WriteLine("{\"sheetName\": \"(no sheets)\"}");
        return;
    }

    var table = dataSet.Tables[0]!;
    var headerRowIndex = DetectHeaderRow(table);
    var headers = GetHeaders(table, headerRowIndex);
    var dataStartRow = headerRowIndex + 1;

    var report = new
    {
        SheetName = table.TableName,
        TotalRows = table.Rows.Count,
        HeaderRowIndex = headerRowIndex,
        Headers = headers,
        First20Rows = GetFirst20Rows(table, headers, dataStartRow),
        NullOrEmptyRowCount = CountNullOrEmptyRows(table, dataStartRow, headers.Count),
        ColumnStats = GetColumnStats(table, headers, dataStartRow),
        DetectedKeyFields = DetectKeyFields(headers),
        Materials = ExtractMaterialsFromDataTable(table)
    };

    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}

static void RunExcelFullReport()
{
    var possibleTestDataPaths = new[]
    {
        Path.Combine(@"C:\Projects\CephasOps\backend\test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "test-data"),
        Path.Combine(Directory.GetCurrentDirectory(), "test-data"),
    };
    string? testDataPath = null;
    foreach (var path in possibleTestDataPaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath)) { testDataPath = fullPath; break; }
    }
    if (testDataPath == null)
    {
        Console.WriteLine("{\"error\": \"Test-data directory not found\"}");
        return;
    }
    var filePath = Path.Combine(testDataPath, "A1862604.xls");
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"{{\"error\": \"File not found: {filePath}\"}}");
        return;
    }
    var json = ParserPlayground.ExcelFullReportBuilder.BuildAndReturnJson(filePath);
    Console.WriteLine(json);
}

static int DetectHeaderRow(System.Data.DataTable table)
{
    if (table.Rows.Count == 0) return 0;
    for (int r = 0; r < Math.Min(10, table.Rows.Count); r++)
    {
        var row = table.Rows[r];
        int nonEmpty = 0;
        for (int c = 0; c < table.Columns.Count; c++)
        {
            var v = row[c];
            if (v != null && v != DBNull.Value && !string.IsNullOrWhiteSpace(v.ToString())) nonEmpty++;
        }
        if (nonEmpty >= 2) return r;
    }
    return 0;
}

static List<string> GetHeaders(System.Data.DataTable table, int headerRowIndex)
{
    var list = new List<string>();
    var row = table.Rows[headerRowIndex];
    for (int c = 0; c < table.Columns.Count; c++)
    {
        var v = row[c];
        var s = (v != null && v != DBNull.Value) ? v.ToString()?.Trim() ?? "" : "";
        if (string.IsNullOrEmpty(s)) s = $"Column{c + 1}";
        list.Add(s);
    }
    return list;
}

static List<Dictionary<string, object?>> GetFirst20Rows(System.Data.DataTable table, List<string> headers, int dataStartRow)
{
    var result = new List<Dictionary<string, object?>>();
    int count = 0;
    for (int r = dataStartRow; r < table.Rows.Count && count < 20; r++)
    {
        var row = table.Rows[r];
        var dict = new Dictionary<string, object?>();
        for (int c = 0; c < headers.Count && c < table.Columns.Count; c++)
        {
            var v = row[c];
            dict[headers[c]] = (v == null || v == DBNull.Value) ? null : (v is DateTime dt ? dt.ToString("O") : v.ToString());
        }
        result.Add(dict);
        count++;
    }
    return result;
}

static int CountNullOrEmptyRows(System.Data.DataTable table, int dataStartRow, int headerCount)
{
    int empty = 0;
    for (int r = dataStartRow; r < table.Rows.Count; r++)
    {
        var row = table.Rows[r];
        bool allEmpty = true;
        for (int c = 0; c < headerCount && c < table.Columns.Count; c++)
        {
            var v = row[c];
            if (v != null && v != DBNull.Value && !string.IsNullOrWhiteSpace(v.ToString())) { allEmpty = false; break; }
        }
        if (allEmpty) empty++;
    }
    return empty;
}

static Dictionary<string, int> GetColumnStats(System.Data.DataTable table, List<string> headers, int dataStartRow)
{
    var stats = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var h in headers) stats[h] = 0;
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

static List<string> DetectKeyFields(List<string> headers)
{
    var patterns = new[] { "OrderNo", "Order No", "OrderNumber", "CustomerName", "Customer Name", "Customer", "Address", "Address1", "Address2", "Location", "Phone", "Contact", "Mobile", "Tel", "Package", "Plan", "Product", "Devices", "Device", "Equipment", "Serial" };
    var detected = new List<string>();
    foreach (var h in headers)
    {
        var n = h.Trim();
        if (string.IsNullOrEmpty(n)) continue;
        foreach (var p in patterns)
        {
            if (n.Equals(p, StringComparison.OrdinalIgnoreCase) || n.Contains(p, StringComparison.OrdinalIgnoreCase))
            { detected.Add(h); break; }
        }
    }
    return detected;
}

// Extract materials using same regions as Syncfusion parser (ExcelDataReader: Col K = index 9, M = 12, O = 14)
static List<object> ExtractMaterialsFromDataTable(System.Data.DataTable table)
{
    var list = new List<object>();
    const int colK = 9, colM = 12, colO = 14;
    if (colK >= table.Columns.Count) return list;

    // Rows 30-50: material names in Col K, ADD/Not Provided in M/O
    for (int row = 30; row <= 50 && row < table.Rows.Count; row++)
    {
        var name = table.Rows[row][colK]?.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name)) continue;
        var addOrQty = colM < table.Columns.Count ? table.Rows[row][colM]?.ToString()?.Trim() : null;
        var notProvided = colO < table.Columns.Count ? table.Rows[row][colO]?.ToString()?.Trim() : null;
        list.Add(new { RowIndex = row + 1, Name = name, ADD_or_Qty = addOrQty, NotProvided = notProvided });
    }
    // Rows 53-65: material + quantity
    for (int row = 53; row <= 65 && row < table.Rows.Count; row++)
    {
        var name = table.Rows[row][colK]?.ToString()?.Trim() ?? "";
        var qty = colM < table.Columns.Count ? table.Rows[row][colM]?.ToString()?.Trim() : null;
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(qty ?? "")) continue;
        list.Add(new { RowIndex = row + 1, Name = name, Quantity = qty });
    }
    return list;
}

static async Task RunCelcomTestAsync()
{
    // Register Syncfusion license
    var syncfusionLicenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
        ?? "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1edHVUR2BcVkVzWEBWYEg=";
    try
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
        Console.WriteLine("✅ Syncfusion license registered");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Syncfusion license registration failed: {ex.Message}");
    }

    // Setup logging
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(LogLevel.Information)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
    });

    // Create parser service
    var parserLogger = loggerFactory.CreateLogger<CephasOps.Application.Parser.Services.SyncfusionExcelParserService>();
    var parserService = new CephasOps.Application.Parser.Services.SyncfusionExcelParserService(parserLogger);

    // Create tester
    var testerLogger = loggerFactory.CreateLogger<ParserPlayground.CelcomParserTester>();
    var tester = new ParserPlayground.CelcomParserTester(parserService, testerLogger);

    // Find test-data directory
    var possibleTestDataPaths = new[]
    {
        Path.Combine(@"C:\Projects\CephasOps\backend\test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "test-data"),
        Path.Combine(Directory.GetCurrentDirectory(), "test-data"),
        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "..", "..", "..", "test-data")
    };

    string? testDataPath = null;
    foreach (var path in possibleTestDataPaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            testDataPath = fullPath;
            break;
        }
    }

    if (testDataPath == null || !Directory.Exists(testDataPath))
    {
        Console.WriteLine($"❌ Test-data directory not found!");
        Console.WriteLine($"\nTried paths:");
        foreach (var path in possibleTestDataPaths)
        {
            Console.WriteLine($"   - {Path.GetFullPath(path)}");
        }
        return;
    }

    Console.WriteLine($"✅ Found test-data directory: {testDataPath}\n");

    // Test Celcom file
    var celcomFile = Path.Combine(testDataPath, "Celcom Partner (Activation).xls");
    if (!File.Exists(celcomFile))
    {
        Console.WriteLine($"❌ Celcom test file not found: {celcomFile}");
        return;
    }

    await tester.TestCelcomFileAsync(celcomFile);

    Console.WriteLine($"\n{new string('=', 100)}");
    Console.WriteLine("TESTING COMPLETE");
    Console.WriteLine($"{new string('=', 100)}\n");
}

static async Task RunDigiTestAsync()
{
    // Register Syncfusion license
    var syncfusionLicenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
        ?? "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1edHVUR2BcVkVzWEBWYEg=";
    try
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
        Console.WriteLine("✅ Syncfusion license registered");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Syncfusion license registration failed: {ex.Message}");
    }

    // Setup logging
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(LogLevel.Information)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
    });

    // Create parser service
    var parserLogger = loggerFactory.CreateLogger<CephasOps.Application.Parser.Services.SyncfusionExcelParserService>();
    var parserService = new CephasOps.Application.Parser.Services.SyncfusionExcelParserService(parserLogger);

    // Create tester
    var testerLogger = loggerFactory.CreateLogger<ParserPlayground.CelcomParserTester>();
    var tester = new ParserPlayground.CelcomParserTester(parserService, testerLogger);

    // Find test-data directory
    var possibleTestDataPaths = new[]
    {
        Path.Combine(@"C:\Projects\CephasOps\backend\test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "test-data"),
        Path.Combine(Directory.GetCurrentDirectory(), "test-data"),
        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "..", "..", "..", "test-data")
    };

    string? testDataPath = null;
    foreach (var path in possibleTestDataPaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            testDataPath = fullPath;
            break;
        }
    }

    if (testDataPath == null || !Directory.Exists(testDataPath))
    {
        Console.WriteLine($"❌ Test-data directory not found!");
        Console.WriteLine($"\nTried paths:");
        foreach (var path in possibleTestDataPaths)
        {
            Console.WriteLine($"   - {Path.GetFullPath(path)}");
        }
        return;
    }

    Console.WriteLine($"✅ Found test-data directory: {testDataPath}\n");

    // Test Digi file
    var digiFile = Path.Combine(testDataPath, "DIGI Partner (Activation - Reschedule).xls");
    if (!File.Exists(digiFile))
    {
        Console.WriteLine($"❌ Digi test file not found: {digiFile}");
        return;
    }

    await tester.TestCelcomFileAsync(digiFile);

    Console.WriteLine($"\n{new string('=', 100)}");
    Console.WriteLine("TESTING COMPLETE");
    Console.WriteLine($"{new string('=', 100)}\n");
}

static async Task RunAssuranceTestAsync()
{
    // Register Syncfusion license (optional, but good to have)
    var syncfusionLicenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
        ?? "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1edHVUR2BcVkVzWEBWYEg=";
    try
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
    }
    catch
    {
        // Ignore if license registration fails
    }

    // Setup logging
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(LogLevel.Information)
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
    });

    // Create services
    var pdfExtractionLogger = loggerFactory.CreateLogger<CephasOps.Application.Parser.Services.PdfTextExtractionService>();
    var pdfParserLogger = loggerFactory.CreateLogger<CephasOps.Application.Parser.Services.PdfOrderParserService>();

    var pdfTextExtractionService = new CephasOps.Application.Parser.Services.PdfTextExtractionService(pdfExtractionLogger);
    var pdfOrderParserService = new CephasOps.Application.Parser.Services.PdfOrderParserService(pdfParserLogger);
    var logger = loggerFactory.CreateLogger<ParserPlayground.AssuranceFileParser>();

    // Create parser instance
    var parser = new ParserPlayground.AssuranceFileParser(
        pdfTextExtractionService,
        pdfOrderParserService,
        logger);

    // Find test-data directory
    var possibleTestDataPaths = new[]
    {
        Path.Combine(@"C:\Projects\CephasOps\backend\test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test-data"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "test-data"),
        Path.Combine(Directory.GetCurrentDirectory(), "test-data"),
        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "..", "..", "..", "test-data")
    };

    string? testDataPath = null;
    foreach (var path in possibleTestDataPaths)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            testDataPath = fullPath;
            break;
        }
    }

    if (testDataPath == null || !Directory.Exists(testDataPath))
    {
        Console.WriteLine($"❌ Test-data directory not found!");
        Console.WriteLine($"\nTried paths:");
        foreach (var path in possibleTestDataPaths)
        {
            Console.WriteLine($"   - {Path.GetFullPath(path)}");
        }
        return;
    }

    Console.WriteLine($"✅ Found test-data directory: {testDataPath}\n");

    // Parse assurance files
    await parser.ParseAssuranceFilesAsync(testDataPath);

    Console.WriteLine($"\n{new string('=', 100)}");
    Console.WriteLine("PARSING COMPLETE");
    Console.WriteLine($"{new string('=', 100)}\n");
}

