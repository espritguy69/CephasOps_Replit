using System.Text.Json;
using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Usage;
using CephasOps.Application.Commands;
using CephasOps.Application.Common;
using CephasOps.Application.Events;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using Color = Syncfusion.Drawing.Color;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Billing service implementation
/// </summary>
public class BillingService : IBillingService
{
    private const string IdempotencyCommandType = "CreateInvoice";
    private static readonly JsonSerializerOptions IdempotencyJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<BillingService> _logger;
    private readonly ICommandProcessingLogStore _idempotencyStore;
    private readonly ITenantUsageService? _tenantUsageService;
    private readonly IEventBus? _eventBus;

    public BillingService(
        ApplicationDbContext context,
        ILogger<BillingService> logger,
        ICommandProcessingLogStore idempotencyStore,
        ITenantUsageService? tenantUsageService = null,
        IEventBus? eventBus = null)
    {
        _context = context;
        _logger = logger;
        _idempotencyStore = idempotencyStore;
        _tenantUsageService = tenantUsageService;
        _eventBus = eventBus;
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(Guid? companyId, string? status = null, Guid? partnerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting invoices for company {CompanyId}", companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = companyId.HasValue 
            ? _context.Invoices.Include(i => i.LineItems).Where(i => i.CompanyId == companyId.Value)
            : _context.Invoices.Include(i => i.LineItems).AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (partnerId.HasValue)
        {
            query = query.Where(i => i.PartnerId == partnerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate <= toDate.Value);
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        // Load partners in bulk (company letterhead not needed for list view)
        var partnerIds = invoices.Where(i => i.PartnerId != Guid.Empty).Select(i => i.PartnerId).Distinct().ToList();
        var partners = await _context.Partners
            .Where(p => partnerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return invoices.Select(i => MapToInvoiceDto(i, partners, null)).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting invoice {InvoiceId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.Invoices.Include(i => i.LineItems).Where(i => i.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);

        if (invoice == null) return null;

        // Load partner
        Partner? partner = null;
        if (invoice.PartnerId != Guid.Empty)
        {
            partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);
        }

        var partners = partner != null 
            ? new Dictionary<Guid, Partner> { [partner.Id] = partner }
            : new Dictionary<Guid, Partner>();

        var company = invoice.CompanyId != Guid.Empty 
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken)
            : null;

        var orderData = await LoadOrderDataForLineItemsAsync(invoice.LineItems.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList(), cancellationToken);

        // Recompute tax rate so returned totals always match line items (fixes display mismatch)
        decimal taxRate = 0.06m;
        if (invoice.CompanyId != Guid.Empty)
        {
            var taxCode = await _context.Set<TaxCode>()
                .FirstOrDefaultAsync(tc => tc.CompanyId == invoice.CompanyId && (tc.Code == "GST" || tc.IsDefault) && tc.IsActive && !tc.IsDeleted, cancellationToken);
            if (taxCode != null)
                taxRate = taxCode.TaxRate / 100m;
        }
        return MapToInvoiceDto(invoice, partners, company, orderData, taxRate);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("CreateInvoice");
        FinancialIsolationGuard.RequireCompany(companyId, "CreateInvoice");

        var idempotencyKey = BuildInvoiceIdempotencyKey(dto.IdempotencyKey, companyId);
        if (idempotencyKey != null)
        {
            var existing = await _idempotencyStore.TryGetCompletedResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
            if (existing?.ResultJson != null)
            {
                var invoiceId = DeserializeInvoiceIdFromResult(existing.ResultJson);
                if (invoiceId.HasValue)
                {
                    var cached = await GetInvoiceByIdAsync(invoiceId.Value, companyId, cancellationToken).ConfigureAwait(false);
                    if (cached != null)
                    {
                        _logger.LogInformation("CreateInvoice idempotency reuse. key={Key}, invoiceId={InvoiceId}", idempotencyKey, invoiceId);
                        return cached;
                    }
                }
            }

            var executionId = Guid.NewGuid();
            var claimed = await _idempotencyStore.TryClaimAsync(executionId, idempotencyKey, IdempotencyCommandType, null, null, cancellationToken).ConfigureAwait(false);
            if (!claimed)
            {
                var retryExisting = await _idempotencyStore.TryGetCompletedResultAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
                if (retryExisting?.ResultJson != null)
                {
                    var invoiceId = DeserializeInvoiceIdFromResult(retryExisting.ResultJson);
                    if (invoiceId.HasValue)
                    {
                        var cached = await GetInvoiceByIdAsync(invoiceId.Value, companyId, cancellationToken).ConfigureAwait(false);
                        if (cached != null)
                        {
                            _logger.LogInformation("CreateInvoice idempotency reuse (after claim conflict). key={Key}, invoiceId={InvoiceId}", idempotencyKey, invoiceId);
                            return cached;
                        }
                    }
                }
                throw new InvalidOperationException("CreateInvoice: Another request with the same idempotency key is in progress or failed. Retry later.");
            }

            try
            {
                var result = await CreateInvoiceCoreAsync(dto, companyId, userId, cancellationToken).ConfigureAwait(false);
                var resultJson = JsonSerializer.Serialize(new { InvoiceId = result.Id }, IdempotencyJsonOptions);
                await _idempotencyStore.MarkCompletedAsync(executionId, resultJson, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                await _idempotencyStore.MarkFailedAsync(executionId, ex.Message, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        return await CreateInvoiceCoreAsync(dto, companyId, userId, cancellationToken).ConfigureAwait(false);
    }

    private static string? BuildInvoiceIdempotencyKey(string? clientKey, Guid? companyId)
    {
        if (string.IsNullOrWhiteSpace(clientKey) || !companyId.HasValue || companyId.Value == Guid.Empty)
            return null;
        return $"{companyId.Value:N}:{IdempotencyCommandType}:{clientKey.Trim()}";
    }

    private static Guid? DeserializeInvoiceIdFromResult(string resultJson)
    {
        try
        {
            var doc = JsonDocument.Parse(resultJson);
            if (doc.RootElement.TryGetProperty("invoiceId", out var prop) && prop.TryGetGuid(out var id))
                return id;
        }
        catch { /* ignore */ }
        return null;
    }

    private async Task<InvoiceDto> CreateInvoiceCoreAsync(CreateInvoiceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var orderIdsInLines = dto.LineItems?.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList() ?? new List<Guid>();
        if (orderIdsInLines.Count > 0)
        {
            var orders = await _context.Orders
                .Where(o => orderIdsInLines.Contains(o.Id))
                .Select(o => new { o.Id, o.CompanyId })
                .ToListAsync(cancellationToken);
            if (orders.Count != orderIdsInLines.Count)
            {
                var foundIds = orders.Select(o => o.Id).ToHashSet();
                var missing = orderIdsInLines.Where(id => !foundIds.Contains(id)).ToList();
                throw new InvalidOperationException(
                    "CreateInvoice: One or more orders were not found or do not belong to the current company. " +
                    $"Missing or out-of-scope OrderIds: {string.Join(", ", missing.Take(5))}" +
                    (missing.Count > 5 ? $" (and {missing.Count - 5} more)" : "."));
            }
            FinancialIsolationGuard.RequireSameCompanySet(
                "CreateInvoice",
                companyId!.Value,
                orders.Select(o => ("Order", (Guid?)o.CompanyId, (object?)o.Id)));
        }

        _logger.LogInformation("Creating invoice for company {CompanyId}", companyId);

        var termsInDays = dto.TermsInDays ?? 45;
        var dueDate = dto.DueDate ?? dto.InvoiceDate.AddDays(termsInDays);

        var subtotal = dto.LineItems?.Sum(item => item.Quantity * item.UnitPrice) ?? 0m;
        
        // Get GST rate from default tax code
        decimal taxRate = 0.06m; // Default 6% GST
        if (companyId.HasValue)
        {
            try
            {
                var defaultTaxCode = await _context.Set<TaxCode>()
                    .FirstOrDefaultAsync(tc => tc.CompanyId == companyId.Value && tc.Code == "GST" && tc.IsActive && !tc.IsDeleted, cancellationToken);
                
                if (defaultTaxCode == null)
                {
                    // Try to get any default tax code
                    var defaultTax = await _context.Set<TaxCode>()
                        .FirstOrDefaultAsync(tc => tc.CompanyId == companyId.Value && tc.IsDefault && tc.IsActive && !tc.IsDeleted, cancellationToken);
                    
                    if (defaultTax != null)
                    {
                        taxRate = defaultTax.TaxRate / 100m; // Convert percentage to decimal
                    }
                }
                else
                {
                    taxRate = defaultTaxCode.TaxRate / 100m; // Convert percentage to decimal
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get tax rate from settings, using default 6%");
            }
        }
        
        var taxAmount = subtotal * taxRate;
        var totalAmount = subtotal + taxAmount;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? Guid.Empty, // Company feature removed
            InvoiceNumber = await GenerateInvoiceNumberAsync(companyId ?? Guid.Empty, cancellationToken),
            PartnerId = dto.PartnerId,
            InvoiceDate = dto.InvoiceDate,
            TermsInDays = termsInDays,
            DueDate = dueDate,
            SubTotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Status = "Draft",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var itemDto in dto.LineItems ?? [])
        {
            var lineItem = new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId ?? Guid.Empty, // Company feature removed
                InvoiceId = invoice.Id,
                Description = itemDto.Description,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                Total = itemDto.Quantity * itemDto.UnitPrice,
                OrderId = itemDto.OrderId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            invoice.LineItems.Add(lineItem);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice created. tenantId={TenantId}, invoiceId={InvoiceId}, operation=CreateInvoice, success=true", companyId, invoice.Id);

        if (_tenantUsageService != null && companyId.HasValue && companyId.Value != Guid.Empty)
            await _tenantUsageService.RecordUsageAsync(companyId, TenantUsageService.MetricKeys.InvoicesGenerated, 1, cancellationToken);

        if (_eventBus != null)
        {
            var evt = new InvoiceGeneratedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                CompanyId = companyId,
                TriggeredByUserId = userId,
                InvoiceId = invoice.Id,
                PartnerId = invoice.PartnerId != Guid.Empty ? invoice.PartnerId : null,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status
            };
            await _eventBus.PublishAsync(evt, cancellationToken);
        }

        // Load partner for DTO
        Partner? partner = null;
        if (invoice.PartnerId != Guid.Empty)
        {
            partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);
        }

        var partners = partner != null 
            ? new Dictionary<Guid, Partner> { [partner.Id] = partner }
            : new Dictionary<Guid, Partner>();

        var company = invoice.CompanyId != Guid.Empty 
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken)
            : null;

        var orderData = await LoadOrderDataForLineItemsAsync(invoice.LineItems.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList(), cancellationToken);
        return MapToInvoiceDto(invoice, partners, company, orderData);
    }

    public async Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating invoice {InvoiceId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.Invoices.Include(i => i.LineItems).Where(i => i.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);

        if (invoice == null)
        {
            throw new KeyNotFoundException($"Invoice with ID {id} not found");
        }

        // Lock InvoiceDate and DueDate when invoice is Sent - do not allow changes
        var isSent = string.Equals(invoice.Status, "Sent", StringComparison.OrdinalIgnoreCase);

        if (dto.InvoiceDate.HasValue && !isSent)
        {
            invoice.InvoiceDate = dto.InvoiceDate.Value;
            // Recalculate DueDate when InvoiceDate changes (before Sent)
            var termsInDays = dto.TermsInDays ?? invoice.TermsInDays;
            invoice.TermsInDays = termsInDays;
            invoice.DueDate = invoice.InvoiceDate.AddDays(termsInDays);
        }
        else if (dto.TermsInDays.HasValue && !isSent)
        {
            invoice.TermsInDays = dto.TermsInDays.Value;
            invoice.DueDate = invoice.InvoiceDate.AddDays(invoice.TermsInDays);
        }
        else if (dto.DueDate.HasValue && !isSent)
        {
            invoice.DueDate = dto.DueDate;
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            invoice.Status = dto.Status;
        }

        if (dto.SubmissionId != null)
        {
            invoice.SubmissionId = dto.SubmissionId;
        }

        if (dto.SubmittedAt.HasValue)
        {
            invoice.SubmittedAt = dto.SubmittedAt;
        }

        if (dto.PaidAt.HasValue)
        {
            invoice.PaidAt = dto.PaidAt;
        }

        if (dto.PartnerId.HasValue && dto.PartnerId.Value != Guid.Empty && !isSent)
        {
            invoice.PartnerId = dto.PartnerId.Value;
        }

        // Line items update (Draft only)
        if (dto.LineItems != null && !isSent)
        {
            var existingIds = dto.LineItems.Where(li => li.Id != Guid.Empty).Select(li => li.Id).ToHashSet();
            var toRemove = invoice.LineItems.Where(li => !existingIds.Contains(li.Id)).ToList();
            foreach (var li in toRemove)
            {
                _context.InvoiceLineItems.Remove(li);
            }

            decimal subtotal = 0;
            foreach (var itemDto in dto.LineItems)
            {
                var lineTotal = itemDto.Quantity * itemDto.UnitPrice;
                subtotal += lineTotal;

                if (itemDto.Id == Guid.Empty)
                {
                    var newItem = new InvoiceLineItem
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = invoice.CompanyId,
                        InvoiceId = invoice.Id,
                        Description = itemDto.Description,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                        Total = lineTotal,
                        OrderId = itemDto.OrderId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    invoice.LineItems.Add(newItem);
                }
                else
                {
                    var existing = invoice.LineItems.FirstOrDefault(li => li.Id == itemDto.Id);
                    if (existing != null)
                    {
                        existing.Description = itemDto.Description;
                        existing.Quantity = itemDto.Quantity;
                        existing.UnitPrice = itemDto.UnitPrice;
                        existing.Total = lineTotal;
                        existing.OrderId = itemDto.OrderId;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            decimal taxRate = 0.06m;
            if (invoice.CompanyId != Guid.Empty)
            {
                var taxCode = await _context.Set<TaxCode>()
                    .FirstOrDefaultAsync(tc => tc.CompanyId == invoice.CompanyId && (tc.Code == "GST" || tc.IsDefault) && tc.IsActive && !tc.IsDeleted, cancellationToken);
                if (taxCode != null)
                    taxRate = taxCode.TaxRate / 100m;
            }
            invoice.SubTotal = subtotal;
            invoice.TaxAmount = subtotal * taxRate;
            invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount;
        }

        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice updated. tenantId={TenantId}, invoiceId={InvoiceId}, operation=UpdateInvoice, success=true", companyId, id);

        // Load partner for DTO
        Partner? partner = null;
        if (invoice.PartnerId != Guid.Empty)
        {
            partner = await _context.Partners
                .FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);
        }

        var partners = partner != null 
            ? new Dictionary<Guid, Partner> { [partner.Id] = partner }
            : new Dictionary<Guid, Partner>();

        var company = invoice.CompanyId != Guid.Empty 
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken)
            : null;

        var orderData = await LoadOrderDataForLineItemsAsync(invoice.LineItems.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList(), cancellationToken);
        return MapToInvoiceDto(invoice, partners, company, orderData);
    }

    public async Task DeleteInvoiceAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting invoice {InvoiceId} for company {CompanyId}", id, companyId);

        // SuperAdmin can access all companies (companyId is null), regular users are filtered by companyId
        var query = _context.Invoices.Where(i => i.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);

        if (invoice == null)
        {
            throw new KeyNotFoundException($"Invoice with ID {id} not found");
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Invoice deleted. tenantId={TenantId}, invoiceId={InvoiceId}, operation=DeleteInvoice, success=true", companyId, id);
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF for invoice {InvoiceId} for company {CompanyId}", id, companyId);
        
        // Verify invoice exists and user has access
        var query = _context.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.Id == id);
        if (companyId.HasValue)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }
        
        var invoice = await query.FirstOrDefaultAsync(cancellationToken);
        if (invoice == null)
        {
            throw new KeyNotFoundException($"Invoice with ID {id} not found");
        }

        // Load company and partner for header details
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken);

        var partner = await _context.Partners
            .FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);

        var orderData = await LoadOrderDataForLineItemsAsync(invoice.LineItems.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList(), cancellationToken);

        var billToSubject = invoice.LineItems
            .Where(li => li.OrderId.HasValue && orderData.TryGetValue(li.OrderId.Value, out _))
            .Select(li => orderData[li.OrderId!.Value].OrderType)
            .FirstOrDefault(ot => !string.IsNullOrEmpty(ot)) ?? "Non Prelaid Activation";

        try
        {
            // Create PDF document
            using var pdfDocument = new PdfDocument();
            var page = pdfDocument.Pages.Add();
            var graphics = page.Graphics;

            // Set page margins
            float margin = 40;
            float yPosition = margin;
            float pageWidth = page.Size.Width - (margin * 2);
            float lineHeight = 20;

            // Header - Company Info
            var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
            var normalFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
            var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
            var boldFont = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);

            // Company letterhead
            if (company != null)
            {
                graphics.DrawString(company.LegalName ?? company.ShortName ?? "CephasOps", headerFont, PdfBrushes.Black, new PointF(margin, yPosition));
                yPosition += lineHeight;
                
                if (!string.IsNullOrEmpty(company.Address))
                {
                    graphics.DrawString(company.Address, normalFont, PdfBrushes.Black, new PointF(margin, yPosition));
                    yPosition += lineHeight;
                }
                
                var contactParts = new List<string>();
                if (!string.IsNullOrEmpty(company.Phone)) contactParts.Add(company.Phone);
                if (!string.IsNullOrEmpty(company.Email)) contactParts.Add(company.Email);
                if (contactParts.Count > 0)
                {
                    graphics.DrawString(string.Join(" | ", contactParts), smallFont, PdfBrushes.Black, new PointF(margin, yPosition));
                    yPosition += lineHeight;
                }
                
                if (!string.IsNullOrEmpty(company.RegistrationNo))
                {
                    graphics.DrawString($"Registration: {company.RegistrationNo}", smallFont, PdfBrushes.Black, new PointF(margin, yPosition));
                    yPosition += lineHeight;
                }
            }

            yPosition += 20;

            // Invoice Title
            graphics.DrawString("INVOICE", headerFont, PdfBrushes.Black, new PointF(margin, yPosition));
            yPosition += lineHeight + 10;

            // Bill To (left) + Invoice Metadata (right) - side by side
            var metaStartY = yPosition;
            graphics.DrawString("Bill To:", boldFont, PdfBrushes.Black, new PointF(margin, yPosition));
            yPosition += lineHeight;
            if (partner != null)
            {
                graphics.DrawString(partner.Name, normalFont, PdfBrushes.Black, new PointF(margin, yPosition));
                yPosition += lineHeight;
                if (!string.IsNullOrEmpty(partner.BillingAddress))
                {
                    foreach (var addrLine in partner.BillingAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        graphics.DrawString(addrLine.Trim(), normalFont, PdfBrushes.Black, new PointF(margin, yPosition));
                        yPosition += lineHeight;
                    }
                }
                var personInCharge = !string.IsNullOrEmpty(partner.ContactName) ? partner.ContactName : "Finance Department";
                graphics.DrawString($"Person in charge: {personInCharge}", normalFont, PdfBrushes.Black, new PointF(margin, yPosition));
                yPosition += lineHeight;
                if (!string.IsNullOrEmpty(partner.ContactPhone))
                {
                    graphics.DrawString($"TEL: {partner.ContactPhone}", normalFont, PdfBrushes.Black, new PointF(margin, yPosition));
                    yPosition += lineHeight;
                }
                graphics.DrawString($"Subject: {billToSubject}", normalFont, PdfBrushes.Black, new PointF(margin, yPosition));
                yPosition += lineHeight;
            }

            yPosition += 15;

            // Invoice Metadata (right-aligned block)
            float metaX = margin + pageWidth - 200;
            graphics.DrawString($"Date Issued: {invoice.InvoiceDate:dd MMM yyyy}", normalFont, PdfBrushes.Black, new PointF(metaX, metaStartY));
            graphics.DrawString($"Invoice No.: {invoice.InvoiceNumber}", normalFont, PdfBrushes.Black, new PointF(metaX, metaStartY + lineHeight));
            graphics.DrawString("Terms: Net 45 days", normalFont, PdfBrushes.Black, new PointF(metaX, metaStartY + lineHeight * 2));
            graphics.DrawString("Prepared By: Cephas Admin", normalFont, PdfBrushes.Black, new PointF(metaX, metaStartY + lineHeight * 3));
            if (invoice.DueDate.HasValue)
                graphics.DrawString($"Due Date: {invoice.DueDate.Value:dd MMM yyyy}", normalFont, PdfBrushes.Black, new PointF(metaX, metaStartY + lineHeight * 4));

            yPosition += 20;

            // Line Items Table Header: No | Description | Qty/Unit | Unit Price | Discount | Total
            float tableY = yPosition;
            float col0Width = 25;
            float col1Width = pageWidth * 0.32f;
            float col2Width = 60;
            float col3Width = 70;
            float col4Width = 55;

            graphics.DrawRectangle(new PdfSolidBrush(Color.FromArgb(68, 114, 196)), new RectangleF(margin, tableY, pageWidth, lineHeight + 5));
            graphics.DrawString("No", boldFont, PdfBrushes.White, new PointF(margin + 3, tableY + 3));
            graphics.DrawString("Description", boldFont, PdfBrushes.White, new PointF(margin + col0Width + 3, tableY + 3));
            graphics.DrawString("Qty/Unit", boldFont, PdfBrushes.White, new PointF(margin + col0Width + col1Width, tableY + 3));
            graphics.DrawString("Unit Price", boldFont, PdfBrushes.White, new PointF(margin + col0Width + col1Width + col2Width, tableY + 3));
            graphics.DrawString("Discount", boldFont, PdfBrushes.White, new PointF(margin + col0Width + col1Width + col2Width + col3Width, tableY + 3));
            graphics.DrawString("Total", boldFont, PdfBrushes.White, new PointF(margin + col0Width + col1Width + col2Width + col3Width + col4Width, tableY + 3));

            tableY += lineHeight + 5;

            int rowNum = 1;
            float rowLineH = 12;
            foreach (var item in invoice.LineItems)
            {
                var od = item.OrderId.HasValue && orderData.TryGetValue(item.OrderId.Value, out var d) ? d : default;
                float rowH = rowLineH * 4 + 6;
                float descX = margin + col0Width + 3;
                float descY = tableY + 2;

                if (!string.IsNullOrWhiteSpace(od.CustomerName) || !string.IsNullOrWhiteSpace(od.ServiceId) || !string.IsNullOrWhiteSpace(od.OrderType) || !string.IsNullOrWhiteSpace(od.DocketNo))
                {
                    graphics.DrawString($"CUSTOMER NAME: {od.CustomerName}", smallFont, PdfBrushes.Black, new PointF(descX, descY));
                    graphics.DrawString($"SERVICE ID: {od.ServiceId}", smallFont, PdfBrushes.Black, new PointF(descX, descY + rowLineH));
                    graphics.DrawString($"ORDER TYPE: {od.OrderType}", smallFont, PdfBrushes.Black, new PointF(descX, descY + rowLineH * 2));
                    graphics.DrawString($"DOCKET NO: {od.DocketNo}", smallFont, PdfBrushes.Black, new PointF(descX, descY + rowLineH * 3));
                }
                else
                {
                    graphics.DrawString(item.Description ?? "N/A", smallFont, PdfBrushes.Black, new PointF(descX, descY));
                }

                graphics.DrawString(rowNum.ToString(), normalFont, PdfBrushes.Black, new PointF(margin + 3, tableY + 6));
                graphics.DrawString(item.Quantity.ToString("0.##"), normalFont, PdfBrushes.Black, new PointF(margin + col0Width + col1Width, tableY + 6));
                graphics.DrawString(item.UnitPrice.ToString("C"), normalFont, PdfBrushes.Black, new PointF(margin + col0Width + col1Width + col2Width, tableY + 6));
                graphics.DrawString("0.00", normalFont, PdfBrushes.Black, new PointF(margin + col0Width + col1Width + col2Width + col3Width, tableY + 6));
                graphics.DrawString(item.Total.ToString("C"), normalFont, PdfBrushes.Black, new PointF(margin + col0Width + col1Width + col2Width + col3Width + col4Width, tableY + 6));

                graphics.DrawLine(new PdfPen(Color.LightGray), new PointF(margin, tableY + rowH), new PointF(margin + pageWidth, tableY + rowH));
                tableY += rowH + 2;
                rowNum++;
            }

            yPosition = tableY + 20;

            // Totals Section (right-aligned)
            float totalsX = margin + pageWidth - 120;
            graphics.DrawString("Subtotal:", normalFont, PdfBrushes.Black, new PointF(totalsX, yPosition));
            graphics.DrawString(invoice.SubTotal.ToString("C"), normalFont, PdfBrushes.Black, new PointF(totalsX + 50, yPosition));
            yPosition += lineHeight;

            graphics.DrawString("Tax (GST):", normalFont, PdfBrushes.Black, new PointF(totalsX, yPosition));
            graphics.DrawString(invoice.TaxAmount.ToString("C"), normalFont, PdfBrushes.Black, new PointF(totalsX + 50, yPosition));
            yPosition += lineHeight;

            graphics.DrawString("TOTAL:", boldFont, PdfBrushes.Black, new PointF(totalsX, yPosition));
            graphics.DrawString(invoice.TotalAmount.ToString("C"), boldFont, PdfBrushes.Black, new PointF(totalsX + 50, yPosition));

            // Footer (accounting-style)
            yPosition = page.Size.Height - margin - 80;
            graphics.DrawString("Bank Name: AgroBank", smallFont, PdfBrushes.Black, new PointF(margin, yPosition));
            yPosition += lineHeight;
            graphics.DrawString("Bank Account Number: 1005511000058559", smallFont, PdfBrushes.Black, new PointF(margin, yPosition));
            yPosition += lineHeight;
            graphics.DrawString("Payable to: CEPHAS TRADING & SERVICES", smallFont, PdfBrushes.Black, new PointF(margin, yPosition));
            yPosition += lineHeight + 8;
            graphics.DrawString("\"This is a computer generated document. No signature is required.\"", smallFont, PdfBrushes.Gray, new PointF(margin, yPosition));

            // Save to memory stream
            using var stream = new MemoryStream();
            pdfDocument.Save(stream);
            pdfDocument.Close(true);

            var pdfBytes = stream.ToArray();
            _logger.LogInformation("✅ Invoice PDF generated successfully: {Size} KB", pdfBytes.Length / 1024);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating invoice PDF for invoice {InvoiceId}", id);
            throw;
        }
    }

