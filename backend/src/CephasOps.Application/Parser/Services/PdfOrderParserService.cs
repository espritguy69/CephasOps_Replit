using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Utilities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service to parse order data from PDF text using pattern matching
/// </summary>
public interface IPdfOrderParserService
{
    /// <summary>
    /// Parse order data from PDF text content
    /// </summary>
    ParsedOrderData ParseFromText(string pdfText, string fileName);
}

public class PdfOrderParserService : IPdfOrderParserService
{
    private readonly ILogger<PdfOrderParserService> _logger;

    public PdfOrderParserService(ILogger<PdfOrderParserService> logger)
    {
        _logger = logger;
    }

    public ParsedOrderData ParseFromText(string pdfText, string fileName)
    {
        var data = new ParsedOrderData
        {
            SourceFileName = fileName,
            PartnerCode = "TIME",
            ConfidenceScore = 0.7m // Start with moderate confidence for PDF parsing
        };

        if (string.IsNullOrWhiteSpace(pdfText))
        {
            _logger.LogWarning("PDF text is empty for file: {FileName}", fileName);
            return data;
        }

        // Normalize text - remove extra whitespace
        var normalizedText = Regex.Replace(pdfText, @"\s+", " ");

        // Extract Service ID (TBBN)
        data.ServiceId = ExtractServiceId(normalizedText);

        // Extract Customer Name
        data.CustomerName = ExtractCustomerName(normalizedText);

        // Extract Contact Number
        data.CustomerPhone = NormalizePhone(ExtractPhoneNumber(normalizedText));

        // ✅ Extract Additional Contact Number for Assurance orders
        data.AdditionalContactNumber = NormalizePhone(ExtractAdditionalContactNumber(normalizedText));

        // Extract Email
        data.CustomerEmail = ExtractEmail(normalizedText);

        // Extract Service Address
        data.ServiceAddress = ExtractServiceAddress(normalizedText);

        // Extract Old Address (for modifications)
        data.OldAddress = ExtractOldAddress(normalizedText);

        // Extract Appointment Date & Time
        var appointment = ExtractAppointmentDateTime(normalizedText);
        data.AppointmentDateTime = appointment.Date;
        data.AppointmentWindow = appointment.Window;

        // Extract Package/Bandwidth
        data.PackageName = ExtractPackageName(normalizedText);
        data.Bandwidth = ExtractBandwidth(normalizedText);

        // Extract ONU Serial Number
        data.OnuSerialNumber = ExtractOnuSerial(normalizedText);

        // Extract VOIP Service ID
        data.VoipServiceId = ExtractVoipServiceId(normalizedText);

        // ✅ Extract Username and Password from Internet details
        data.Username = ExtractUsername(normalizedText);
        data.Password = ExtractPassword(normalizedText);

        // ✅ Extract Last Fault Date (for warranty checking)
        data.LastFaultDate = ExtractLastFaultDate(normalizedText);

        // ✅ Extract TTKT (Ticket ID) for Assurance orders
        data.TicketId = ExtractTicketId(normalizedText);

        // ✅ Extract AWO Number for Assurance orders
        var awoNumber = ExtractAwoNumber(normalizedText, fileName);
        data.AwoNumber = awoNumber; // Store in property, not just remarks
        
        // ✅ Extract Issue Description (LOSi, LOBi, etc.)
        var issueDescription = ExtractIssueDescription(normalizedText);
        data.Issue = issueDescription; // Store in dedicated field
        
        // ✅ Extract Equipment Model
        var equipmentModel = ExtractEquipmentModel(normalizedText);
        
        // ✅ Extract URLs (Work Order, Assign SI, Docket)
        var urls = ExtractUrls(normalizedText);
        
        // ✅ Extract MRA (Material Return Advice) specific fields
        var materialDocNumber = ExtractMaterialDocNumber(normalizedText);
        var sapMaterialCode = ExtractSapMaterialCode(normalizedText);
        var materialDescription = ExtractMaterialDescription(normalizedText);
        var submissionNumber = ExtractSubmissionNumber(normalizedText);

        // Build remarks with assurance-specific info if applicable
        if (!string.IsNullOrEmpty(data.TicketId) || !string.IsNullOrEmpty(awoNumber) || !string.IsNullOrEmpty(materialDocNumber))
        {
            var remarksParts = new List<string>();
            
            if (!string.IsNullOrEmpty(awoNumber))
            {
                remarksParts.Add($"AWO: {awoNumber}");
            }
            
            if (!string.IsNullOrEmpty(issueDescription))
            {
                remarksParts.Add($"Issue: {issueDescription}");
            }
            
            if (!string.IsNullOrEmpty(equipmentModel))
            {
                remarksParts.Add($"Equipment: {equipmentModel}");
            }
            
            // ✅ Extract and add Splitter Name to remarks (their naming convention differs)
            var splitterName = ExtractSplitterName(normalizedText);
            if (!string.IsNullOrEmpty(splitterName))
            {
                remarksParts.Add($"Splitter Name: {splitterName}");
            }
            
            // ✅ Add Last Fault Date to remarks with warranty note
            if (data.LastFaultDate.HasValue)
            {
                var daysSinceFault = (DateTime.UtcNow - data.LastFaultDate.Value).TotalDays;
                var warrantyNote = daysSinceFault <= 30 
                    ? "⚠️ Within 30 days - Customer might not pay" 
                    : "Outside 30 days";
                remarksParts.Add($"Last Fault Date: {data.LastFaultDate.Value:yyyy-MM-dd HH:mm:ss} ({warrantyNote})");
            }
            
            // MRA-specific information
            if (!string.IsNullOrEmpty(materialDocNumber))
            {
                remarksParts.Add($"Material Doc #: {materialDocNumber}");
            }
            
            if (!string.IsNullOrEmpty(submissionNumber))
            {
                remarksParts.Add($"Submission No: {submissionNumber}");
            }
            
            if (!string.IsNullOrEmpty(sapMaterialCode))
            {
                remarksParts.Add($"SAP Material Code: {sapMaterialCode}");
            }
            
            if (!string.IsNullOrEmpty(materialDescription))
            {
                remarksParts.Add($"Material: {materialDescription}");
            }
            
            if (urls.Any())
            {
                remarksParts.Add($"URLs: {string.Join(", ", urls)}");
            }
            
            if (!string.IsNullOrEmpty(data.Remarks))
            {
                remarksParts.Add(data.Remarks);
            }
            
            data.Remarks = remarksParts.Any() ? string.Join(" | ", remarksParts) : data.Remarks;
        }

        // Detect Order Type (including Assurance)
        data.OrderTypeCode = DetectOrderTypeFromText(normalizedText, fileName);
        data.OrderTypeHint = data.OrderTypeCode.Replace("_", " ");
        
        // ✅ Set PartnerCode based on order type
        if (data.OrderTypeCode == "ASSURANCE")
        {
            data.PartnerCode = "TIME-ASSURANCE";
        }
        // PartnerCode defaults to "TIME" (set in initializer) for other order types

        // Calculate confidence based on extracted fields
        data.ConfidenceScore = CalculateConfidence(data);
        
        _logger.LogInformation("PDF parsed: ServiceId={ServiceId}, OrderType={OrderType}, PartnerCode={PartnerCode}, Customer={Customer}, Date={Date}, Confidence={Confidence}",
            data.ServiceId, data.OrderTypeCode, data.PartnerCode, data.CustomerName, data.AppointmentDateTime, data.ConfidenceScore);

        _logger.LogInformation("PDF parsed: ServiceId={ServiceId}, Customer={Customer}, Date={Date}, Confidence={Confidence}",
            data.ServiceId, data.CustomerName, data.AppointmentDateTime, data.ConfidenceScore);

        return data;
    }

