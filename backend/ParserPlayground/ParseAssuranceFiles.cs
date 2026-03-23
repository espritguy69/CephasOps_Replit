using CephasOps.Application.Parser.Services;
using CephasOps.Application.Parser.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using MsgReader.Outlook;
using System.Text;
using System.Text.Json;

namespace ParserPlayground;

/// <summary>
/// Script to parse assurance files (.msg and .pdf) and extract:
/// - Appointment date and time
/// - Issue descriptions
/// - Remarks
/// - All URLs (including setappt URL)
/// </summary>
public class AssuranceFileParser
{
    private readonly IPdfTextExtractionService _pdfTextExtractionService;
    private readonly IPdfOrderParserService _pdfOrderParserService;
    private readonly ILogger _logger;

    public AssuranceFileParser(
        IPdfTextExtractionService pdfTextExtractionService,
        IPdfOrderParserService pdfOrderParserService,
        ILogger logger)
    {
        _pdfTextExtractionService = pdfTextExtractionService;
        _pdfOrderParserService = pdfOrderParserService;
        _logger = logger;
    }

    public async Task ParseAssuranceFilesAsync(string testDataPath)
    {
        // File paths
        var msgFilePath = Path.Combine(testDataPath, 
            "APPMT - CEPHAS TRADING  SERVICESTBBNA749296GWANG YAODONGTTKT202512178631842AWO444622.msg");
        var pdfFilePath = Path.Combine(testDataPath, 
            "APPMT - _CEPHAS TRADING & SERVICES__TBBNA261593G__Chow Yu Yang__TTKT202511138603863__AWO437884_.pdf");

        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine("PARSING ASSURANCE FILES");
        Console.WriteLine($"{new string('=', 100)}\n");

        // Parse .msg file
        if (File.Exists(msgFilePath))
        {
            await ParseMsgFileAsync(msgFilePath);
        }
        else
        {
            Console.WriteLine($"❌ MSG file not found: {msgFilePath}");
        }

        // Parse .pdf file
        if (File.Exists(pdfFilePath))
        {
            await ParsePdfFileAsync(pdfFilePath);
        }
        else
        {
            Console.WriteLine($"❌ PDF file not found: {pdfFilePath}");
        }
    }

