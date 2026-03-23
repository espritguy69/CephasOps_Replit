using CephasOps.Application.Parser.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ParserPlayground;

/// <summary>
/// Test harness for Celcom Excel parser
/// </summary>
public class CelcomParserTester
{
    private readonly ISyncfusionExcelParserService _parserService;
    private readonly ILogger<CelcomParserTester> _logger;

    public CelcomParserTester(
        ISyncfusionExcelParserService parserService,
        ILogger<CelcomParserTester> logger)
    {
        _parserService = parserService;
        _logger = logger;
    }

    public async Task TestCelcomFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ File not found: {filePath}");
            return;
        }

        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine($"TESTING: {Path.GetFileName(filePath)}");
        Console.WriteLine($"{new string('=', 100)}\n");

        try
        {
            // Read file as IFormFile
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            
            using var stream = new MemoryStream(fileBytes);
            var formFile = new FormFile(stream, 0, fileBytes.Length, fileName, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.ms-excel"
            };

            // Parse the file
            Console.WriteLine($"📋 Parsing file: {fileName}...\n");
            var result = await _parserService.ParseAsync(formFile);

            if (result.Success && result.OrderData != null)
            {
                var data = result.OrderData;
                
                Console.WriteLine($"✅ PARSING SUCCESSFUL\n");
                Console.WriteLine($"📊 CONFIDENCE SCORE: {data.ConfidenceScore:P0}\n");
                
                Console.WriteLine($"📋 EXTRACTED DATA:\n");
                Console.WriteLine($"   Partner Code: {data.PartnerCode ?? "(null)"}");
                Console.WriteLine($"   Order Type: {data.OrderTypeCode ?? "(null)"} ({data.OrderTypeHint ?? "(null)"})");
                Console.WriteLine($"   Service ID: {data.ServiceId ?? "(null)"}");
                Console.WriteLine($"   Customer Name: {data.CustomerName ?? "(null)"}");
                Console.WriteLine($"   Customer Phone: {data.CustomerPhone ?? "(null)"}");
                Console.WriteLine($"   Customer Email: {data.CustomerEmail ?? "(null)"}");
                Console.WriteLine($"   Service Address: {data.ServiceAddress ?? "(null)"}");
                Console.WriteLine($"   Appointment Date/Time: {data.AppointmentDateTime?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "(null)"}");
                
                Console.WriteLine($"\n🔐 NETWORK CREDENTIALS:\n");
                Console.WriteLine($"   LOGIN ID: {data.Username ?? "(null) ⚠️ MISSING"}");
                Console.WriteLine($"   PASSWORD: {(string.IsNullOrEmpty(data.Password) ? "(null) ⚠️ MISSING" : "***")}");
                Console.WriteLine($"   ONU PASSWORD: {(string.IsNullOrEmpty(data.OnuPassword) ? "(null) ⚠️ MISSING" : "***")}");
                
                Console.WriteLine($"\n📦 PACKAGE INFO:\n");
                Console.WriteLine($"   Package Name: {data.PackageName ?? "(null)"}");
                Console.WriteLine($"   Bandwidth: {data.Bandwidth ?? "(null)"}");
                
                Console.WriteLine($"\n🌐 NETWORK IPs:\n");
                Console.WriteLine($"   WAN IP: {data.InternetWanIp ?? "(null)"}");
                Console.WriteLine($"   LAN IP: {data.InternetLanIp ?? "(null)"}");
                Console.WriteLine($"   Gateway: {data.InternetGateway ?? "(null)"}");
                Console.WriteLine($"   Subnet Mask: {data.InternetSubnetMask ?? "(null)"}");
                
                Console.WriteLine($"\n📝 REMARKS:\n");
                if (!string.IsNullOrEmpty(data.Remarks))
                {
                    var remarksLines = data.Remarks.Split('\n');
                    foreach (var line in remarksLines)
                    {
                        Console.WriteLine($"   {line}");
                    }
                }
                else
                {
                    Console.WriteLine($"   (empty)");
                }
                
                Console.WriteLine($"\n📦 MATERIALS ({data.Materials?.Count ?? 0} items):\n");
                if (data.Materials != null && data.Materials.Any())
                {
                    foreach (var material in data.Materials)
                    {
                        Console.WriteLine($"   - {material.Name} (Qty: {material.Quantity}, Required: {material.IsRequired})");
                        if (!string.IsNullOrEmpty(material.Notes))
                        {
                            Console.WriteLine($"     Notes: {material.Notes}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"   (no materials extracted)");
                }
                
                Console.WriteLine($"\n📍 SPLITTER LOCATION:\n");
                Console.WriteLine($"   {data.SplitterLocation ?? "(null)"}");
                
                // Validation summary
                Console.WriteLine($"\n{new string('=', 100)}");
                Console.WriteLine($"VALIDATION SUMMARY");
                Console.WriteLine($"{new string('=', 100)}\n");
                
                var missingFields = new List<string>();
                if (string.IsNullOrEmpty(data.ServiceId)) missingFields.Add("Service ID");
                if (string.IsNullOrEmpty(data.CustomerName)) missingFields.Add("Customer Name");
                if (string.IsNullOrEmpty(data.CustomerPhone)) missingFields.Add("Customer Phone");
                if (string.IsNullOrEmpty(data.ServiceAddress)) missingFields.Add("Service Address");
                if (!data.AppointmentDateTime.HasValue) missingFields.Add("Appointment Date/Time");
                if (string.IsNullOrEmpty(data.Username)) missingFields.Add("LOGIN ID ⚠️");
                if (string.IsNullOrEmpty(data.Password)) missingFields.Add("PASSWORD ⚠️");
                if (string.IsNullOrEmpty(data.OnuPassword)) missingFields.Add("ONU PASSWORD ⚠️");
                
                if (missingFields.Any())
                {
                    Console.WriteLine($"⚠️  MISSING FIELDS ({missingFields.Count}):");
                    foreach (var field in missingFields)
                    {
                        Console.WriteLine($"   - {field}");
                    }
                }
                else
                {
                    Console.WriteLine($"✅ All critical fields extracted!");
                }
                
                if (result.ValidationErrors != null && result.ValidationErrors.Any())
                {
                    Console.WriteLine($"\n❌ VALIDATION ERRORS ({result.ValidationErrors.Count}):");
                    foreach (var error in result.ValidationErrors)
                    {
                        Console.WriteLine($"   - {error}");
                    }
                }
                else
                {
                    Console.WriteLine($"\n✅ No validation errors");
                }
            }
            else
            {
                Console.WriteLine($"❌ PARSING FAILED\n");
                if (result.ValidationErrors != null && result.ValidationErrors.Any())
                {
                    Console.WriteLine($"Validation Errors:");
                    foreach (var error in result.ValidationErrors)
                    {
                        Console.WriteLine($"   - {error}");
                    }
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"Error: {result.ErrorMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ EXCEPTION: {ex.Message}");
            Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
        }
    }
}

