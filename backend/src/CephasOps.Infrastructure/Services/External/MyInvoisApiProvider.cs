using CephasOps.Domain.Billing;
using CephasOps.Domain.Settings;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CephasOps.Infrastructure.Services.External;

/// <summary>
/// MyInvois API provider implementation
/// Uses HttpClient to call MyInvois REST API (no official SDK available)
/// </summary>
public class MyInvoisApiProvider : IEInvoiceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGlobalSettingsReader _globalSettings;
    private readonly ILogger<MyInvoisApiProvider> _logger;
    private readonly InvoiceXmlBuilder _xmlBuilder;

    public MyInvoisApiProvider(
        IHttpClientFactory httpClientFactory,
        IGlobalSettingsReader globalSettings,
        ILogger<MyInvoisApiProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _globalSettings = globalSettings;
        _logger = logger;
        _xmlBuilder = new InvoiceXmlBuilder();
    }

    public async Task<EInvoiceSubmissionResult> SubmitInvoiceAsync(
        EInvoiceInvoiceDto invoice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting invoice {InvoiceNumber} to MyInvois", invoice.InvoiceNumber);

            // Get MyInvois settings
            var baseUrl = await _globalSettings.GetValueAsync<string>("MyInvois_BaseUrl", cancellationToken) 
                ?? "https://api-sandbox.myinvois.hasil.gov.my";
            var clientId = await _globalSettings.GetValueAsync<string>("MyInvois_ClientId", cancellationToken);
            var clientSecret = await _globalSettings.GetValueAsync<string>("MyInvois_ClientSecret", cancellationToken);
            var isEnabled = await _globalSettings.GetValueAsync<bool>("MyInvois_Enabled", cancellationToken);

            if (!isEnabled || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("MyInvois is not enabled or credentials are missing");
                return new EInvoiceSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "MyInvois is not enabled or credentials are missing"
                };
            }

            // Get access token
            var accessToken = await GetAccessTokenAsync(baseUrl, clientId, clientSecret, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new EInvoiceSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain MyInvois access token"
                };
            }

            // Build XML payload (MyInvois requires XML format)
            var xmlPayload = _xmlBuilder.BuildInvoiceXml(invoice);

            // Submit invoice
            var httpClient = _httpClientFactory.CreateClient("MyInvois");
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");
            var response = await httpClient.PostAsync("/api/v1.0/invoices", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var submissionId = ExtractSubmissionId(responseContent);

                _logger.LogInformation("Invoice {InvoiceNumber} submitted successfully. SubmissionId: {SubmissionId}", 
                    invoice.InvoiceNumber, submissionId);

                return new EInvoiceSubmissionResult
                {
                    Success = true,
                    SubmissionId = submissionId,
                    Message = "Invoice submitted successfully",
                    ResponseCode = response.StatusCode.ToString(),
                    SubmittedAt = DateTime.UtcNow
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to submit invoice {InvoiceNumber}. Status: {Status}, Error: {Error}", 
                    invoice.InvoiceNumber, response.StatusCode, errorContent);

                return new EInvoiceSubmissionResult
                {
                    Success = false,
                    ErrorMessage = $"MyInvois API error: {response.StatusCode} - {errorContent}",
                    ResponseCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting invoice {InvoiceNumber} to MyInvois", invoice.InvoiceNumber);
            return new EInvoiceSubmissionResult
            {
                Success = false,
                ErrorMessage = $"Exception: {ex.Message}"
            };
        }
    }

    public async Task<EInvoiceStatusResult> GetInvoiceStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking MyInvois status for submission {SubmissionId}", submissionId);

            var baseUrl = await _globalSettings.GetValueAsync<string>("MyInvois_BaseUrl", cancellationToken) 
                ?? "https://api-sandbox.myinvois.hasil.gov.my";
            var clientId = await _globalSettings.GetValueAsync<string>("MyInvois_ClientId", cancellationToken);
            var clientSecret = await _globalSettings.GetValueAsync<string>("MyInvois_ClientSecret", cancellationToken);

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return new EInvoiceStatusResult
                {
                    Success = false,
                    SubmissionId = submissionId,
                    ErrorMessage = "MyInvois credentials are missing"
                };
            }

            var accessToken = await GetAccessTokenAsync(baseUrl, clientId, clientSecret, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new EInvoiceStatusResult
                {
                    Success = false,
                    SubmissionId = submissionId,
                    ErrorMessage = "Failed to obtain MyInvois access token"
                };
            }

            var httpClient = _httpClientFactory.CreateClient("MyInvois");
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync($"/api/v1.0/invoices/{submissionId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = ExtractStatus(responseContent);

                return new EInvoiceStatusResult
                {
                    Success = true,
                    SubmissionId = submissionId,
                    Status = status,
                    LastUpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new EInvoiceStatusResult
                {
                    Success = false,
                    SubmissionId = submissionId,
                    ErrorMessage = $"Status check failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking MyInvois status for {SubmissionId}", submissionId);
            return new EInvoiceStatusResult
            {
                Success = false,
                SubmissionId = submissionId,
                ErrorMessage = $"Exception: {ex.Message}"
            };
        }
    }

    public async Task<EInvoiceSubmissionResult> SubmitCreditNoteAsync(
        EInvoiceCreditNoteDto creditNote,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Submitting credit note {CreditNoteNumber} to MyInvois", creditNote.CreditNoteNumber);

            var baseUrl = await _globalSettings.GetValueAsync<string>("MyInvois_BaseUrl", cancellationToken) 
                ?? "https://api-sandbox.myinvois.hasil.gov.my";
            var clientId = await _globalSettings.GetValueAsync<string>("MyInvois_ClientId", cancellationToken);
            var clientSecret = await _globalSettings.GetValueAsync<string>("MyInvois_ClientSecret", cancellationToken);
            var isEnabled = await _globalSettings.GetValueAsync<bool>("MyInvois_Enabled", cancellationToken);

            if (!isEnabled || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("MyInvois is not enabled or credentials are missing");
                return new EInvoiceSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "MyInvois is not enabled or credentials are missing"
                };
            }

            var accessToken = await GetAccessTokenAsync(baseUrl, clientId, clientSecret, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new EInvoiceSubmissionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain MyInvois access token"
                };
            }

            // Build XML payload for credit note
            var xmlPayload = _xmlBuilder.BuildCreditNoteXml(creditNote);

            var httpClient = _httpClientFactory.CreateClient("MyInvois");
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            var content = new StringContent(xmlPayload, Encoding.UTF8, new MediaTypeHeaderValue("application/xml"));
            var response = await httpClient.PostAsync("/api/v1.0/credit-notes", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var submissionId = ExtractSubmissionId(responseContent);

                _logger.LogInformation("Credit note {CreditNoteNumber} submitted successfully. SubmissionId: {SubmissionId}", 
                    creditNote.CreditNoteNumber, submissionId);

                return new EInvoiceSubmissionResult
                {
                    Success = true,
                    SubmissionId = submissionId,
                    Message = "Credit note submitted successfully",
                    ResponseCode = response.StatusCode.ToString(),
                    SubmittedAt = DateTime.UtcNow
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to submit credit note {CreditNoteNumber}. Status: {Status}, Error: {Error}", 
                    creditNote.CreditNoteNumber, response.StatusCode, errorContent);

                return new EInvoiceSubmissionResult
                {
                    Success = false,
                    ErrorMessage = $"MyInvois API error: {response.StatusCode} - {errorContent}",
                    ResponseCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting credit note {CreditNoteNumber} to MyInvois", creditNote.CreditNoteNumber);
            return new EInvoiceSubmissionResult
            {
                Success = false,
                ErrorMessage = $"Exception: {ex.Message}"
            };
        }
    }

    public async Task<EInvoiceStatusResult> GetCreditNoteStatusAsync(
        string submissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking MyInvois credit note status for submission {SubmissionId}", submissionId);

            var baseUrl = await _globalSettings.GetValueAsync<string>("MyInvois_BaseUrl", cancellationToken) 
                ?? "https://api-sandbox.myinvois.hasil.gov.my";
            var clientId = await _globalSettings.GetValueAsync<string>("MyInvois_ClientId", cancellationToken);
            var clientSecret = await _globalSettings.GetValueAsync<string>("MyInvois_ClientSecret", cancellationToken);

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return new EInvoiceStatusResult
                {
                    Success = false,
                    SubmissionId = submissionId,
                    ErrorMessage = "MyInvois credentials are missing"
                };
            }

            var accessToken = await GetAccessTokenAsync(baseUrl, clientId, clientSecret, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return new EInvoiceStatusResult
                {
                    Success = false,
                    SubmissionId = submissionId,
                    ErrorMessage = "Failed to obtain MyInvois access token"
                };
            }

            var httpClient = _httpClientFactory.CreateClient("MyInvois");
            httpClient.BaseAddress = new Uri(baseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync($"/api/v1.0/credit-notes/{submissionId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = ExtractStatus(responseContent);

                return new EInvoiceStatusResult
                {
                    Success = true,
                    SubmissionId = submissionId,
                    Status = status,
                    LastUpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                return new EInvoiceStatusResult
                {
                    Success = false,
                    SubmissionId = submissionId,
                    ErrorMessage = $"Status check failed: {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking MyInvois credit note status for {SubmissionId}", submissionId);
            return new EInvoiceStatusResult
            {
                Success = false,
                SubmissionId = submissionId,
                ErrorMessage = $"Exception: {ex.Message}"
            };
        }
    }

    private async Task<string?> GetAccessTokenAsync(string baseUrl, string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("MyInvois");
            httpClient.BaseAddress = new Uri(baseUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/auth/token");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", 
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);
                return tokenResponse.GetProperty("access_token").GetString();
            }

            _logger.LogError("Failed to obtain MyInvois access token. Status: {Status}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining MyInvois access token");
            return null;
        }
    }

    private string ExtractSubmissionId(string responseContent)
    {
        // Parse XML or JSON response to extract submission ID
        // This is a placeholder - actual implementation depends on MyInvois API response format
        try
        {
            // Try JSON first
            var jsonDoc = JsonDocument.Parse(responseContent);
            if (jsonDoc.RootElement.TryGetProperty("submissionId", out var submissionId))
                return submissionId.GetString() ?? Guid.NewGuid().ToString();
        }
        catch
        {
            // Not JSON, try XML
            // XML parsing would go here
        }

        // Fallback: generate a temporary ID
        return Guid.NewGuid().ToString();
    }

    private string ExtractStatus(string responseContent)
    {
        // Parse response to extract status
        // Placeholder implementation
        try
        {
            var jsonDoc = JsonDocument.Parse(responseContent);
            if (jsonDoc.RootElement.TryGetProperty("status", out var status))
                return status.GetString() ?? "Unknown";
        }
        catch
        {
            // XML parsing would go here
        }

        return "Unknown";
    }
}

