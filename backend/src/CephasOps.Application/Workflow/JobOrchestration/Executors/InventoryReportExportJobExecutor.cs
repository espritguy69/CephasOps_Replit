using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Parser.Services.Converters;
using CephasOps.Domain.Workflow.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Workflow.JobOrchestration.Executors;

/// <summary>
/// Generates inventory report CSV (UsageSummary or SerialLifecycle) and optionally emails it (Phase 10).
/// Payload: reportType, companyId, departmentId; UsageSummary: fromDate, toDate, groupBy?, materialId?, locationId?; SerialLifecycle: serialNumbers; optional: emailTo, emailAccountId.
/// Replaces legacy inventoryreportexport BackgroundJob execution.
/// </summary>
public sealed class InventoryReportExportJobExecutor : IJobExecutor
{
    public string JobType => "inventoryreportexport";

    private readonly IStockLedgerService _ledgerService;
    private readonly ICsvService _csvService;
    private readonly IEmailSendingService? _emailSendingService;
    private readonly ITenantUsageService? _tenantUsageService;
    private readonly ILogger<InventoryReportExportJobExecutor> _logger;

    public InventoryReportExportJobExecutor(
        IStockLedgerService ledgerService,
        ICsvService csvService,
        ILogger<InventoryReportExportJobExecutor> logger,
        IEmailSendingService? emailSendingService = null,
        ITenantUsageService? tenantUsageService = null)
    {
        _ledgerService = ledgerService;
        _csvService = csvService;
        _logger = logger;
        _emailSendingService = emailSendingService;
        _tenantUsageService = tenantUsageService;
    }

    public async Task<bool> ExecuteAsync(JobExecution job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing inventory report export job {JobId}", job.Id);

        if (string.IsNullOrEmpty(job.PayloadJson))
            throw new ArgumentException("inventoryreportexport job requires payload with reportType");

        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(job.PayloadJson);
        if (payload == null || !payload.TryGetValue("reportType", out var rtEl))
            throw new ArgumentException("reportType is required");
        var reportType = rtEl.ValueKind == JsonValueKind.String ? rtEl.GetString()?.Trim() : rtEl.ToString()?.Trim();
        if (string.IsNullOrEmpty(reportType))
            throw new ArgumentException("reportType cannot be empty");

        var isUsage = string.Equals(reportType, "UsageSummary", StringComparison.OrdinalIgnoreCase);
        var isSerial = string.Equals(reportType, "SerialLifecycle", StringComparison.OrdinalIgnoreCase);
        if (!isUsage && !isSerial)
            throw new ArgumentException("reportType must be UsageSummary or SerialLifecycle");

        Guid? companyId = job.CompanyId;
        if (payload.TryGetValue("companyId", out var cidEl))
        {
            var cidStr = cidEl.ValueKind == JsonValueKind.String ? cidEl.GetString() : cidEl.ToString();
            if (!string.IsNullOrEmpty(cidStr) && Guid.TryParse(cidStr, out var cid))
                companyId = cid;
        }
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            throw new InvalidOperationException("Background job requires tenant context.");
        var effectiveCompanyId = companyId.Value;
        if (!payload.TryGetValue("departmentId", out var didEl))
            throw new ArgumentException("departmentId is required");
        var didStr = didEl.ValueKind == JsonValueKind.String ? didEl.GetString() : didEl.ToString();
        if (string.IsNullOrEmpty(didStr) || !Guid.TryParse(didStr, out var departmentId))
            throw new ArgumentException("departmentId must be a valid GUID");
        Guid? departmentIdNullable = departmentId;

        byte[] csvBytes;
        string fileName;
        if (isUsage)
        {
            if (!payload.TryGetValue("fromDate", out var fd) || !payload.TryGetValue("toDate", out var td))
                throw new ArgumentException("fromDate and toDate are required for UsageSummary");
            var fromStr = fd.ValueKind == JsonValueKind.String ? fd.GetString() : fd.ToString();
            var toStr = td.ValueKind == JsonValueKind.String ? td.GetString() : td.ToString();
            if (!DateTime.TryParse(fromStr, out var fromDate) || !DateTime.TryParse(toStr, out var toDate))
                throw new ArgumentException("fromDate and toDate must be valid dates");
            string? groupBy = payload.TryGetValue("groupBy", out var gb) ? (gb.ValueKind == JsonValueKind.String ? gb.GetString() : gb.ToString()) : null;
            Guid? materialId = payload.TryGetValue("materialId", out var mid) && Guid.TryParse(mid.ValueKind == JsonValueKind.String ? mid.GetString() : mid.ToString(), out var m) ? m : null;
            Guid? locationId = payload.TryGetValue("locationId", out var lid) && Guid.TryParse(lid.ValueKind == JsonValueKind.String ? lid.GetString() : lid.ToString(), out var l) ? l : null;
            var rows = await _ledgerService.GetUsageSummaryExportRowsAsync(fromDate, toDate, groupBy, materialId, locationId, effectiveCompanyId, departmentIdNullable, cancellationToken);
            csvBytes = _csvService.ExportToCsvBytes(rows);
            fileName = $"usage-summary-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";
        }
        else
        {
            if (!payload.TryGetValue("serialNumbers", out var snEl))
                throw new ArgumentException("serialNumbers (comma-separated) is required for SerialLifecycle");
            var serialNumbersStr = snEl.ValueKind == JsonValueKind.String ? snEl.GetString() : snEl.ToString();
            if (string.IsNullOrWhiteSpace(serialNumbersStr))
                throw new ArgumentException("serialNumbers is required");
            var serialNumbers = serialNumbersStr!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(s => s.Length > 0).Distinct().ToList();
            if (serialNumbers.Count == 0 || serialNumbers.Count > 50)
                throw new ArgumentException("serialNumbers must contain 1 to 50 serial numbers");
            var rows = await _ledgerService.GetSerialLifecycleExportRowsAsync(serialNumbers, effectiveCompanyId, departmentIdNullable, cancellationToken);
            csvBytes = _csvService.ExportToCsvBytes(rows);
            fileName = $"serial-lifecycle-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        }

        string? emailTo = payload.TryGetValue("emailTo", out var et) ? (et.ValueKind == JsonValueKind.String ? et.GetString()?.Trim() : et.ToString()?.Trim()) : null;
        string? emailAccountIdStr = payload.TryGetValue("emailAccountId", out var eid) ? (eid.ValueKind == JsonValueKind.String ? eid.GetString() : eid.ToString()) : null;
        if (!string.IsNullOrEmpty(emailTo) && Guid.TryParse(emailAccountIdStr, out var emailAccountId) && _emailSendingService != null)
        {
            IFormFile formFile = new InMemoryFormFile(csvBytes, fileName, "text/csv");
            var result = await _emailSendingService.SendEmailAsync(
                emailAccountId,
                emailTo,
                subject: $"Inventory report: {reportType}",
                body: $"Please find the attached inventory report ({fileName}).",
                attachments: new List<IFormFile> { formFile },
                cancellationToken: cancellationToken);
            if (!result.Success)
                _logger.LogWarning("Inventory report export email failed: {Error}", result.ErrorMessage);
            else
                _logger.LogInformation("Inventory report export emailed to {Email}", emailTo);
        }

        if (_tenantUsageService != null && companyId.HasValue)
            await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.ReportExports, 1, cancellationToken);

        _logger.LogInformation("Inventory report export job {JobId} completed: {ReportType}, {FileName}", job.Id, reportType, fileName);
        return true;
    }
}