    private string? ExtractServiceId(string text)
    {
        // Pattern: TBBN followed by digits and optional letter (e.g., TBBN1234567G, TBBNA123456G)
        var patterns = new[]
        {
            @"TBBN[A-Z]?\d{5,}[A-Z]?",           // TBBN1234567G, TBBNA123456G
            @"TBBN\s*[A-Z]?\s*\d{5,}\s*[A-Z]?", // TBBN 1234567 G (with spaces)
            @"Service\s*ID[:\s]+(TBBN[A-Z]?\d{5,}[A-Z]?)", // Service ID: TBBN1234567G
            @"TBBN\s*No[:\s]+([A-Z]?\d{5,}[A-Z]?)" // TBBN No: 1234567G
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var serviceId = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                return serviceId.Trim().Replace(" ", ""); // Remove spaces
            }
        }

        return null;
    }

    private string? ExtractCustomerName(string text)
    {
        var patterns = new[]
        {
            @"Customer\s*Name[:\s]+([A-Za-z\s]+?)(?:\n|Customer|Contact|Service|Address|$)",
            @"Name[:\s]+([A-Za-z\s]{3,50})(?:\n|Customer|Contact|Service|Address|$)",
            @"NAMA\s*PELANGGAN[:\s]+([A-Za-z\s]+?)(?:\n|Customer|Contact|Service|Address|$)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var name = match.Groups[1].Value.Trim();
                if (name.Length >= 3 && name.Length <= 100)
                    return name;
            }
        }

        return null;
    }

    private string? ExtractPhoneNumber(string text)
    {
        // Malaysian phone patterns
        var patterns = new[]
        {
            @"(\+?60?1[0-9]{1,2}[-\s]?[0-9]{3,4}[-\s]?[0-9]{3,4})", // +60123456789, 012-345-6789
            @"(0?1[0-9]{1,2}[-\s]?[0-9]{3,4}[-\s]?[0-9]{3,4})",     // 0123456789, 012-345-6789
            @"Contact\s*(?:No|Number)[:\s]+([0-9\s\-+]+)",
            @"Phone[:\s]+([0-9\s\-+]+)",
            @"H/P[:\s]+([0-9\s\-+]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var phone = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                phone = Regex.Replace(phone, @"[^\d+]", ""); // Keep only digits and +
                if (phone.Length >= 9 && phone.Length <= 13)
                    return phone;
            }
        }

        return null;
    }

    private string? ExtractEmail(string text)
    {
        var emailPattern = @"([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})";
        var match = Regex.Match(text, emailPattern);
        return match.Success ? match.Value : null;
    }

    private string? ExtractServiceAddress(string text)
    {
        // ✅ PRIORITY 1: Label-based extraction (more specific patterns first)
        // Stop at assurance-specific keywords and country endings
        var labelPatterns = new[]
        {
            @"Service\s*Address[:\s]+(.+?)(?:\n\s*\n|Old\s*Address|APPOINT|Appointment|Customer\s*Name|Service\s*ID|TTKT|AWO|Splitter|Current\s*ONU|Issue[:\s]|Action\s*summary|Additional\s*contact|Remarks[:\s]|MALAYSIA\.|$)",
            @"NEW\s*ADDRESS[:\s]+(.+?)(?:\n\s*\n|Old\s*Address|APPOINT|Appointment|Customer\s*Name|Service\s*ID|TTKT|AWO|Splitter|Current\s*ONU|Issue[:\s]|Action\s*summary|Additional\s*contact|Remarks[:\s]|MALAYSIA\.|$)",
            @"Installation\s*Address[:\s]+(.+?)(?:\n\s*\n|Old\s*Address|APPOINT|Appointment|Customer\s*Name|Service\s*ID|TTKT|AWO|Splitter|Current\s*ONU|Issue[:\s]|Action\s*summary|Additional\s*contact|Remarks[:\s]|MALAYSIA\.|$)",
            // Enhanced pattern for "Address:" in Customer Details section - stops at Internet/Voice/Appointment sections
            @"Address[:\s]+(.+?)(?:\n\s*\n|Internet\s*details|Voice\s*details|Appointment\s*details|Old\s*Address|APPOINT|Appointment|Customer\s*Name|Service\s*ID|TTKT|AWO|Splitter|Current\s*ONU|Issue[:\s]|Action\s*summary|Additional\s*contact|Remarks[:\s]|MALAYSIA\.|$)", // Generic "Address:" label
        };

        foreach (var pattern in labelPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                var address = match.Groups[1].Value.Trim();
                // Clean up address - remove extra whitespace and newlines
                address = Regex.Replace(address, @"\s+", " ");
                address = Regex.Replace(address, @"\n+", " ");
                address = address.Trim();
                
                // More lenient length check - allow shorter addresses (minimum 5 chars instead of 10)
                if (address.Length >= 5 && address.Length <= 500)
                    return address;
            }
        }

        // ✅ PRIORITY 2: Try to extract address after common keywords (fallback)
        // Look for address-like text after "Service Address", "Address", etc. even without explicit label
        var contextPatterns = new[]
        {
            @"(?:Service\s*Address|Address|Installation\s*Address)[:\s]*\n(.+?)(?:\n\s*\n|Old\s*Address|APPOINT|Appointment|Customer|Service\s*ID|TTKT|AWO|Splitter|Current\s*ONU|Issue[:\s]|Action\s*summary|Additional\s*contact|Remarks[:\s]|MALAYSIA\.|$)",
        };

        foreach (var pattern in contextPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                var address = match.Groups[1].Value.Trim();
                address = Regex.Replace(address, @"\s+", " ");
                address = Regex.Replace(address, @"\n+", " ");
                address = address.Trim();
                
                // Check if it looks like an address (contains numbers, common address words)
                if (address.Length >= 5 && address.Length <= 500 && 
                    (Regex.IsMatch(address, @"\d") || 
                     Regex.IsMatch(address, @"(?:Street|Road|Jalan|Lorong|Taman|Avenue|Boulevard|Drive|Lane|Way)", RegexOptions.IgnoreCase)))
                {
                    return address;
                }
            }
        }

        return null;
    }

    private string? ExtractOldAddress(string text)
    {
        var patterns = new[]
        {
            @"Old\s*Address[:\s]+(.+?)(?:\n\n|New\s*Address|Service\s*Address|Appointment|Customer|Service\s*ID|$)",
            @"Previous\s*Address[:\s]+(.+?)(?:\n\n|New\s*Address|Service\s*Address|Appointment|Customer|Service\s*ID|$)",
            @"FROM\s*ADDRESS[:\s]+(.+?)(?:\n\n|New\s*Address|Service\s*Address|Appointment|Customer|Service\s*ID|$)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                var address = match.Groups[1].Value.Trim();
                if (address.Length >= 10 && address.Length <= 500)
                    return address;
            }
        }

        return null;
    }

    private (DateTime? Date, string? Window) ExtractAppointmentDateTime(string text)
    {
        DateTime? date = null;
        string? window = null;

        // ✅ PRIORITY 1: Try to extract combined date-time format (e.g., "APPOINT. DATE & TIME: 2025-12-18 10:00:00")
        var combinedPatterns = new[]
        {
            @"(?:APPOINT\.?\s*DATE\s*&?\s*TIME|APPOINTMENT\s*DATE\s*&?\s*TIME|APPOINT\s*DATE\s*&?\s*TIME)[:\s]+(\d{4}[-/]\d{1,2}[-/]\d{1,2}\s+\d{1,2}:\d{2}(?::\d{2})?)", // APPOINT. DATE & TIME: 2025-12-18 10:00:00
            @"(?:APPOINT\.?\s*DATE\s*&?\s*TIME|APPOINTMENT\s*DATE\s*&?\s*TIME|APPOINT\s*DATE\s*&?\s*TIME)[:\s]+(\d{1,2}[-/]\d{1,2}[-/]\d{4}\s+\d{1,2}:\d{2}(?::\d{2})?)", // APPOINT. DATE & TIME: 18-12-2025 10:00:00
            @"(?:APPOINT\.?\s*DATE\s*&?\s*TIME|APPOINTMENT\s*DATE\s*&?\s*TIME)[:\s]+(\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4}\s+\d{1,2}:\d{2}(?:\s*[AP]M)?)", // APPOINT. DATE & TIME: 18 Dec 2025 10:00 AM
        };

        foreach (var pattern in combinedPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var dateTimeStr = match.Groups[1].Value.Trim();
                if (DateTime.TryParse(dateTimeStr, out var parsedDateTime))
                {
                    date = parsedDateTime;
                    // Extract time component for window
                    var timeMatch = Regex.Match(dateTimeStr, @"(\d{1,2}:\d{2}(?::\d{2})?)", RegexOptions.IgnoreCase);
                    if (timeMatch.Success)
                    {
                        var timeStr = timeMatch.Value;
                        if (TimeSpan.TryParse(timeStr, out var time))
                        {
                            var endTime = time.Add(TimeSpan.FromHours(2));
                            window = $"{time:hh\\:mm}-{endTime:hh\\:mm}";
                        }
                        else
                        {
                            window = timeStr;
                        }
                    }
                    return (date, window);
                }
            }
        }

        // ✅ PRIORITY 2: Try to extract separate "Appointment Date:" and "Appointment Time:" labels
        // Add TS Appointment patterns first (higher priority for assurance emails)
        var dateLabelPatterns = new[]
        {
            @"(?:TS\s*APPOINTMENT\s*DATE|TS\s*APPOINT\.?\s*DATE)[:\s]+(\d{4}[-/]\d{1,2}[-/]\d{1,2})", // TS Appointment Date: 2025-12-19
            @"(?:TS\s*APPOINTMENT\s*DATE|TS\s*APPOINT\.?\s*DATE)[:\s]+(\d{1,2}[-/]\d{1,2}[-/]\d{4})", // TS Appointment Date: 19-12-2025
            @"(?:APPOINT\.?\s*DATE|APPOINTMENT\s*DATE|APPOINT\s*DATE|Appointment\s*Date)[:\s]+(\d{4}[-/]\d{1,2}[-/]\d{1,2})", // APPOINT. DATE: 2025-12-18
            @"(?:APPOINT\.?\s*DATE|APPOINTMENT\s*DATE|APPOINT\s*DATE|Appointment\s*Date)[:\s]+(\d{1,2}[-/]\d{1,2}[-/]\d{4})", // APPOINT. DATE: 18-12-2025
            @"(?:APPOINT\.?\s*DATE|APPOINTMENT\s*DATE|APPOINT\s*DATE|Appointment\s*Date)[:\s]+(\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4})", // APPOINT. DATE: 18 Dec 2025
            @"(?:APPOINT\.?\s*DATE|APPOINTMENT\s*DATE|APPOINT\s*DATE|Appointment\s*Date)[:\s]+((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2},?\s+\d{4})", // APPOINT. DATE: Dec 18, 2025
        };

        foreach (var pattern in dateLabelPatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var dateStr = match.Groups[1].Value.Trim();
                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    date = parsedDate;
                    break;
                }
            }
        }

        // Extract time separately if date was found
        if (date.HasValue)
        {
            var timeLabelPatterns = new[]
            {
                @"(?:TS\s*APPOINTMENT\s*TIME|TS\s*APPOINT\.?\s*TIME)[:\s]+(\d{1,2}:\d{2}(?::\d{2})?(?:\s*[AP]M)?)", // TS Appointment Time: 13:00:00
                @"(?:APPOINT\.?\s*TIME|APPOINTMENT\s*TIME|APPOINT\s*TIME|Appointment\s*Time)[:\s]+(\d{1,2}:\d{2}(?::\d{2})?(?:\s*[AP]M)?)", // APPOINT. TIME: 10:00 AM
                @"(?:APPOINT\.?\s*TIME|APPOINTMENT\s*TIME|APPOINT\s*TIME|Appointment\s*Time)[:\s]+(\d{1,2}:\d{2}(?:\s*[AP]M)?)", // APPOINT. TIME: 10:00
            };

            foreach (var pattern in timeLabelPatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var timeStr = match.Groups[1].Value.Trim();
                    if (TimeSpan.TryParse(timeStr, out var time))
                    {
                        var endTime = time.Add(TimeSpan.FromHours(2));
                        window = $"{time:hh\\:mm}-{endTime:hh\\:mm}";
                    }
                    else
                    {
                        window = timeStr;
                    }
                    break;
                }
            }
        }

        // ✅ PRIORITY 3: Fallback to generic date patterns (if label-based extraction failed)
        if (!date.HasValue)
        {
            var datePatterns = new[]
            {
                @"(\d{4}[-/]\d{1,2}[-/]\d{1,2})",                    // 2025-12-01, 2025/12/01
                @"(\d{1,2}[-/]\d{1,2}[-/]\d{4})",                    // 01-12-2025, 01/12/2025
                @"(\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{4})", // 1 Dec 2025
                @"((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2},?\s+\d{4})"  // Dec 1, 2025
            };

            foreach (var pattern in datePatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (DateTime.TryParse(match.Value, out var parsedDate))
                    {
                        date = parsedDate;
                        break;
                    }
                }
            }
        }

        // ✅ PRIORITY 4: Extract time patterns (if not already found)
        if (date.HasValue && string.IsNullOrEmpty(window))
        {
            var timePatterns = new[]
            {
                @"(?:APPOINT\.?\s*TIME|APPOINTMENT\s*TIME|APPOINT\s*TIME|Appointment\s*Time)[:\s]+(\d{1,2}:\d{2}(?:\s*[AP]M)?)", // Appointment Time: 14:30
                @"(\d{1,2}:\d{2}(?:\s*[AP]M)?)",                     // 14:30, 2:30 PM
                @"(\d{1,2}:\d{2}:\d{2})",                            // 14:30:00
            };

            foreach (var pattern in timePatterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var timeStr = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    if (TimeSpan.TryParse(timeStr, out var time))
                    {
                        var endTime = time.Add(TimeSpan.FromHours(2));
                        window = $"{time:hh\\:mm}-{endTime:hh\\:mm}";
                    }
                    else
                    {
                        window = timeStr;
                    }
                    break;
                }
            }
        }

        return (date, window);
    }

    private string? ExtractPackageName(string text)
    {
        var patterns = new[]
        {
            @"Package[:\s]+([A-Za-z0-9\s]+?)(?:\n|Bandwidth|Speed|MBPS|$)",
            @"Plan[:\s]+([A-Za-z0-9\s]+?)(?:\n|Bandwidth|Speed|MBPS|$)",
            @"Product[:\s]+([A-Za-z0-9\s]+?)(?:\n|Bandwidth|Speed|MBPS|$)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var package = match.Groups[1].Value.Trim();
                if (package.Length > 0 && package.Length <= 100)
                    return package;
            }
        }

        return null;
    }

    private string? ExtractBandwidth(string text)
    {
        var pattern = @"(\d+)\s*MBPS?";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? $"{match.Groups[1].Value} Mbps" : null;
    }

    private string? ExtractOnuSerial(string text)
    {
        var patterns = new[]
        {
            @"ONU\s*Serial[:\s]+([A-Z0-9]+)",
            @"Serial\s*Number[:\s]+([A-Z0-9]+)",
            @"S/N[:\s]+([A-Z0-9]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private string? ExtractVoipServiceId(string text)
    {
        var patterns = new[]
        {
            @"VOIP[:\s]+([A-Z0-9]+)",
            @"Voice\s*Service\s*ID[:\s]+([A-Z0-9]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private string DetectOrderTypeFromText(string text, string fileName)
    {
        var textUpper = text.ToUpperInvariant();
        var fileNameUpper = fileName.ToUpperInvariant();

        // ✅ Check for Assurance/TTKT first (before other checks)
        if (fileNameUpper.Contains("APPMT") || 
            fileNameUpper.Contains("TTKT") || 
            textUpper.Contains("TTKT") ||
            textUpper.Contains("ASSURANCE"))
        {
            return "ASSURANCE";
        }

        if (fileNameUpper.StartsWith("M") || textUpper.Contains("MODIFICATION") || textUpper.Contains("RELOCATION"))
        {
            if (textUpper.Contains("OUTDOOR") || textUpper.Contains("OLD ADDRESS"))
                return "MODIFICATION_OUTDOOR";
            return "MODIFICATION_INDOOR";
        }

        if (fileNameUpper.StartsWith("A") || textUpper.Contains("ACTIVATION"))
            return "ACTIVATION";

        if (fileNameUpper.StartsWith("C") || textUpper.Contains("CANCELLATION"))
            return "CANCELLATION";

        return "UNKNOWN";
    }

    /// <summary>
    /// Extract TTKT (Ticket ID) from text
    /// </summary>
    private string? ExtractTicketId(string text)
    {
        var patterns = new[]
        {
            @"TTKT\d+",
            @"TTKT[:\s]+(\d+)",
            @"Ticket\s*ID[:\s]+(TTKT\d+)",
            @"Ticket[:\s]+(TTKT\d+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract AWO Number from text or filename
    /// </summary>
    private string? ExtractAwoNumber(string text, string fileName)
    {
        // Try filename first (e.g., APPMT...__AWO437884_.pdf)
        var fileNameMatch = Regex.Match(fileName, @"AWO(\d+)", RegexOptions.IgnoreCase);
        if (fileNameMatch.Success)
        {
            return fileNameMatch.Groups[1].Value;
        }

        // Try text patterns
        var patterns = new[]
        {
            @"AWO[:\s<>]*(\d+)",
            @"AWO\s*Number[:\s]+(\d+)",
            @"Work\s*Order\s*No[:\s]+(\d+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Issue Description (LOSi, LOBi, Link Down, etc.)
    /// </summary>
    private string? ExtractIssueDescription(string text)
    {
        var patterns = new[]
        {
            // More flexible pattern - matches "Issue:" followed by text until line break or specific keywords
            @"Issue[:\s]+([^\n\r]+?)(?:\s*(?:\n|Equipment|AWO|TTKT|Appointment|Contact|Call|Site|Kindly|Thanks|Remarks|$))",
            // Pattern for multi-line issues (captures until specific stop words)
            @"Issue[:\s]+(.+?)(?=\s*(?:Equipment|AWO|TTKT|Appointment|Contact|Call|Site|Kindly|Thanks|Remarks|Call\s+from|Contact\s+Person|$))",
            // Alternative patterns
            @"Description[:\s]+([^\n\r]+?)(?:\s*(?:\n|Equipment|AWO|TTKT|Appointment|Contact|Call|Site|Kindly|Thanks|Remarks|$))",
            @"Problem[:\s]+([^\n\r]+?)(?:\s*(?:\n|Equipment|AWO|TTKT|Appointment|Contact|Call|Site|Kindly|Thanks|Remarks|$))",
            // Specific issue types
            @"(LOSi|LOBi)",
            @"(Link\s+Down[^\n\r]*)",
            @"(Connected\s+but\s+unable\s+to\s+browse[^\n\r]*)",
            @"(Unable\s+to\s+browse[^\n\r]*)",
            @"(No\s+internet[^\n\r]*)",
            @"(No\s+signal[^\n\r]*)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                // For patterns with capture groups, use group 1, otherwise use the whole match
                var issue = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                issue = issue.Trim();
                
                // Clean up common trailing characters
                issue = Regex.Replace(issue, @"[,\s]+$", ""); // Remove trailing commas/spaces
                
                if (!string.IsNullOrEmpty(issue) && issue.Length <= 500) // Increased limit for longer descriptions
                {
                    return issue;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Equipment Model (e.g., HG8145V5 router)
    /// </summary>
    private string? ExtractEquipmentModel(string text)
    {
        var patterns = new[]
        {
            @"Current\s*ONU\s*model[:\s]+([A-Z0-9]+(?:V\d+)?)",  // ✅ Add this pattern first - Current ONU model: HG8145V5
            @"Equipment[:\s]+([A-Z0-9]+(?:V\d+)?)",
            @"Router[:\s]+([A-Z0-9]+(?:V\d+)?)",
            @"ONU\s*Model[:\s]+([A-Z0-9]+(?:V\d+)?)",
            @"Model[:\s]+([A-Z0-9]+(?:V\d+)?)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Material Doc Number from MRA forms
    /// </summary>
    private string? ExtractMaterialDocNumber(string text)
    {
        var patterns = new[]
        {
            @"Material\s*Doc\s*#?[:\s]+(\d+)",
            @"Material\s*Document\s*#?[:\s]+(\d+)",
            @"Material\s*Doc\s*No[:\s]+(\d+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract SAP Material Code from MRA forms
    /// </summary>
    private string? ExtractSapMaterialCode(string text)
    {
        var patterns = new[]
        {
            @"SAP\s*Material\s*Code\s*#?[:\s]+([A-Z0-9\-]+)",
            @"SAP\s*Material\s*Code[:\s]+([A-Z0-9\-]+)",
            @"Material\s*Code[:\s]+([A-Z0-9\-]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Material Description from MRA forms
    /// </summary>
    private string? ExtractMaterialDescription(string text)
    {
        var patterns = new[]
        {
            @"Material\s*Description[:\s]+([^\n\r]+)",
            @"Material\s*Desc[:\s]+([^\n\r]+)",
            @"Description[:\s]+([^\n\r]+)(?=\s*(?:SAP|Material|Service|Customer|AWO|Quantity))"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var desc = match.Groups[1].Value.Trim();
                // Filter out common false positives
                if (!desc.Contains("Material") && desc.Length > 3)
                {
                    return desc;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Submission Number from MRA forms
    /// </summary>
    private string? ExtractSubmissionNumber(string text)
    {
        var patterns = new[]
        {
            @"Submission\s*No[:\s]+(\d+)",
            @"Submission\s*Number[:\s]+(\d+)",
            @"Submission\s*#?[:\s]+(\d+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract URLs (Work Order URL, Assign SI URL, Docket URL)
    /// </summary>
    private List<string> ExtractUrls(string text)
    {
        var urlPattern = @"(https?://[^\s<>""]+)";
        var matches = Regex.Matches(text, urlPattern, RegexOptions.IgnoreCase);
        
        return matches.Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Extract Additional Contact Number from assurance emails
    /// </summary>
    private string? ExtractAdditionalContactNumber(string text)
    {
        var patterns = new[]
        {
            @"Additional\s*contact\s*number[:\s]+([0-9\s\-+]+)",
            @"Additional\s*Contact\s*Number[:\s]+([0-9\s\-+]+)",
            @"Additional\s*contact[:\s]+([0-9\s\-+]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var phone = match.Groups[1].Value.Trim();
                phone = Regex.Replace(phone, @"[^\d+]", ""); // Keep only digits and +
                if (phone.Length >= 9 && phone.Length <= 13)
                    return phone;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Username from Internet details section
    /// </summary>
    private string? ExtractUsername(string text)
    {
        var patterns = new[]
        {
            @"Username[:\s]+([a-zA-Z0-9._%+-@]+)",
            @"User\s*Name[:\s]+([a-zA-Z0-9._%+-@]+)",
            @"Internet\s*Username[:\s]+([a-zA-Z0-9._%+-@]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var username = match.Groups[1].Value.Trim();
                if (username.Length > 0 && username.Length <= 100)
                    return username;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Password from Internet details section
    /// </summary>
    private string? ExtractPassword(string text)
    {
        var patterns = new[]
        {
            @"Password[:\s]+([a-zA-Z0-9]+)",
            @"Internet\s*Password[:\s]+([a-zA-Z0-9]+)",
            @"Network\s*Password[:\s]+([a-zA-Z0-9]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var password = match.Groups[1].Value.Trim();
                if (password.Length > 0 && password.Length <= 100)
                    return password;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Splitter Name from remarks section
    /// Pattern: "Splitter Name: XG1_JMJXW_L12_S2_PERMATA FADASON BLOCK F"
    /// </summary>
    private string? ExtractSplitterName(string text)
    {
        var patterns = new[]
        {
            // Pattern to extract from remarks: "Splitter Name: XG1_JMJXW_L12_S2_PERMATA FADASON BLOCK F"
            @"Splitter\s*Name[:\s]+([A-Z0-9_\-\.\s]+?)(?:\s*(?:\n|Current\s*ONU|Issue|Action\s*summary|Additional\s*contact|Remarks[:\s]|$))",
            @"Splitter[:\s]+([A-Z0-9_\-\.\s]+?)(?:\s*(?:\n|Current\s*ONU|Issue|Action\s*summary|Additional\s*contact|Remarks[:\s]|$))",
            @"Splitter\s*Location[:\s]+([A-Z0-9_\-\.\s]+?)(?:\s*(?:\n|Current\s*ONU|Issue|Action\s*summary|Additional\s*contact|Remarks[:\s]|$))"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                var splitterName = match.Groups[1].Value.Trim();
                // Clean up - remove trailing punctuation and extra spaces
                splitterName = Regex.Replace(splitterName, @"[,\s]+$", "");
                if (splitterName.Length > 0 && splitterName.Length <= 200)
                    return splitterName;
            }
        }

        return null;
    }

    /// <summary>
    /// Extract Last Fault Date from service warranty information
    /// This is critical for warranty checking - if fault is within 30 days, customer might not pay
    /// </summary>
    private DateTime? ExtractLastFaultDate(string text)
    {
        var patterns = new[]
        {
            @"Last\s*Fault\s*Date[:\s]+(\d{4}[-/]\d{1,2}[-/]\d{1,2}(?:\s+\d{1,2}:\d{2}(?::\d{2})?)?)",
            @"Last\s*Fault\s*Date[:\s]+(\d{1,2}[-/]\d{1,2}[-/]\d{4}(?:\s+\d{1,2}:\d{2}(?::\d{2})?)?)",
            @"Fault\s*Date[:\s]+(\d{4}[-/]\d{1,2}[-/]\d{1,2}(?:\s+\d{1,2}:\d{2}(?::\d{2})?)?)",
            @"Last\s*Fault[:\s]+(\d{4}[-/]\d{1,2}[-/]\d{1,2}(?:\s+\d{1,2}:\d{2}(?::\d{2})?)?)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                var dateStr = match.Groups[1].Value.Trim();
                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    return parsedDate;
                }
            }
        }

        return null;
    }

    private string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        return PhoneNumberUtility.NormalizeMalaysianMsisdn(phone);
    }

    /// <summary>
    /// Calculate confidence score using same additive algorithm as Excel Parser
    /// ✅ FIXED: Changed from percentage-based to additive scoring to match Excel Parser behavior
    /// This ensures PDF attachments and email body parsing can reach 100% confidence with core fields,
    /// matching the behavior of Excel file parsing.
    /// </summary>
    private decimal CalculateConfidence(ParsedOrderData data)
    {
        // ✅ Use same additive scoring system as Excel Parser (SyncfusionExcelParserService.CalculateConfidenceScore)
        decimal score = 0.5m; // Base score

        // Required fields for all types (same weights as Excel Parser)
        if (!string.IsNullOrEmpty(data.ServiceId)) score += 0.15m;
        if (!string.IsNullOrEmpty(data.CustomerName)) score += 0.10m;
        if (!string.IsNullOrEmpty(data.CustomerPhone)) score += 0.08m;
        if (!string.IsNullOrEmpty(data.ServiceAddress)) score += 0.10m;
        if (data.AppointmentDateTime.HasValue) score += 0.10m;

        // Order-type specific (same logic as Excel Parser)
        var orderType = data.OrderTypeCode ?? "ACTIVATION";
        if (orderType == "ACTIVATION" || string.IsNullOrEmpty(data.OrderTypeCode))
        {
            if (!string.IsNullOrEmpty(data.PackageName)) score += 0.05m;
            if (!string.IsNullOrEmpty(data.Username)) score += 0.05m;
            // Note: Materials not available in PDF parsing, so we skip that boost
        }
        else if (orderType == "MODIFICATION_OUTDOOR")
        {
            if (!string.IsNullOrEmpty(data.OldAddress)) score += 0.10m;
        }

        return Math.Min(score, 1.0m); // Cap at 100% (same as Excel Parser)
    }
}