    private async Task<Dictionary<Guid, (string CustomerName, string ServiceId, string OrderType, string DocketNo)>> LoadOrderDataForLineItemsAsync(
        List<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0)
            return new Dictionary<Guid, (string, string, string, string)>();

        var orders = await _context.Orders
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var orderTypeIds = orders.Select(o => o.OrderTypeId).Distinct().ToList();
        var orderTypes = await _context.OrderTypes
            .Where(ot => orderTypeIds.Contains(ot.Id))
            .ToDictionaryAsync(ot => ot.Id, cancellationToken);

        var result = new Dictionary<Guid, (string CustomerName, string ServiceId, string OrderType, string DocketNo)>();
        foreach (var order in orders)
        {
            var orderTypeName = orderTypes.TryGetValue(order.OrderTypeId, out var ot) ? ot.Name : "";
            result[order.Id] = (
                order.CustomerName ?? "",
                order.ServiceId ?? "",
                orderTypeName,
                order.DocketNumber ?? ""
            );
        }
        return result;
    }

    public async Task<Guid?> GetInvoiceCompanyIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Where(i => i.Id == id)
            .Select(i => new { i.CompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (invoice == null) return null;
        // When tenant scope is set (and not platform bypass), do not leak another tenant's company
        if (!TenantSafetyGuard.IsPlatformBypassActive && TenantScope.CurrentTenantId.HasValue && TenantScope.CurrentTenantId.Value != Guid.Empty
            && invoice.CompanyId != TenantScope.CurrentTenantId.Value)
            return null;
        return invoice.CompanyId;
    }

    public async Task<ResolvedInvoiceLineDto?> ResolveInvoiceLineFromOrderAsync(Guid orderId, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("ResolveInvoiceLineFromOrder");
        FinancialIsolationGuard.RequireCompany(companyId, "ResolveInvoiceLineFromOrder");
        var refDate = referenceDate ?? DateTime.UtcNow;
        // Explicit company-scoped read (defense-in-depth): caller provides companyId; do not rely on ambient tenant.
        var order = await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Partner)
            .Include(o => o.OrderCategory)
            .Where(o => o.Id == orderId && o.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (order == null)
            return null;
        if (!order.OrderCategoryId.HasValue || order.OrderCategoryId.Value == Guid.Empty)
        {
            _logger.LogWarning("Order {OrderId} has no OrderCategoryId; cannot resolve BillingRatecard.", orderId);
            return null;
        }
        var serviceCategory = order.OrderCategory?.Code;
        if (string.IsNullOrEmpty(serviceCategory))
        {
            var cat = await _context.OrderCategories.Where(oc => oc.Id == order.OrderCategoryId.Value).Select(oc => oc.Code).FirstOrDefaultAsync(cancellationToken);
            serviceCategory = cat ?? "";
        }
        if (string.IsNullOrEmpty(serviceCategory))
        {
            _logger.LogWarning("Order {OrderId} OrderCategory has no Code; cannot resolve BillingRatecard.", orderId);
            return null;
        }
        var partnerGroupId = order.Partner?.GroupId;
        // Explicit company-scoped: caller provides companyId; do not rely on ambient tenant.
        var baseQuery = _context.BillingRatecards
            .IgnoreQueryFilters()
            .Where(br => br.CompanyId == companyId && br.IsActive
                && br.OrderTypeId == order.OrderTypeId
                && br.ServiceCategory == serviceCategory
                && (br.EffectiveFrom == null || br.EffectiveFrom <= refDate)
                && (br.EffectiveTo == null || br.EffectiveTo >= refDate));

        // 1. Exact partner rate: PartnerId + OrderTypeId + ServiceCategory + InstallationMethodId (then without method)
        var partnerRate = await baseQuery
            .Where(br => br.PartnerId == order.PartnerId)
            .Where(br => order.InstallationMethodId.HasValue ? br.InstallationMethodId == order.InstallationMethodId : (br.InstallationMethodId == null || br.InstallationMethodId == order.InstallationMethodId))
            .OrderByDescending(br => br.InstallationMethodId != null)
            .FirstOrDefaultAsync(cancellationToken);
        if (partnerRate != null)
        {
            var desc = !string.IsNullOrWhiteSpace(partnerRate.Description) ? partnerRate.Description : $"{order.OrderCategory?.Name ?? serviceCategory} - Order";
            return new ResolvedInvoiceLineDto { OrderId = orderId, Description = desc, Quantity = 1, UnitPrice = partnerRate.Amount, BillingRatecardId = partnerRate.Id };
        }
        partnerRate = await baseQuery
            .Where(br => br.PartnerId == order.PartnerId && br.InstallationMethodId == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (partnerRate != null)
        {
            var desc = !string.IsNullOrWhiteSpace(partnerRate.Description) ? partnerRate.Description : $"{order.OrderCategory?.Name ?? serviceCategory} - Order";
            return new ResolvedInvoiceLineDto { OrderId = orderId, Description = desc, Quantity = 1, UnitPrice = partnerRate.Amount, BillingRatecardId = partnerRate.Id };
        }

        // 2. Partner group rate
        if (partnerGroupId.HasValue)
        {
            var pgRate = await baseQuery
                .Where(br => br.PartnerGroupId == partnerGroupId && br.PartnerId == null)
                .Where(br => order.InstallationMethodId.HasValue ? br.InstallationMethodId == order.InstallationMethodId : true)
                .OrderByDescending(br => br.InstallationMethodId != null)
                .FirstOrDefaultAsync(cancellationToken);
            if (pgRate == null)
                pgRate = await baseQuery.Where(br => br.PartnerGroupId == partnerGroupId && br.PartnerId == null && br.InstallationMethodId == null).FirstOrDefaultAsync(cancellationToken);
            if (pgRate != null)
            {
                var desc = !string.IsNullOrWhiteSpace(pgRate.Description) ? pgRate.Description : $"{order.OrderCategory?.Name ?? serviceCategory} - Order";
                return new ResolvedInvoiceLineDto { OrderId = orderId, Description = desc, Quantity = 1, UnitPrice = pgRate.Amount, BillingRatecardId = pgRate.Id };
            }
        }

        // 3. Department rate
        if (order.DepartmentId.HasValue)
        {
            var deptRate = await baseQuery
                .Where(br => (br.DepartmentId == order.DepartmentId || br.DepartmentId == null) && br.PartnerId == null && br.PartnerGroupId == null)
                .Where(br => order.InstallationMethodId.HasValue ? br.InstallationMethodId == order.InstallationMethodId : true)
                .OrderByDescending(br => br.DepartmentId != null)
                .OrderByDescending(br => br.InstallationMethodId != null)
                .FirstOrDefaultAsync(cancellationToken);
            if (deptRate == null)
                deptRate = await baseQuery.Where(br => (br.DepartmentId == order.DepartmentId || br.DepartmentId == null) && br.PartnerId == null && br.PartnerGroupId == null && br.InstallationMethodId == null).FirstOrDefaultAsync(cancellationToken);
            if (deptRate != null)
            {
                var desc = !string.IsNullOrWhiteSpace(deptRate.Description) ? deptRate.Description : $"{order.OrderCategory?.Name ?? serviceCategory} - Order";
                return new ResolvedInvoiceLineDto { OrderId = orderId, Description = desc, Quantity = 1, UnitPrice = deptRate.Amount, BillingRatecardId = deptRate.Id };
            }
        }

        // 4. Company default (no partner, no department)
        var defaultRate = await baseQuery
            .Where(br => br.PartnerId == null && br.PartnerGroupId == null && br.DepartmentId == null)
            .Where(br => order.InstallationMethodId.HasValue ? br.InstallationMethodId == order.InstallationMethodId : true)
            .OrderByDescending(br => br.InstallationMethodId != null)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultRate == null)
            defaultRate = await baseQuery.Where(br => br.PartnerId == null && br.PartnerGroupId == null && br.DepartmentId == null && br.InstallationMethodId == null).FirstOrDefaultAsync(cancellationToken);
        if (defaultRate != null)
        {
            var desc = !string.IsNullOrWhiteSpace(defaultRate.Description) ? defaultRate.Description : $"{order.OrderCategory?.Name ?? serviceCategory} - Order";
            return new ResolvedInvoiceLineDto { OrderId = orderId, Description = desc, Quantity = 1, UnitPrice = defaultRate.Amount, BillingRatecardId = defaultRate.Id };
        }

        _logger.LogWarning("No BillingRatecard found for Order {OrderId} (PartnerId={PartnerId}, OrderTypeId={OrderTypeId}, ServiceCategory={ServiceCategory}).", orderId, order.PartnerId, order.OrderTypeId, serviceCategory);
        return null;
    }

    public async Task<BuildInvoiceLinesResult> BuildInvoiceLinesFromOrdersAsync(IReadOnlyList<Guid> orderIds, Guid companyId, DateTime? referenceDate = null, CancellationToken cancellationToken = default)
    {
        FinancialIsolationGuard.RequireTenantOrBypass("BuildInvoiceLinesFromOrders");
        FinancialIsolationGuard.RequireCompany(companyId, "BuildInvoiceLinesFromOrders");

        var result = new BuildInvoiceLinesResult();
        if (orderIds == null || orderIds.Count == 0)
            return result;
        foreach (var orderId in orderIds.Distinct())
        {
            var resolved = await ResolveInvoiceLineFromOrderAsync(orderId, companyId, referenceDate, cancellationToken);
            if (resolved != null)
            {
                result.LineItems.Add(new CreateInvoiceLineItemDto
                {
                    OrderId = orderId,
                    Description = resolved.Description,
                    Quantity = resolved.Quantity,
                    UnitPrice = resolved.UnitPrice
                });
            }
            else
            {
                result.UnresolvedOrderIds.Add(orderId);
                result.Messages.Add($"Order {orderId}: no BillingRatecard match or order missing OrderCategoryId.");
            }
        }
        return result;
    }

    private async Task<string> GenerateInvoiceNumberAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow;
        var prefix = $"INV-{today:yyyyMMdd}";
        
        var lastInvoice = await _context.Invoices
            .Where(i => i.CompanyId == companyId && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        return $"{prefix}-{sequence:D4}";
    }

    private static InvoiceDto MapToInvoiceDto(Invoice invoice, Dictionary<Guid, Partner>? partners = null, Company? company = null, Dictionary<Guid, (string CustomerName, string ServiceId, string OrderType, string DocketNo)>? orderData = null, decimal? taxRate = null)
    {
        partners ??= new Dictionary<Guid, Partner>();
        orderData ??= new Dictionary<Guid, (string, string, string, string)>();
        var partner = partners.TryGetValue(invoice.PartnerId, out var p) ? p : null;

        // When taxRate is provided, recompute line totals and invoice totals so display always tallies
        var rate = taxRate ?? 0.06m;
        var lineItemDtos = invoice.LineItems.Select(li =>
        {
            var od = li.OrderId.HasValue && orderData.TryGetValue(li.OrderId.Value, out var d) ? d : default;
            var lineTotal = li.Quantity * li.UnitPrice;
            return new InvoiceLineItemDto
            {
                Id = li.Id,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Total = lineTotal,
                OrderId = li.OrderId,
                CustomerName = od.CustomerName,
                ServiceId = od.ServiceId,
                OrderType = od.OrderType,
                DocketNo = od.DocketNo
            };
        }).ToList();

        decimal subTotal;
        decimal taxAmount;
        decimal totalAmount;
        if (taxRate.HasValue)
        {
            subTotal = lineItemDtos.Sum(li => li.Total);
            taxAmount = subTotal * rate;
            totalAmount = subTotal + taxAmount;
        }
        else
        {
            subTotal = invoice.SubTotal;
            taxAmount = invoice.TaxAmount;
            totalAmount = invoice.TotalAmount;
        }

        var billToSubject = lineItemDtos.FirstOrDefault(li => !string.IsNullOrEmpty(li.OrderType))?.OrderType ?? "Non Prelaid Activation";

        return new InvoiceDto
        {
            Id = invoice.Id,
            CompanyId = invoice.CompanyId,
            InvoiceNumber = invoice.InvoiceNumber,
            PartnerId = invoice.PartnerId,
            PartnerName = partner?.Name ?? string.Empty,
            PartnerAddress = partner?.BillingAddress,
            PartnerContactName = !string.IsNullOrEmpty(partner?.ContactName) ? partner.ContactName : "Finance Department",
            PartnerContactEmail = partner?.ContactEmail,
            PartnerContactPhone = partner?.ContactPhone,
            BillToSubject = billToSubject,
            InvoiceDate = invoice.InvoiceDate,
            TermsInDays = invoice.TermsInDays,
            DueDate = invoice.DueDate,
            TotalAmount = totalAmount,
            TaxAmount = taxAmount,
            SubTotal = subTotal,
            DoRefNo = null,
            PurchaseOrderNo = null,
            Status = invoice.Status,
            SubmissionId = invoice.SubmissionId,
            SubmittedAt = invoice.SubmittedAt,
            PaidAt = invoice.PaidAt,
            LineItems = lineItemDtos,
            CreatedAt = invoice.CreatedAt,
            CompanyLetterhead = company != null ? new CompanyLetterheadDto
            {
                Name = company.LegalName ?? company.ShortName ?? "CephasOps",
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                RegistrationNo = company.RegistrationNo
            } : null
        };
    }
}

