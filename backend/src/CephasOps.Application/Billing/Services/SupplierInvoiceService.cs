using CephasOps.Application.Billing.DTOs;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Billing.Enums;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Supplier Invoice service implementation
/// </summary>
public class SupplierInvoiceService : ISupplierInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SupplierInvoiceService> _logger;

    public SupplierInvoiceService(ApplicationDbContext context, ILogger<SupplierInvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SupplierInvoiceDto>> GetSupplierInvoicesAsync(Guid? companyId, SupplierInvoiceStatus? status = null, string? supplierName = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .Include(i => i.LineItems)
            .AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(supplierName))
        {
            query = query.Where(i => EF.Functions.ILike(i.SupplierName, $"%{supplierName}%"));
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

        return invoices.Select(MapToDto).ToList();
    }

    public async Task<SupplierInvoiceDto?> GetSupplierInvoiceByIdAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .Include(i => i.LineItems)
            .Where(i => i.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);
        return invoice != null ? MapToDto(invoice) : null;
    }

    public async Task<SupplierInvoiceDto> CreateSupplierInvoiceAsync(CreateSupplierInvoiceDto dto, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var invoice = new SupplierInvoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            InvoiceNumber = dto.InvoiceNumber.Trim(),
            InternalReference = dto.InternalReference,
            SupplierName = dto.SupplierName.Trim(),
            SupplierTaxNumber = dto.SupplierTaxNumber,
            SupplierAddress = dto.SupplierAddress,
            SupplierEmail = dto.SupplierEmail,
            InvoiceDate = dto.InvoiceDate,
            ReceivedDate = DateTime.UtcNow,
            DueDate = dto.DueDate,
            Currency = dto.Currency,
            Status = SupplierInvoiceStatus.Draft,
            CostCentreId = dto.CostCentreId,
            DefaultPnlTypeId = dto.DefaultPnlTypeId,
            Description = dto.Description,
            Notes = dto.Notes,
            AttachmentPath = dto.AttachmentPath,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calculate totals from line items
        decimal subTotal = 0;
        decimal taxAmount = 0;
        int lineNumber = 1;

        foreach (var lineDto in dto.LineItems)
        {
            var lineTotal = lineDto.Quantity * lineDto.UnitPrice;
            var lineTax = lineTotal * lineDto.TaxRate / 100;
            var totalWithTax = lineTotal + lineTax;

            var lineItem = new SupplierInvoiceLineItem
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                SupplierInvoiceId = invoice.Id,
                LineNumber = lineNumber++,
                Description = lineDto.Description,
                Quantity = lineDto.Quantity,
                UnitOfMeasure = lineDto.UnitOfMeasure,
                UnitPrice = lineDto.UnitPrice,
                LineTotal = lineTotal,
                TaxRate = lineDto.TaxRate,
                TaxAmount = lineTax,
                TotalWithTax = totalWithTax,
                PnlTypeId = lineDto.PnlTypeId ?? dto.DefaultPnlTypeId,
                CostCentreId = lineDto.CostCentreId ?? dto.CostCentreId,
                AssetId = lineDto.AssetId,
                Notes = lineDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            invoice.LineItems.Add(lineItem);
            subTotal += lineTotal;
            taxAmount += lineTax;
        }

        invoice.SubTotal = subTotal;
        invoice.TaxAmount = taxAmount;
        invoice.TotalAmount = subTotal + taxAmount;
        invoice.OutstandingAmount = invoice.TotalAmount;

        _context.SupplierInvoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Supplier invoice created: {InvoiceId}, Number: {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);

        return MapToDto(invoice);
    }

    public async Task<SupplierInvoiceDto> UpdateSupplierInvoiceAsync(Guid id, UpdateSupplierInvoiceDto dto, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .Include(i => i.LineItems)
            .Where(i => i.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);
        if (invoice == null)
        {
            throw new KeyNotFoundException($"Supplier Invoice with ID {id} not found");
        }

        if (!string.IsNullOrEmpty(dto.InvoiceNumber)) invoice.InvoiceNumber = dto.InvoiceNumber.Trim();
        if (dto.InternalReference != null) invoice.InternalReference = dto.InternalReference;
        if (!string.IsNullOrEmpty(dto.SupplierName)) invoice.SupplierName = dto.SupplierName.Trim();
        if (dto.SupplierTaxNumber != null) invoice.SupplierTaxNumber = dto.SupplierTaxNumber;
        if (dto.SupplierAddress != null) invoice.SupplierAddress = dto.SupplierAddress;
        if (dto.SupplierEmail != null) invoice.SupplierEmail = dto.SupplierEmail;
        if (dto.InvoiceDate.HasValue) invoice.InvoiceDate = dto.InvoiceDate.Value;
        if (dto.DueDate.HasValue) invoice.DueDate = dto.DueDate;
        if (dto.Status.HasValue) invoice.Status = dto.Status.Value;
        if (dto.CostCentreId.HasValue) invoice.CostCentreId = dto.CostCentreId;
        if (dto.DefaultPnlTypeId.HasValue) invoice.DefaultPnlTypeId = dto.DefaultPnlTypeId;
        if (dto.Description != null) invoice.Description = dto.Description;
        if (dto.Notes != null) invoice.Notes = dto.Notes;
        if (dto.AttachmentPath != null) invoice.AttachmentPath = dto.AttachmentPath;
        invoice.UpdatedAt = DateTime.UtcNow;

        // Check and update status for overdue
        if (invoice.DueDate.HasValue && invoice.DueDate.Value < DateTime.UtcNow && invoice.Status == SupplierInvoiceStatus.Approved)
        {
            invoice.Status = SupplierInvoiceStatus.Overdue;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Supplier invoice updated: {InvoiceId}", id);

        return MapToDto(invoice);
    }

    public async Task DeleteSupplierInvoiceAsync(Guid id, Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices.Where(i => i.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);
        if (invoice == null)
        {
            throw new KeyNotFoundException($"Supplier Invoice with ID {id} not found");
        }

        if (invoice.AmountPaid > 0)
        {
            throw new InvalidOperationException("Cannot delete invoice with payments. Void the payments first.");
        }

        _context.SupplierInvoices.Remove(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Supplier invoice deleted: {InvoiceId}", id);
    }

    public async Task<SupplierInvoiceDto> ApproveSupplierInvoiceAsync(Guid id, Guid? companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .Include(i => i.LineItems)
            .Where(i => i.Id == id);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoice = await query.FirstOrDefaultAsync(cancellationToken);
        if (invoice == null)
        {
            throw new KeyNotFoundException($"Supplier Invoice with ID {id} not found");
        }

        invoice.Status = SupplierInvoiceStatus.Approved;
        invoice.ApprovedByUserId = userId;
        invoice.ApprovedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Supplier invoice approved: {InvoiceId} by user {UserId}", id, userId);

        return MapToDto(invoice);
    }

    public async Task<SupplierInvoiceSummaryDto> GetSupplierInvoiceSummaryAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices.AsQueryable();

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoices = await query.ToListAsync(cancellationToken);

        var thisMonth = DateTime.UtcNow.ToString("yyyy-MM");

        return new SupplierInvoiceSummaryDto
        {
            TotalInvoices = invoices.Count,
            PendingApproval = invoices.Count(i => i.Status == SupplierInvoiceStatus.PendingApproval || i.Status == SupplierInvoiceStatus.Draft),
            OverdueInvoices = invoices.Count(i => i.Status == SupplierInvoiceStatus.Overdue || (i.DueDate.HasValue && i.DueDate.Value < DateTime.UtcNow && i.OutstandingAmount > 0)),
            TotalOutstanding = invoices.Sum(i => i.OutstandingAmount),
            TotalPaid = invoices.Sum(i => i.AmountPaid),
            TotalThisMonth = invoices.Where(i => i.InvoiceDate.ToString("yyyy-MM") == thisMonth).Sum(i => i.TotalAmount),
            ByStatus = invoices
                .GroupBy(i => i.Status)
                .Select(g => new SupplierInvoicesByStatusDto
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(i => i.TotalAmount)
                })
                .OrderBy(x => x.Status)
                .ToList()
        };
    }

    public async Task<List<SupplierInvoiceDto>> GetOverdueInvoicesAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierInvoices
            .Include(i => i.LineItems)
            .Where(i => i.OutstandingAmount > 0 && i.DueDate.HasValue && i.DueDate.Value < DateTime.UtcNow);

        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        var invoices = await query
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

        return invoices.Select(MapToDto).ToList();
    }

    private static SupplierInvoiceDto MapToDto(SupplierInvoice invoice)
    {
        return new SupplierInvoiceDto
        {
            Id = invoice.Id,
            CompanyId = invoice.CompanyId,
            InvoiceNumber = invoice.InvoiceNumber,
            InternalReference = invoice.InternalReference,
            SupplierName = invoice.SupplierName,
            SupplierTaxNumber = invoice.SupplierTaxNumber,
            SupplierAddress = invoice.SupplierAddress,
            SupplierEmail = invoice.SupplierEmail,
            InvoiceDate = invoice.InvoiceDate,
            ReceivedDate = invoice.ReceivedDate,
            DueDate = invoice.DueDate,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            AmountPaid = invoice.AmountPaid,
            OutstandingAmount = invoice.OutstandingAmount,
            Currency = invoice.Currency,
            Status = invoice.Status,
            CostCentreId = invoice.CostCentreId,
            DefaultPnlTypeId = invoice.DefaultPnlTypeId,
            Description = invoice.Description,
            Notes = invoice.Notes,
            AttachmentPath = invoice.AttachmentPath,
            CreatedByUserId = invoice.CreatedByUserId,
            ApprovedByUserId = invoice.ApprovedByUserId,
            ApprovedAt = invoice.ApprovedAt,
            PaidAt = invoice.PaidAt,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            LineItems = invoice.LineItems?.Select(l => new SupplierInvoiceLineItemDto
            {
                Id = l.Id,
                SupplierInvoiceId = l.SupplierInvoiceId,
                LineNumber = l.LineNumber,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitOfMeasure = l.UnitOfMeasure,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                TaxRate = l.TaxRate,
                TaxAmount = l.TaxAmount,
                TotalWithTax = l.TotalWithTax,
                PnlTypeId = l.PnlTypeId,
                CostCentreId = l.CostCentreId,
                AssetId = l.AssetId,
                Notes = l.Notes
            }).ToList() ?? new List<SupplierInvoiceLineItemDto>()
        };
    }
}