    private async Task ParseMsgFileAsync(string msgFilePath)
    {
        Console.WriteLine($"\n{new string('-', 100)}");
        Console.WriteLine($"PARSING .MSG FILE: {Path.GetFileName(msgFilePath)}");
        Console.WriteLine($"{new string('-', 100)}\n");

        try
        {
            // Read MSG file
            var msgBytes = await File.ReadAllBytesAsync(msgFilePath);
            using var msgStream = new MemoryStream(msgBytes);
            
            // Parse MSG using MsgReader
            using var message = new Storage.Message(msgStream);
            
            Console.WriteLine($"📧 Email Information:");
            Console.WriteLine($"   Subject: {message.Subject ?? "(empty)"}");
            Console.WriteLine($"   From: {message.Sender?.Email ?? message.Sender?.DisplayName ?? "(empty)"}");
            Console.WriteLine($"   Sent: {message.SentOn:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   Attachments: {message.Attachments?.Count ?? 0}");

            // Extract email body - MsgReader Storage.Message properties
            string emailBody = "";
            try
            {
                // Try BodyText property (standard MsgReader property)
                // Using reflection as fallback in case property names differ
                var bodyTextProp = message.GetType().GetProperty("BodyText");
                if (bodyTextProp != null)
                {
                    emailBody = bodyTextProp.GetValue(message)?.ToString() ?? "";
                }
                
                // Fallback: Try BodyHtml and strip HTML
                if (string.IsNullOrWhiteSpace(emailBody))
                {
                    var bodyHtmlProp = message.GetType().GetProperty("BodyHtml");
                    if (bodyHtmlProp != null)
                    {
                        var htmlBody = bodyHtmlProp.GetValue(message)?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(htmlBody))
                        {
                            // Strip HTML tags for parsing
                            emailBody = System.Text.RegularExpressions.Regex.Replace(htmlBody, "<.*?>", " ")
                                .Replace("&nbsp;", " ")
                                .Replace("&amp;", "&")
                                .Replace("&lt;", "<")
                                .Replace("&gt;", ">")
                                .Replace("&quot;", "\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Could not extract email body using reflection: {ex.Message}");
                Console.WriteLine($"   Trying alternative method...");
                
                // Last resort: Try to extract from raw message if possible
                // For now, we'll just note that body extraction failed
            }
            
            var emailSubject = message.Subject ?? "";

            Console.WriteLine($"\n📄 Email Body (first 500 chars):");
            if (!string.IsNullOrEmpty(emailBody))
            {
                Console.WriteLine($"   {emailBody.Substring(0, Math.Min(500, emailBody.Length))}...");
            }
            else
            {
                Console.WriteLine($"   (body extraction failed - trying alternative method)");
            }
            
            // Combine subject and body for parsing (parser expects this format)
            var fullText = $"{emailSubject} {emailBody}";
            
            // Parse using PdfOrderParserService (it can parse text from any source)
            var parsedData = _pdfOrderParserService.ParseFromText(fullText, Path.GetFileName(msgFilePath));
            
            // Display parsed results
            DisplayParsedResults(parsedData, "MSG File");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing MSG file: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }

    private async Task ParsePdfFileAsync(string pdfFilePath)
    {
        Console.WriteLine($"\n{new string('-', 100)}");
        Console.WriteLine($"PARSING .PDF FILE: {Path.GetFileName(pdfFilePath)}");
        Console.WriteLine($"{new string('-', 100)}\n");

        try
        {
            // Read PDF file
            var pdfBytes = await File.ReadAllBytesAsync(pdfFilePath);
            
            // Extract text from PDF
            Console.WriteLine("📄 Extracting text from PDF...");
            var extractedText = await _pdfTextExtractionService.ExtractTextFromBytesAsync(pdfBytes);
            
            Console.WriteLine($"   Extracted {extractedText.Length} characters");
            Console.WriteLine($"\n📄 PDF Text (first 500 chars):");
            Console.WriteLine($"   {extractedText.Substring(0, Math.Min(500, extractedText.Length))}...");
            
            // Parse using PdfOrderParserService
            var parsedData = _pdfOrderParserService.ParseFromText(extractedText, Path.GetFileName(pdfFilePath));
            
            // Display parsed results
            DisplayParsedResults(parsedData, "PDF File");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing PDF file: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }

    private void DisplayParsedResults(ParsedOrderData parsedData, string sourceType)
    {
        Console.WriteLine($"\n{new string('=', 100)}");
        Console.WriteLine($"PARSED RESULTS - {sourceType}");
        Console.WriteLine($"{new string('=', 100)}\n");

        // Basic Information
        Console.WriteLine("📋 Basic Information:");
        Console.WriteLine($"   Service ID: {parsedData.ServiceId ?? "(empty)"}");
        Console.WriteLine($"   Ticket ID (TTKT): {parsedData.TicketId ?? "(empty)"}");
        Console.WriteLine($"   AWO Number: {parsedData.AwoNumber ?? "(empty)"}");
        Console.WriteLine($"   Customer Name: {parsedData.CustomerName ?? "(empty)"}");
        Console.WriteLine($"   Order Type: {parsedData.OrderTypeCode ?? "(empty)"}");
        Console.WriteLine($"   Partner Code: {parsedData.PartnerCode ?? "(empty)"}");

        // Appointment Date & Time
        Console.WriteLine($"\n📅 Appointment Date & Time:");
        if (parsedData.AppointmentDateTime.HasValue)
        {
            Console.WriteLine($"   Date: {parsedData.AppointmentDateTime.Value:yyyy-MM-dd}");
            Console.WriteLine($"   DateTime: {parsedData.AppointmentDateTime.Value:yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            Console.WriteLine($"   Date: (not extracted)");
        }
        Console.WriteLine($"   Window: {parsedData.AppointmentWindow ?? "(empty)"}");

        // Extract Issue Description from Remarks
        Console.WriteLine($"\n🔍 Issue Description:");
        var issueDescription = ExtractIssueFromRemarks(parsedData.Remarks);
        if (!string.IsNullOrEmpty(issueDescription))
        {
            Console.WriteLine($"   {issueDescription}");
        }
        else
        {
            Console.WriteLine($"   (not found in remarks)");
        }

        // Extract URLs from Remarks
        Console.WriteLine($"\n🔗 URLs:");
        var urls = ExtractUrlsFromRemarks(parsedData.Remarks);
        if (urls.Any())
        {
            foreach (var url in urls)
            {
                var urlType = DetermineUrlType(url);
                Console.WriteLine($"   [{urlType}] {url}");
            }
        }
        else
        {
            Console.WriteLine($"   (no URLs found)");
        }

        // Full Remarks
        Console.WriteLine($"\n📝 Full Remarks:");
        if (!string.IsNullOrEmpty(parsedData.Remarks))
        {
            // Split by | for better readability
            var remarksParts = parsedData.Remarks.Split('|', StringSplitOptions.TrimEntries);
            foreach (var part in remarksParts)
            {
                Console.WriteLine($"   • {part}");
            }
        }
        else
        {
            Console.WriteLine($"   (empty)");
        }

        // Other fields
        Console.WriteLine($"\n📧 Contact Information:");
        Console.WriteLine($"   Phone: {parsedData.CustomerPhone ?? "(empty)"}");
        Console.WriteLine($"   Email: {parsedData.CustomerEmail ?? "(empty)"}");
        
        Console.WriteLine($"\n📍 Address:");
        Console.WriteLine($"   {parsedData.ServiceAddress ?? "(empty)"}");

        Console.WriteLine($"\n🎯 Confidence Score: {parsedData.ConfidenceScore:P1}");
    }

    private string ExtractIssueFromRemarks(string? remarks)
    {
        if (string.IsNullOrEmpty(remarks))
            return string.Empty;

        // Look for "Issue:" pattern
        var issueMatch = System.Text.RegularExpressions.Regex.Match(
            remarks, 
            @"Issue[:\s]+([^|]+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (issueMatch.Success && issueMatch.Groups.Count > 1)
        {
            return issueMatch.Groups[1].Value.Trim();
        }

        return string.Empty;
    }

    private List<string> ExtractUrlsFromRemarks(string? remarks)
    {
        var urls = new List<string>();
        
        if (string.IsNullOrEmpty(remarks))
            return urls;

        // Extract URLs pattern
        var urlPattern = @"(https?://[^\s<>""|]+)";
        var matches = System.Text.RegularExpressions.Regex.Matches(
            remarks, 
            urlPattern, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Success && match.Groups.Count > 1)
            {
                urls.Add(match.Groups[1].Value.Trim());
            }
        }

        return urls.Distinct().ToList();
    }

    private string DetermineUrlType(string url)
    {
        if (url.Contains("setappt", StringComparison.OrdinalIgnoreCase))
            return "APPOINTMENT SETUP";
        
        if (url.Contains("assign", StringComparison.OrdinalIgnoreCase))
            return "ASSIGN SI";
        
        if (url.Contains("docket", StringComparison.OrdinalIgnoreCase))
            return "DOCKET";
        
        if (url.Contains("workorder", StringComparison.OrdinalIgnoreCase) || 
            url.Contains("work-order", StringComparison.OrdinalIgnoreCase))
            return "WORK ORDER";
        
        return "URL";
    }
}

