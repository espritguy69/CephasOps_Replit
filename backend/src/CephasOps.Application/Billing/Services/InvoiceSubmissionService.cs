using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.JobOrchestration;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Billing;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Service for tracking invoice submissions to portals
/// </summary>
public class InvoiceSubmissionService : IInvoiceSubmissionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceSubmissionService> _logger;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly EInvoiceProviderFactory _eInvoiceProviderFactory;
    private readonly ICurrencyExchangeService _currencyExchangeService;
    private readonly IJobExecutionEnqueuer? _jobExecutionEnqueuer;

    public InvoiceSubmissionService(
        ApplicationDbContext context,
        ILogger<InvoiceSubmissionService> logger,
        IWorkflowEngineService workflowEngineService,
        EInvoiceProviderFactory eInvoiceProviderFactory,
        ICurrencyExchangeService currencyExchangeService,
        IJobExecutionEnqueuer? jobExecutionEnqueuer = null)
    {
        _context = context;
        _logger = logger;
        _workflowEngineService = workflowEngineService;
        _eInvoiceProviderFactory = eInvoiceProviderFactory;
        _currencyExchangeService = currencyExchangeService;
        _jobExecutionEnqueuer = jobExecutionEnqueuer;
    }

    public async Task<InvoiceSubmissionHistoryDto> RecordSubmissionAsync(
        Guid invoiceId,
        string submissionId,
        string portalType,
        Guid submittedByUserId,
        string? responseMessage = null,
        string? responseCode = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording invoice submission: InvoiceId={InvoiceId}, SubmissionId={SubmissionId}, PortalType={PortalType}",
            invoiceId, submissionId, portalType);

        // Get invoice to get CompanyId
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
        {
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
        }

        // Deactivate previous submissions for this invoice (tenant-scoped)
        if (invoice.CompanyId.HasValue)
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""InvoiceSubmissionHistory"" SET ""IsActive"" = false WHERE ""InvoiceId"" = {0} AND ""CompanyId"" = {1} AND ""IsActive"" = true",
                invoiceId, invoice.CompanyId.Value);
        else
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE ""InvoiceSubmissionHistory"" SET ""IsActive"" = false WHERE ""InvoiceId"" = {0} AND ""CompanyId"" IS NULL AND ""IsActive"" = true",
                invoiceId);

        // Create new submission history record
        var submission = new InvoiceSubmissionHistory
        {
            Id = Guid.NewGuid(),
            CompanyId = invoice.CompanyId,
            InvoiceId = invoiceId,
            SubmissionId = submissionId,
            SubmittedAt = DateTime.UtcNow,
            Status = "Submitted",
            ResponseMessage = responseMessage,
            ResponseCode = responseCode,
            PortalType = portalType,
            SubmittedByUserId = submittedByUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InvoiceSubmissionHistory.Add(submission);

        // Update invoice with current submission ID
        invoice.SubmissionId = submissionId;
        invoice.SubmittedAt = DateTime.UtcNow;
        invoice.Status = "SubmittedToPortal";
        invoice.UpdatedAt = DateTime.UtcNow;
        
        // Auto-calculate due date: SubmittedAt + 45 days per billing terms
        invoice.DueDate = DateTime.UtcNow.AddDays(45);

        await _context.SaveChangesAsync(cancellationToken);

        // Phase 9: Enqueue MyInvois status poll job so we update status after portal processes
        if (string.Equals(portalType, "MyInvois", StringComparison.OrdinalIgnoreCase) && _jobExecutionEnqueuer != null)
        {
            var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object> { ["submissionHistoryId"] = submission.Id.ToString() });
            var nextRunAt = DateTime.UtcNow.AddMinutes(2);
            await _jobExecutionEnqueuer.EnqueueAsync("myinvoisstatuspoll", payloadJson, companyId: invoice.CompanyId, nextRunAtUtc: nextRunAt, cancellationToken: cancellationToken);
            _logger.LogDebug("Enqueued MyInvois status poll job for submission history {SubmissionHistoryId}", submission.Id);
        }

        // ⚠️ FIXED: Transition related Orders to "SubmittedToPortal" via workflow engine
        // Find all orders related to this invoice (via InvoiceLineItems or Order.InvoiceId)
        var relatedOrderIds = await _context.InvoiceLineItems
            .Where(li => li.InvoiceId == invoiceId && li.OrderId.HasValue)
            .Select(li => li.OrderId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Also check orders that directly reference this invoice
        var directOrderIds = await _context.Orders
            .Where(o => o.InvoiceId == invoiceId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var allOrderIds = relatedOrderIds.Union(directOrderIds).Distinct().ToList();

        // Transition each related order to "SubmittedToPortal" via workflow engine
        foreach (var orderId in allOrderIds)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order == null || order.Status == OrderStatus.SubmittedToPortal)
                {
                    continue; // Skip if order not found or already in target status
                }

                var executeDto = new ExecuteTransitionDto
                {
                    EntityType = "Order",
                    EntityId = orderId,
                    TargetStatus = OrderStatus.SubmittedToPortal,
                    PartnerId = order.PartnerId,
                    DepartmentId = order.DepartmentId,
                    Payload = new Dictionary<string, object>
                    {
                        ["reason"] = $"Invoice {invoice.InvoiceNumber} submitted to {portalType}",
                        ["invoiceId"] = invoiceId.ToString(),
                        ["submissionId"] = submissionId,
                        ["source"] = "InvoiceSubmissionService"
                    }
                };

                var workflowJob = await _workflowEngineService.ExecuteTransitionAsync(
                    invoice.CompanyId ?? Guid.Empty, // CompanyId is nullable in entity, use fallback
                    executeDto,
                    submittedByUserId,
                    cancellationToken);

                if (workflowJob.State != "Succeeded")
                {
                    _logger.LogWarning("Failed to transition Order {OrderId} to SubmittedToPortal: {Error}",
                        orderId, workflowJob.LastError);
                }
                else
                {
                    _logger.LogInformation("Order {OrderId} transitioned to SubmittedToPortal via workflow engine",
                        orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning Order {OrderId} to SubmittedToPortal", orderId);
                // Continue with other orders even if one fails
            }
        }

        _logger.LogInformation("Invoice submission recorded: SubmissionHistoryId={SubmissionHistoryId}, RelatedOrdersCount={OrderCount}",
            submission.Id, allOrderIds.Count);

        return MapToDto(submission);
    }

    public async Task<InvoiceSubmissionHistoryDto> UpdateSubmissionStatusAsync(
        Guid submissionHistoryId,
        string status,
        string? rejectionReason = null,
        string? paymentStatus = null,
        string? paymentReference = null,
        CancellationToken cancellationToken = default)
    {
        var submission = await _context.InvoiceSubmissionHistory
            .FirstOrDefaultAsync(s => s.Id == submissionHistoryId, cancellationToken);

        if (submission == null)
        {
            throw new KeyNotFoundException($"Submission history with ID {submissionHistoryId} not found");
        }

        submission.Status = status;
        submission.RejectionReason = rejectionReason;
        submission.PaymentStatus = paymentStatus;
        submission.PaymentReference = paymentReference;
        submission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Submission status updated: SubmissionHistoryId={SubmissionHistoryId}, Status={Status}",
            submissionHistoryId, status);

        return MapToDto(submission);
    }

    public async Task<List<InvoiceSubmissionHistoryDto>> GetSubmissionHistoryAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var submissions = await _context.InvoiceSubmissionHistory
            .Where(s => s.InvoiceId == invoiceId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        return submissions.Select(MapToDto).ToList();
    }

    public async Task<InvoiceSubmissionHistoryDto?> GetActiveSubmissionAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _context.InvoiceSubmissionHistory
            .Where(s => s.InvoiceId == invoiceId && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        return submission != null ? MapToDto(submission) : null;
    }

    public async Task<InvoiceSubmissionHistoryDto?> GetSubmissionByHistoryIdAsync(
        Guid submissionHistoryId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _context.InvoiceSubmissionHistory
            .FirstOrDefaultAsync(s => s.Id == submissionHistoryId, cancellationToken);
        return submission != null ? MapToDto(submission) : null;
    }

    public async Task<InvoiceSubmissionHistoryDto> SubmitInvoiceToPortalAsync(
        Guid invoiceId,
        Guid submittedByUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting invoice {InvoiceId} to e-invoice portal", invoiceId);

        // Get invoice with line items, company, and partner
        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
        {
            throw new KeyNotFoundException($"Invoice with ID {invoiceId} not found");
        }

        // Get company details
        Company? company = null;
        if (invoice.CompanyId.HasValue)
        {
            company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == invoice.CompanyId.Value, cancellationToken);
        }
        // If no company found, try to get the first active company as fallback
        if (company == null)
        {
            company = await _context.Companies
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Get partner details
        Partner? partner = null;
        if (invoice.PartnerId != Guid.Empty)
        {
            partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);
        }

        // Get e-invoice provider
        var provider = await _eInvoiceProviderFactory.GetProviderAsync(cancellationToken);

        // Convert invoice to EInvoiceInvoiceDto
        var eInvoiceDto = new CephasOps.Domain.Billing.EInvoiceInvoiceDto
        {
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            CurrencyCode = "MYR", // Invoice entity doesn't have currency - default to MYR
            ExchangeRate = null, // Invoice entity doesn't have exchange rate
            Notes = null, // Invoice entity doesn't have notes field
            Supplier = new CephasOps.Domain.Billing.EInvoicePartyDto
            {
                Name = company?.LegalName ?? company?.ShortName ?? "CephasOps",
                RegistrationNumber = company?.RegistrationNo,
                TaxId = company?.TaxId,
                CountryCode = "MY"
            },
            Customer = new CephasOps.Domain.Billing.EInvoicePartyDto
            {
                Name = partner?.Name ?? string.Empty,
                CountryCode = "MY"
            },
            LineItems = invoice.LineItems.Select((li, index) => new CephasOps.Domain.Billing.EInvoiceLineItemDto
            {
                LineNumber = index + 1,
                Description = li.Description ?? string.Empty,
                ItemCode = null, // InvoiceLineItem doesn't have ItemCode
                Quantity = li.Quantity,
                UnitOfMeasure = "UNIT", // InvoiceLineItem doesn't have UnitOfMeasure - default to UNIT
                UnitPrice = li.UnitPrice,
                LineTotal = li.Total, // InvoiceLineItem uses Total, not LineTotal
                TaxRate = 0, // InvoiceLineItem doesn't have TaxRate - calculate from invoice level
                TaxAmount = 0, // InvoiceLineItem doesn't have TaxAmount - calculate from invoice level
                TaxCode = null, // InvoiceLineItem doesn't have TaxCode
                DiscountAmount = 0 // InvoiceLineItem doesn't have DiscountAmount
            }).ToList()
        };

        // Submit to portal
        var submissionResult = await provider.SubmitInvoiceAsync(eInvoiceDto, cancellationToken);

        if (!submissionResult.Success || string.IsNullOrEmpty(submissionResult.SubmissionId))
        {
            throw new InvalidOperationException(
                $"Failed to submit invoice to portal: {submissionResult.ErrorMessage ?? submissionResult.Message ?? "Unknown error"}");
        }

        // Record submission
        return await RecordSubmissionAsync(
            invoiceId,
            submissionResult.SubmissionId,
            "MyInvois",
            submittedByUserId,
            submissionResult.Message,
            submissionResult.ResponseCode,
            cancellationToken);
    }

    private InvoiceSubmissionHistoryDto MapToDto(InvoiceSubmissionHistory submission)
    {
        return new InvoiceSubmissionHistoryDto
        {
            Id = submission.Id,
            InvoiceId = submission.InvoiceId,
            SubmissionId = submission.SubmissionId,
            SubmittedAt = submission.SubmittedAt,
            Status = submission.Status,
            ResponseMessage = submission.ResponseMessage,
            ResponseCode = submission.ResponseCode,
            RejectionReason = submission.RejectionReason,
            PortalType = submission.PortalType,
            SubmittedByUserId = submission.SubmittedByUserId,
            IsActive = submission.IsActive,
            PaymentStatus = submission.PaymentStatus,
            PaymentReference = submission.PaymentReference,
            Notes = submission.Notes,
            CreatedAt = submission.CreatedAt
        };
    }
}

