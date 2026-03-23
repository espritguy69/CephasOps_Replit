using CephasOps.Application.Files.DTOs;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Domain.Settings.Enums;
using CephasOps.Infrastructure.Persistence;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Settings.Services;

/// <summary>
/// Document generation service - TEMPLATE-DRIVEN implementation
/// 
/// HOW IT WORKS:
/// 1. Users create HTML templates with Handlebars placeholders in Settings → Document Templates
/// 2. When generating a document, this service:
///    a) Loads the template from database
///    b) Fetches entity data (Invoice, Order, etc.)
///    c) Renders HTML using Handlebars
///    d) Converts HTML to PDF using QuestPDF
///    e) Saves to file storage
/// 
/// NO HARDCODED LAYOUTS - everything is user-configurable through templates.
/// 
/// SUPPORTED DOCUMENT TYPES:
/// - Invoice: Partner invoices
/// - JobDocket: Work orders for installers
/// - RmaForm: Return merchandise authorization
/// - PurchaseOrder: Purchase orders to suppliers
/// - Quotation: Customer quotations
/// - BOQ: Bill of quantities for projects
/// - DeliveryOrder: Delivery orders for shipments
/// - PaymentReceipt: Payment receipts
/// - Generic: Any custom document (user provides JSON data)
/// </summary>
public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentTemplateService _templateService;
    private readonly IFileService _fileService;
    private readonly ICarboneRenderer _carboneRenderer;
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly IHandlebars _handlebars;
    private const int PdfGenerationMaxRetries = 2;

    static DocumentGenerationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentGenerationService(
        ApplicationDbContext context,
        IDocumentTemplateService templateService,
        IFileService fileService,
        ICarboneRenderer carboneRenderer,
        ILogger<DocumentGenerationService> logger)
    {
        _context = context;
        _templateService = templateService;
        _fileService = fileService;
        _carboneRenderer = carboneRenderer;
        _logger = logger;
        
        _handlebars = Handlebars.Create();
        RegisterHandlebarsHelpers();
    }

    #region Handlebars Helpers

    private void RegisterHandlebarsHelpers()
    {
        // {{currency amount}} → RM 1,234.56
        _handlebars.RegisterHelper("currency", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && decimal.TryParse(parameters[0]?.ToString(), out var amount))
            {
                writer.WriteSafeString($"RM {amount:N2}");
            }
        });

        // {{date value "dd MMM yyyy"}} → 15 Dec 2024
        _handlebars.RegisterHelper("date", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && DateTime.TryParse(parameters[0]?.ToString(), out var date))
            {
                var format = parameters.Length > 1 ? parameters[1]?.ToString() ?? "dd MMM yyyy" : "dd MMM yyyy";
                writer.WriteSafeString(date.ToString(format));
            }
        });

        // {{time value}} → 09:30
        _handlebars.RegisterHelper("time", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && TimeSpan.TryParse(parameters[0]?.ToString(), out var time))
            {
                writer.WriteSafeString(time.ToString(@"hh\:mm"));
            }
        });

        // {{number value 2}} → 1,234.56
        _handlebars.RegisterHelper("number", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && decimal.TryParse(parameters[0]?.ToString(), out var number))
            {
                var decimals = parameters.Length > 1 && int.TryParse(parameters[1]?.ToString(), out var d) ? d : 2;
                writer.WriteSafeString(number.ToString($"N{decimals}"));
            }
        });

        // {{rowIndex @index}} → 1 (1-based index)
        _handlebars.RegisterHelper("rowIndex", (writer, context, parameters) =>
        {
            if (parameters.Length > 0 && int.TryParse(parameters[0]?.ToString(), out var index))
            {
                writer.WriteSafeString((index + 1).ToString());
            }
        });

        // {{uppercase value}} → VALUE
        _handlebars.RegisterHelper("uppercase", (writer, context, parameters) =>
        {
            if (parameters.Length > 0)
            {
                writer.WriteSafeString(parameters[0]?.ToString()?.ToUpper() ?? "");
            }
        });

        // {{lowercase value}} → value
        _handlebars.RegisterHelper("lowercase", (writer, context, parameters) =>
        {
            if (parameters.Length > 0)
            {
                writer.WriteSafeString(parameters[0]?.ToString()?.ToLower() ?? "");
            }
        });
    }

    #endregion

    #region Main Generation Methods

    public async Task<GeneratedDocumentDto> GenerateInvoiceDocumentAsync(Guid invoiceId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating invoice document for invoice {InvoiceId}", invoiceId);

        ValidateEntitySetExists(_context.Invoices, "Invoice");

        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, cancellationToken);

        if (invoice == null)
            throw new KeyNotFoundException($"Invoice entity with ID {invoiceId} not found for company {companyId}");

        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);
        var company = invoice.CompanyId != Guid.Empty
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken)
            : null;

        // Load order data for line items (customerName, serviceId, orderType, docketNo)
        var orderIds = invoice.LineItems.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList();
        var orderData = await LoadOrderDataForInvoiceLineItemsAsync(orderIds, cancellationToken);

        var billToSubject = invoice.LineItems
            .Where(li => li.OrderId.HasValue && orderData.TryGetValue(li.OrderId!.Value, out var od) && !string.IsNullOrEmpty(od.OrderType))
            .Select(li => orderData[li.OrderId!.Value].OrderType)
            .FirstOrDefault() ?? "Non Prelaid Activation";

        var template = await GetTemplateAsync(companyId, "Invoice", invoice.PartnerId, templateId, cancellationToken);

        // Build data model - all fields available as {{invoice.number}}, {{partner.name}}, etc.
        var lineItemsData = new List<Dictionary<string, object?>>();
        for (var i = 0; i < invoice.LineItems.Count; i++)
        {
            var item = invoice.LineItems.ElementAt(i);
            var od = item.OrderId.HasValue && orderData.TryGetValue(item.OrderId.Value, out var d) ? d : default;
            var hasOrderData = !string.IsNullOrEmpty(od.CustomerName) || !string.IsNullOrEmpty(od.ServiceId) || !string.IsNullOrEmpty(od.OrderType) || !string.IsNullOrEmpty(od.DocketNo);
            var description = hasOrderData
                ? $"CUSTOMER NAME: {od.CustomerName}\nSERVICE ID: {od.ServiceId}\nORDER TYPE: {od.OrderType}\nDOCKET NO: {od.DocketNo}"
                : (item.Description ?? "");

            lineItemsData.Add(new Dictionary<string, object?>
            {
                ["index"] = i + 1,
                ["description"] = description,
                ["quantity"] = item.Quantity,
                ["unitPrice"] = item.UnitPrice,
                ["total"] = item.Total,
                ["customerName"] = od.CustomerName,
                ["serviceId"] = od.ServiceId,
                ["orderType"] = od.OrderType,
                ["docketNo"] = od.DocketNo
            });
        }

        var partnerAddress = partner?.BillingAddress != null
            ? string.Join("\n", partner.BillingAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()))
            : "";
        var personInCharge = !string.IsNullOrEmpty(partner?.ContactName) ? partner.ContactName : "Finance Department";

        var data = new Dictionary<string, object>
        {
            ["invoice"] = new Dictionary<string, object?>
            {
                ["id"] = invoice.Id.ToString(),
                ["number"] = invoice.InvoiceNumber,
                ["date"] = invoice.InvoiceDate.ToString("yyyy-MM-dd"),
                ["dueDate"] = invoice.DueDate?.ToString("yyyy-MM-dd"),
                ["status"] = invoice.Status,
                ["termsInDays"] = invoice.TermsInDays,
                ["subTotal"] = invoice.SubTotal,
                ["taxAmount"] = invoice.TaxAmount,
                ["totalAmount"] = invoice.TotalAmount
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["id"] = partner?.Id.ToString(),
                ["name"] = partner?.Name ?? "Unknown Partner",
                ["address"] = partnerAddress,
                ["contactName"] = personInCharge,
                ["contactPhone"] = partner?.ContactPhone ?? "",
                ["contactEmail"] = partner?.ContactEmail ?? ""
            },
            ["billToSubject"] = billToSubject,
            ["lineItems"] = lineItemsData,
            ["company"] = new Dictionary<string, object?>
            {
                ["name"] = company?.LegalName ?? company?.ShortName ?? "CephasOps",
                ["letterhead"] = company != null ? new Dictionary<string, object?>
                {
                    ["name"] = company.LegalName ?? company.ShortName ?? "CephasOps",
                    ["address"] = company.Address ?? "",
                    ["phone"] = company.Phone ?? "",
                    ["email"] = company.Email ?? "",
                    ["registrationNo"] = company.RegistrationNo ?? ""
                } : null
            },
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"Invoice_{invoice.InvoiceNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "Invoice", "Invoice", invoiceId, companyId,
            new { InvoiceNumber = invoice.InvoiceNumber, PartnerName = partner?.Name },
            cancellationToken);
    }

    public async Task<string> RenderInvoiceHtmlAsync(Guid invoiceId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rendering invoice HTML for preview {InvoiceId}", invoiceId);

        ValidateEntitySetExists(_context.Invoices, "Invoice");

        var invoice = await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, cancellationToken);

        if (invoice == null)
            throw new KeyNotFoundException($"Invoice entity with ID {invoiceId} not found for company {companyId}");

        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == invoice.PartnerId, cancellationToken);
        var company = invoice.CompanyId != Guid.Empty
            ? await _context.Companies.FirstOrDefaultAsync(c => c.Id == invoice.CompanyId, cancellationToken)
            : null;

        var orderIds = invoice.LineItems.Where(li => li.OrderId.HasValue).Select(li => li.OrderId!.Value).Distinct().ToList();
        var orderData = await LoadOrderDataForInvoiceLineItemsAsync(orderIds, cancellationToken);

        var billToSubject = invoice.LineItems
            .Where(li => li.OrderId.HasValue && orderData.TryGetValue(li.OrderId!.Value, out var od) && !string.IsNullOrEmpty(od.OrderType))
            .Select(li => orderData[li.OrderId!.Value].OrderType)
            .FirstOrDefault() ?? "Non Prelaid Activation";

        var template = await GetTemplateAsync(companyId, "Invoice", invoice.PartnerId, templateId, cancellationToken);

        var engineType = DocumentEngineTypeExtensions.ParseEngineType(template.Engine);
        if (engineType != DocumentEngineType.Handlebars && engineType != DocumentEngineType.CarboneHtml)
        {
            throw new InvalidOperationException($"HTML preview is only supported for Handlebars or CarboneHtml templates. Template '{template.Name}' uses {engineType}.");
        }
        // HtmlBody uses Handlebars syntax for both engines - compile and render

        var lineItemsData = new List<Dictionary<string, object?>>();
        for (var i = 0; i < invoice.LineItems.Count; i++)
        {
            var item = invoice.LineItems.ElementAt(i);
            var od = item.OrderId.HasValue && orderData.TryGetValue(item.OrderId.Value, out var d) ? d : default;
            var hasOrderData = !string.IsNullOrEmpty(od.CustomerName) || !string.IsNullOrEmpty(od.ServiceId) || !string.IsNullOrEmpty(od.OrderType) || !string.IsNullOrEmpty(od.DocketNo);
            var description = hasOrderData
                ? $"CUSTOMER NAME: {od.CustomerName}\nSERVICE ID: {od.ServiceId}\nORDER TYPE: {od.OrderType}\nDOCKET NO: {od.DocketNo}"
                : (item.Description ?? "");

            lineItemsData.Add(new Dictionary<string, object?>
            {
                ["index"] = i + 1,
                ["description"] = description,
                ["quantity"] = item.Quantity,
                ["unitPrice"] = item.UnitPrice,
                ["total"] = item.Total,
                ["customerName"] = od.CustomerName,
                ["serviceId"] = od.ServiceId,
                ["orderType"] = od.OrderType,
                ["docketNo"] = od.DocketNo
            });
        }

        var partnerAddress = partner?.BillingAddress != null
            ? string.Join("\n", partner.BillingAddress.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()))
            : "";
        var personInCharge = !string.IsNullOrEmpty(partner?.ContactName) ? partner.ContactName : "Finance Department";

        var data = new Dictionary<string, object>
        {
            ["invoice"] = new Dictionary<string, object?>
            {
                ["id"] = invoice.Id.ToString(),
                ["number"] = invoice.InvoiceNumber,
                ["date"] = invoice.InvoiceDate.ToString("yyyy-MM-dd"),
                ["dueDate"] = invoice.DueDate?.ToString("yyyy-MM-dd"),
                ["status"] = invoice.Status,
                ["termsInDays"] = invoice.TermsInDays,
                ["subTotal"] = invoice.SubTotal,
                ["taxAmount"] = invoice.TaxAmount,
                ["totalAmount"] = invoice.TotalAmount
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["id"] = partner?.Id.ToString(),
                ["name"] = partner?.Name ?? "Unknown Partner",
                ["address"] = partnerAddress,
                ["contactName"] = personInCharge,
                ["contactPhone"] = partner?.ContactPhone ?? "",
                ["contactEmail"] = partner?.ContactEmail ?? ""
            },
            ["billToSubject"] = billToSubject,
            ["lineItems"] = lineItemsData,
            ["company"] = new Dictionary<string, object?>
            {
                ["name"] = company?.LegalName ?? company?.ShortName ?? "CephasOps",
                ["letterhead"] = company != null ? new Dictionary<string, object?>
                {
                    ["name"] = company.LegalName ?? company.ShortName ?? "CephasOps",
                    ["address"] = company.Address ?? "",
                    ["phone"] = company.Phone ?? "",
                    ["email"] = company.Email ?? "",
                    ["registrationNo"] = company.RegistrationNo ?? ""
                } : null
            },
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        var compiledTemplate = _handlebars.Compile(template.HtmlBody);
        return compiledTemplate(data);
    }

    private async Task<Dictionary<Guid, (string CustomerName, string ServiceId, string OrderType, string DocketNo)>> LoadOrderDataForInvoiceLineItemsAsync(
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

    public async Task<GeneratedDocumentDto> GenerateJobDocketAsync(Guid orderId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating job docket for order {OrderId}", orderId);

        ValidateEntitySetExists(_context.Orders, "Order");

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == companyId, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException($"Order entity with ID {orderId} not found for company {companyId}");

        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == order.PartnerId, cancellationToken);
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.Id == order.BuildingId, cancellationToken);
        var assignedSi = order.AssignedSiId.HasValue
            ? await _context.ServiceInstallers.FirstOrDefaultAsync(s => s.Id == order.AssignedSiId.Value, cancellationToken)
            : null;
        var orderType = await _context.OrderTypes.FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);

        var template = await GetTemplateAsync(companyId, "JobDocket", order.PartnerId, templateId, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["order"] = new Dictionary<string, object?>
            {
                ["id"] = order.Id.ToString(),
                ["serviceId"] = order.ServiceId,
                ["ticketId"] = order.TicketId,
                ["status"] = order.Status,
                ["priority"] = order.Priority ?? "Normal",
                ["appointmentDate"] = order.AppointmentDate.ToString("yyyy-MM-dd"),
                ["appointmentWindowFrom"] = order.AppointmentWindowFrom.ToString(@"hh\:mm"),
                ["appointmentWindowTo"] = order.AppointmentWindowTo.ToString(@"hh\:mm"),
                ["docketNumber"] = order.DocketNumber,
                ["partnerNotes"] = order.PartnerNotes,
                ["internalNotes"] = order.OrderNotesInternal
            },
            ["customer"] = new Dictionary<string, object?>
            {
                ["name"] = order.CustomerName,
                ["phone"] = order.CustomerPhone,
                ["email"] = order.CustomerEmail
            },
            ["location"] = new Dictionary<string, object?>
            {
                ["buildingName"] = building?.Name ?? "Unknown",
                ["unitNo"] = order.UnitNo,
                ["addressLine1"] = order.AddressLine1,
                ["addressLine2"] = order.AddressLine2,
                ["city"] = order.City,
                ["state"] = order.State,
                ["postcode"] = order.Postcode,
                ["fullAddress"] = $"{order.AddressLine1}, {order.City}, {order.State} {order.Postcode}"
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["id"] = partner?.Id.ToString(),
                ["name"] = partner?.Name ?? "Unknown"
            },
            ["installer"] = new Dictionary<string, object?>
            {
                ["id"] = assignedSi?.Id.ToString(),
                ["name"] = assignedSi?.Name ?? "Not Assigned",
                ["phone"] = assignedSi?.Phone
            },
            ["orderType"] = new Dictionary<string, object?>
            {
                ["id"] = orderType?.Id.ToString(),
                ["name"] = orderType?.Name ?? "Unknown"
            },
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"JobDocket_{order.ServiceId}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "JobDocket", "Order", orderId, companyId,
            new { ServiceId = order.ServiceId, CustomerName = order.CustomerName },
            cancellationToken);
    }

    public async Task<GeneratedDocumentDto> GenerateRmaFormAsync(Guid rmaRequestId, Guid companyId, Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating RMA form for RMA request {RmaRequestId}", rmaRequestId);

        ValidateEntitySetExists(_context.RmaRequests, "RmaRequest");

        var rmaRequest = await _context.RmaRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == rmaRequestId && r.CompanyId == companyId, cancellationToken);

        if (rmaRequest == null)
            throw new KeyNotFoundException($"RMA request entity with ID {rmaRequestId} not found for company {companyId}");

        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == rmaRequest.PartnerId, cancellationToken);

        var template = await GetTemplateAsync(companyId, "RmaForm", null, templateId, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["rma"] = new Dictionary<string, object?>
            {
                ["id"] = rmaRequest.Id.ToString(),
                ["number"] = rmaRequest.RmaNumber,
                ["requestDate"] = rmaRequest.RequestDate.ToString("yyyy-MM-dd"),
                ["reason"] = rmaRequest.Reason,
                ["status"] = rmaRequest.Status
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["id"] = partner?.Id.ToString(),
                ["name"] = partner?.Name ?? "Unknown"
            },
            ["items"] = rmaRequest.Items.Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["serialisedItemId"] = item.SerialisedItemId.ToString(),
                ["notes"] = item.Notes,
                ["result"] = item.Result ?? "Pending"
            }).ToList(),
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"RMA_{rmaRequest.RmaNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "RmaForm", "RmaRequest", rmaRequestId, companyId,
            new { RmaNumber = rmaRequest.RmaNumber },
            cancellationToken);
    }

    public async Task<GeneratedDocumentDto> GenerateDocumentAsync(GenerateDocumentDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating document: type {DocumentType}, reference {ReferenceEntity}/{ReferenceId}",
            dto.DocumentType, dto.ReferenceEntity, dto.ReferenceId);

        return dto.DocumentType.ToLower() switch
        {
            "invoice" => await GenerateInvoiceDocumentAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            "jobdocket" => await GenerateJobDocketAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            "rmaform" => await GenerateRmaFormAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            
            // Additional document types
            "purchaseorder" or "po" => await GeneratePurchaseOrderAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            "quotation" or "quote" => await GenerateQuotationAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            "boq" or "billofquantities" => await GenerateBoqAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            "deliveryorder" or "do" => await GenerateDeliveryOrderAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            "paymentreceipt" => await GeneratePaymentReceiptAsync(dto.ReferenceId, companyId, dto.TemplateId, cancellationToken),
            
            // Generic - user provides custom data
            _ => await GenerateGenericDocumentAsync(dto, companyId, userId, cancellationToken)
        };
    }

    #region Additional Document Types

    private async Task<GeneratedDocumentDto> GeneratePurchaseOrderAsync(Guid purchaseOrderId, Guid companyId, Guid? templateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating Purchase Order {PurchaseOrderId}", purchaseOrderId);

        ValidateEntitySetExists(_context.PurchaseOrders, "PurchaseOrder");

        var po = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId && p.CompanyId == companyId, cancellationToken);

        if (po == null)
            throw new KeyNotFoundException($"Purchase Order entity with ID {purchaseOrderId} not found for company {companyId}");

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == po.SupplierId, cancellationToken);

        var template = await GetTemplateAsync(companyId, "PurchaseOrder", null, templateId, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["po"] = new Dictionary<string, object?>
            {
                ["id"] = po.Id.ToString(),
                ["number"] = po.PoNumber,
                ["date"] = po.PoDate.ToString("yyyy-MM-dd"),
                ["expectedDeliveryDate"] = po.ExpectedDeliveryDate?.ToString("yyyy-MM-dd"),
                ["status"] = po.Status,
                ["deliveryAddress"] = po.DeliveryAddress,
                ["subTotal"] = po.SubTotal,
                ["taxAmount"] = po.TaxAmount,
                ["discountAmount"] = po.DiscountAmount,
                ["totalAmount"] = po.TotalAmount,
                ["currency"] = po.Currency,
                ["paymentTerms"] = po.PaymentTerms,
                ["termsAndConditions"] = po.TermsAndConditions,
                ["notes"] = po.Notes
            },
            ["supplier"] = new Dictionary<string, object?>
            {
                ["id"] = supplier?.Id.ToString(),
                ["name"] = supplier?.Name ?? "Unknown",
                ["code"] = supplier?.Code,
                ["contactPerson"] = supplier?.ContactPerson,
                ["phone"] = supplier?.Phone,
                ["email"] = supplier?.Email,
                ["address"] = supplier?.Address,
                ["city"] = supplier?.City,
                ["state"] = supplier?.State,
                ["postcode"] = supplier?.Postcode
            },
            ["items"] = po.Items.OrderBy(i => i.LineNumber).Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["lineNumber"] = item.LineNumber,
                ["description"] = item.Description,
                ["sku"] = item.Sku,
                ["unit"] = item.Unit,
                ["quantity"] = item.Quantity,
                ["unitPrice"] = item.UnitPrice,
                ["discountPercent"] = item.DiscountPercent,
                ["taxPercent"] = item.TaxPercent,
                ["total"] = item.Total,
                ["notes"] = item.Notes
            }).ToList(),
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"PO_{po.PoNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "PurchaseOrder", "PurchaseOrder", purchaseOrderId, companyId,
            new { PoNumber = po.PoNumber, SupplierName = supplier?.Name },
            cancellationToken);
    }

    private async Task<GeneratedDocumentDto> GenerateQuotationAsync(Guid quotationId, Guid companyId, Guid? templateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating Quotation {QuotationId}", quotationId);

        ValidateEntitySetExists(_context.Quotations, "Quotation");

        var quotation = await _context.Quotations
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == quotationId && q.CompanyId == companyId, cancellationToken);

        if (quotation == null)
            throw new KeyNotFoundException($"Quotation entity with ID {quotationId} not found for company {companyId}");

        var partner = quotation.PartnerId.HasValue
            ? await _context.Partners.FirstOrDefaultAsync(p => p.Id == quotation.PartnerId, cancellationToken)
            : null;

        var template = await GetTemplateAsync(companyId, "Quotation", quotation.PartnerId, templateId, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["quotation"] = new Dictionary<string, object?>
            {
                ["id"] = quotation.Id.ToString(),
                ["number"] = quotation.QuotationNumber,
                ["date"] = quotation.QuotationDate.ToString("yyyy-MM-dd"),
                ["validUntil"] = quotation.ValidUntil?.ToString("yyyy-MM-dd"),
                ["status"] = quotation.Status,
                ["subject"] = quotation.Subject,
                ["introduction"] = quotation.Introduction,
                ["subTotal"] = quotation.SubTotal,
                ["taxAmount"] = quotation.TaxAmount,
                ["discountAmount"] = quotation.DiscountAmount,
                ["totalAmount"] = quotation.TotalAmount,
                ["currency"] = quotation.Currency,
                ["paymentTerms"] = quotation.PaymentTerms,
                ["deliveryTerms"] = quotation.DeliveryTerms,
                ["termsAndConditions"] = quotation.TermsAndConditions,
                ["notes"] = quotation.Notes
            },
            ["customer"] = new Dictionary<string, object?>
            {
                ["name"] = quotation.CustomerName,
                ["phone"] = quotation.CustomerPhone,
                ["email"] = quotation.CustomerEmail,
                ["address"] = quotation.CustomerAddress
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["id"] = partner?.Id.ToString(),
                ["name"] = partner?.Name
            },
            ["items"] = quotation.Items.OrderBy(i => i.LineNumber).Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["lineNumber"] = item.LineNumber,
                ["itemType"] = item.ItemType,
                ["description"] = item.Description,
                ["sku"] = item.Sku,
                ["unit"] = item.Unit,
                ["quantity"] = item.Quantity,
                ["unitPrice"] = item.UnitPrice,
                ["discountPercent"] = item.DiscountPercent,
                ["taxPercent"] = item.TaxPercent,
                ["total"] = item.Total,
                ["notes"] = item.Notes
            }).ToList(),
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"Quote_{quotation.QuotationNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "Quotation", "Quotation", quotationId, companyId,
            new { QuotationNumber = quotation.QuotationNumber, CustomerName = quotation.CustomerName },
            cancellationToken);
    }

    private async Task<GeneratedDocumentDto> GenerateBoqAsync(Guid projectId, Guid companyId, Guid? templateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating BOQ for project {ProjectId}", projectId);

        ValidateEntitySetExists(_context.Projects, "Project");

        var project = await _context.Projects
            .Include(p => p.BoqItems)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId, cancellationToken);

        if (project == null)
            throw new KeyNotFoundException($"Project entity with ID {projectId} not found for company {companyId}");

        var partner = project.PartnerId.HasValue
            ? await _context.Partners.FirstOrDefaultAsync(p => p.Id == project.PartnerId, cancellationToken)
            : null;

        var template = await GetTemplateAsync(companyId, "BOQ", project.PartnerId, templateId, cancellationToken);

        var materialItems = project.BoqItems.Where(b => b.ItemType == "Material").OrderBy(b => b.LineNumber).ToList();
        var laborItems = project.BoqItems.Where(b => b.ItemType == "Labor").OrderBy(b => b.LineNumber).ToList();
        var otherItems = project.BoqItems.Where(b => b.ItemType != "Material" && b.ItemType != "Labor").OrderBy(b => b.LineNumber).ToList();

        var data = new Dictionary<string, object>
        {
            ["project"] = new Dictionary<string, object?>
            {
                ["id"] = project.Id.ToString(),
                ["code"] = project.ProjectCode,
                ["name"] = project.Name,
                ["description"] = project.Description,
                ["projectType"] = project.ProjectType,
                ["status"] = project.Status,
                ["customerName"] = project.CustomerName,
                ["customerPhone"] = project.CustomerPhone,
                ["customerEmail"] = project.CustomerEmail,
                ["siteAddress"] = project.SiteAddress,
                ["city"] = project.City,
                ["state"] = project.State,
                ["postcode"] = project.Postcode,
                ["startDate"] = project.StartDate?.ToString("yyyy-MM-dd"),
                ["endDate"] = project.EndDate?.ToString("yyyy-MM-dd"),
                ["budgetAmount"] = project.BudgetAmount,
                ["contractValue"] = project.ContractValue,
                ["currency"] = project.Currency
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["id"] = partner?.Id.ToString(),
                ["name"] = partner?.Name
            },
            ["materials"] = materialItems.Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["lineNumber"] = item.LineNumber,
                ["section"] = item.Section,
                ["description"] = item.Description,
                ["sku"] = item.Sku,
                ["unit"] = item.Unit,
                ["quantity"] = item.Quantity,
                ["unitRate"] = item.UnitRate,
                ["total"] = item.Total,
                ["markupPercent"] = item.MarkupPercent,
                ["sellingPrice"] = item.SellingPrice,
                ["isOptional"] = item.IsOptional
            }).ToList(),
            ["labor"] = laborItems.Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["lineNumber"] = item.LineNumber,
                ["section"] = item.Section,
                ["description"] = item.Description,
                ["unit"] = item.Unit,
                ["quantity"] = item.Quantity,
                ["unitRate"] = item.UnitRate,
                ["total"] = item.Total,
                ["markupPercent"] = item.MarkupPercent,
                ["sellingPrice"] = item.SellingPrice
            }).ToList(),
            ["otherItems"] = otherItems.Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["lineNumber"] = item.LineNumber,
                ["itemType"] = item.ItemType,
                ["section"] = item.Section,
                ["description"] = item.Description,
                ["unit"] = item.Unit,
                ["quantity"] = item.Quantity,
                ["unitRate"] = item.UnitRate,
                ["total"] = item.Total
            }).ToList(),
            ["summary"] = new Dictionary<string, object?>
            {
                ["materialTotal"] = materialItems.Sum(m => m.Total),
                ["laborTotal"] = laborItems.Sum(l => l.Total),
                ["otherTotal"] = otherItems.Sum(o => o.Total),
                ["grandTotal"] = project.BoqItems.Sum(b => b.Total),
                ["sellingTotal"] = project.BoqItems.Sum(b => b.SellingPrice)
            },
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"BOQ_{project.ProjectCode}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "BOQ", "Project", projectId, companyId,
            new { ProjectCode = project.ProjectCode, ProjectName = project.Name },
            cancellationToken);
    }

    private async Task<GeneratedDocumentDto> GenerateDeliveryOrderAsync(Guid deliveryOrderId, Guid companyId, Guid? templateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating Delivery Order {DeliveryOrderId}", deliveryOrderId);

        ValidateEntitySetExists(_context.DeliveryOrders, "DeliveryOrder");

        var deliveryOrder = await _context.DeliveryOrders
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == deliveryOrderId && d.CompanyId == companyId, cancellationToken);

        if (deliveryOrder == null)
            throw new KeyNotFoundException($"Delivery Order entity with ID {deliveryOrderId} not found for company {companyId}");

        var template = await GetTemplateAsync(companyId, "DeliveryOrder", null, templateId, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["deliveryOrder"] = new Dictionary<string, object?>
            {
                ["id"] = deliveryOrder.Id.ToString(),
                ["number"] = deliveryOrder.DoNumber,
                ["date"] = deliveryOrder.DoDate.ToString("yyyy-MM-dd"),
                ["type"] = deliveryOrder.DoType,
                ["status"] = deliveryOrder.Status,
                ["expectedDeliveryDate"] = deliveryOrder.ExpectedDeliveryDate?.ToString("yyyy-MM-dd"),
                ["actualDeliveryDate"] = deliveryOrder.ActualDeliveryDate?.ToString("yyyy-MM-dd"),
                ["deliveryPerson"] = deliveryOrder.DeliveryPerson,
                ["vehicleNumber"] = deliveryOrder.VehicleNumber,
                ["notes"] = deliveryOrder.Notes
            },
            ["recipient"] = new Dictionary<string, object?>
            {
                ["name"] = deliveryOrder.RecipientName,
                ["phone"] = deliveryOrder.RecipientPhone,
                ["email"] = deliveryOrder.RecipientEmail,
                ["address"] = deliveryOrder.DeliveryAddress,
                ["city"] = deliveryOrder.City,
                ["state"] = deliveryOrder.State,
                ["postcode"] = deliveryOrder.Postcode
            },
            ["items"] = deliveryOrder.Items.OrderBy(i => i.LineNumber).Select((item, index) => new Dictionary<string, object?>
            {
                ["index"] = index,
                ["lineNumber"] = item.LineNumber,
                ["description"] = item.Description,
                ["sku"] = item.Sku,
                ["unit"] = item.Unit,
                ["quantity"] = item.Quantity,
                ["quantityDelivered"] = item.QuantityDelivered,
                ["serialNumbers"] = item.SerialNumbers,
                ["notes"] = item.Notes
            }).ToList(),
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"DO_{deliveryOrder.DoNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "DeliveryOrder", "DeliveryOrder", deliveryOrderId, companyId,
            new { DoNumber = deliveryOrder.DoNumber, RecipientName = deliveryOrder.RecipientName },
            cancellationToken);
    }

    private async Task<GeneratedDocumentDto> GeneratePaymentReceiptAsync(Guid paymentId, Guid companyId, Guid? templateId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating Payment Receipt {PaymentId}", paymentId);

        ValidateEntitySetExists(_context.Payments, "Payment");

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.CompanyId == companyId, cancellationToken);

        if (payment == null)
            throw new KeyNotFoundException($"Payment entity with ID {paymentId} not found for company {companyId}");

        var template = await GetTemplateAsync(companyId, "PaymentReceipt", null, templateId, cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["payment"] = new Dictionary<string, object?>
            {
                ["id"] = payment.Id.ToString(),
                ["number"] = payment.PaymentNumber,
                ["date"] = payment.PaymentDate.ToString("yyyy-MM-dd"),
                ["type"] = payment.PaymentType.ToString(),
                ["method"] = payment.PaymentMethod.ToString(),
                ["amount"] = payment.Amount,
                ["currency"] = payment.Currency,
                ["bankAccount"] = payment.BankAccount,
                ["bankReference"] = payment.BankReference,
                ["chequeNumber"] = payment.ChequeNumber,
                ["description"] = payment.Description,
                ["notes"] = payment.Notes
            },
            ["payer"] = new Dictionary<string, object?>
            {
                ["name"] = payment.PayerPayeeName
            },
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"Receipt_{payment.PaymentNumber}_{DateTime.UtcNow:yyyyMMdd}.pdf",
            "PaymentReceipt", "Payment", paymentId, companyId,
            new { PaymentNumber = payment.PaymentNumber, PayerName = payment.PayerPayeeName },
            cancellationToken);
    }

    #endregion

    /// <summary>
    /// Generic document generation - for any custom document type
    /// User creates template and provides custom JSON data via API
    /// </summary>
    private async Task<GeneratedDocumentDto> GenerateGenericDocumentAsync(GenerateDocumentDto dto, Guid companyId, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating generic document: {DocumentType}", dto.DocumentType);

        var template = await GetTemplateAsync(companyId, dto.DocumentType, null, dto.TemplateId, cancellationToken);

        // For generic documents, data comes from API request or is empty
        var data = new Dictionary<string, object>
        {
            ["documentType"] = dto.DocumentType,
            ["referenceEntity"] = dto.ReferenceEntity,
            ["referenceId"] = dto.ReferenceId.ToString(),
            ["documentDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            ["generatedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")
        };

        return await GenerateFromTemplateAsync(
            template, data,
            $"{dto.DocumentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            dto.DocumentType, dto.ReferenceEntity, dto.ReferenceId, companyId,
            null, cancellationToken);
    }

    #endregion

    #region Core Template Rendering

    private async Task<DocumentTemplate> GetTemplateAsync(Guid companyId, string documentType, Guid? partnerId, Guid? templateId, CancellationToken cancellationToken)
    {
        DocumentTemplate? template = null;

        if (templateId.HasValue)
        {
            template = await _context.DocumentTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId.Value && t.CompanyId == companyId && t.IsActive, cancellationToken);
        }

        if (template == null && partnerId.HasValue)
        {
            template = await _context.DocumentTemplates
                .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.DocumentType == documentType && t.PartnerId == partnerId && t.IsActive, cancellationToken);
        }

        if (template == null)
        {
            template = await _context.DocumentTemplates
                .FirstOrDefaultAsync(t => t.CompanyId == companyId && t.DocumentType == documentType && t.PartnerId == null && t.IsActive, cancellationToken);
        }

        if (template == null)
        {
            throw new InvalidOperationException(
                $"No active template found for document type '{documentType}' (CompanyId: {companyId}). " +
                $"Please create a template in Settings → Document Templates with DocumentType = '{documentType}'.");
        }

        // Validate template has required fields based on engine type
        var engineType = DocumentEngineTypeExtensions.ParseEngineType(template.Engine);
        if (engineType == DocumentEngineType.CarboneDocx)
        {
            if (!template.TemplateFileId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Template '{template.Name}' (ID: {template.Id}) uses CarboneDocx engine but TemplateFileId is not set. " +
                    "Please upload a DOCX/ODT template file and set the TemplateFileId.");
            }
        }
        else if (engineType == DocumentEngineType.Handlebars || engineType == DocumentEngineType.CarboneHtml)
        {
            if (string.IsNullOrWhiteSpace(template.HtmlBody))
            {
                throw new InvalidOperationException(
                    $"Template '{template.Name}' (ID: {template.Id}) uses {engineType} engine but HtmlBody is empty. " +
                    "Please provide HTML template content in the HtmlBody field.");
            }
        }

        return template;
    }

    private async Task<GeneratedDocumentDto> GenerateFromTemplateAsync(
        DocumentTemplate template,
        object data,
        string fileName,
        string documentType,
        string referenceEntity,
        Guid referenceId,
        Guid companyId,
        object? metadata,
        CancellationToken cancellationToken)
    {
        // Parse engine type from template.Engine string
        var engineType = DocumentEngineTypeExtensions.ParseEngineType(template.Engine);
        
        _logger.LogInformation(
            "Generating document: Type={DocumentType}, Engine={Engine}, TemplateId={TemplateId}",
            documentType, engineType, template.Id);

        // Generate PDF based on engine type
        byte[] pdfBytes;
        
        switch (engineType)
        {
            case DocumentEngineType.CarboneHtml:
                // Carbone with HTML template from html_body
                _logger.LogDebug("Using Carbone HTML engine for {DocumentType}", documentType);
                pdfBytes = await ExecutePdfGenerationWithRetryAsync(
                    () => _carboneRenderer.RenderFromHtmlAsync(
                        template.HtmlBody,
                        data,
                        documentType,
                        cancellationToken),
                    engineType.ToString(),
                    template.Id,
                    documentType,
                    cancellationToken);
                break;

            case DocumentEngineType.CarboneDocx:
                // Carbone with DOCX/ODT template file
                if (!template.TemplateFileId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"CarboneDocx engine requires TemplateFileId to be set on template '{template.Name}' (ID: {template.Id}). " +
                        "Please upload a DOCX/ODT template file and set the TemplateFileId.");
                }
                _logger.LogDebug("Using Carbone DOCX engine for {DocumentType}, TemplateFileId={TemplateFileId}",
                    documentType, template.TemplateFileId);
                pdfBytes = await ExecutePdfGenerationWithRetryAsync(
                    () => _carboneRenderer.RenderFromFileAsync(
                        template.TemplateFileId.Value,
                        data,
                        documentType,
                        cancellationToken),
                    engineType.ToString(),
                    template.Id,
                    documentType,
                    cancellationToken);
                break;

            case DocumentEngineType.Handlebars:
            default:
                // Default: Handlebars + QuestPDF (existing flow)
                _logger.LogDebug("Using Handlebars + QuestPDF engine for {DocumentType}", documentType);
                var compiledTemplate = _handlebars.Compile(template.HtmlBody);
                pdfBytes = await ExecutePdfGenerationWithRetryAsync(
                    () => Task.FromResult(GeneratePdfFromHtml(compiledTemplate(data))),
                    engineType.ToString(),
                    template.Id,
                    documentType,
                    cancellationToken);
                break;
        }

        // Save to file storage (common for all engines)
        var fileDto = await SavePdfToStorageAsync(
            pdfBytes, fileName, companyId, Guid.Empty,
            documentType, referenceId, cancellationToken);

        // Create audit record (common for all engines)
        var generatedDocument = new GeneratedDocument
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DocumentType = documentType,
            ReferenceEntity = referenceEntity,
            ReferenceId = referenceId,
            TemplateId = template.Id,
            FileId = fileDto.Id,
            Format = "Pdf",
            GeneratedAt = DateTime.UtcNow,
            GeneratedByUserId = null,
            MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GeneratedDocuments.Add(generatedDocument);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Document generated: {DocumentId} using template {TemplateId} with engine {Engine}",
            generatedDocument.Id, template.Id, engineType);

        return MapToDto(generatedDocument);
    }

    /// <summary>
    /// Convert rendered HTML to PDF using QuestPDF
    /// 
    /// QuestPDF doesn't natively support HTML, so we:
    /// 1. Parse basic HTML structure
    /// 2. Render as QuestPDF elements
    /// 
    /// For complex HTML, consider adding QuestPDF.Html or PuppeteerSharp package.
    /// </summary>
    private byte[] GeneratePdfFromHtml(string html)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(column =>
                {
                    // Simple HTML to QuestPDF conversion
                    // This handles basic HTML - for complex layouts, use QuestPDF.Html package
                    RenderHtmlContent(column, html);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Generated by CephasOps | ");
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Basic HTML to QuestPDF renderer
    /// Handles: h1-h6, p, strong, em, br, hr, table, tr, td, th, ul, ol, li, div
    /// </summary>
    private void RenderHtmlContent(ColumnDescriptor column, string html)
    {
        // Strip HTML tags and render as plain text with basic formatting
        // For production, consider using a proper HTML parser or QuestPDF.Html
        
        // Remove script and style tags
        html = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Handle headings
        html = Regex.Replace(html, @"<h1[^>]*>(.*?)</h1>", "\n###H1###$1###/H1###\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<h2[^>]*>(.*?)</h2>", "\n###H2###$1###/H2###\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<h3[^>]*>(.*?)</h3>", "\n###H3###$1###/H3###\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Handle paragraphs and line breaks
        html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<p[^>]*>(.*?)</p>", "\n$1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<div[^>]*>(.*?)</div>", "\n$1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Handle horizontal rule
        html = Regex.Replace(html, @"<hr\s*/?>", "\n---\n", RegexOptions.IgnoreCase);
        
        // Handle lists
        html = Regex.Replace(html, @"<li[^>]*>(.*?)</li>", "• $1\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[uo]l[^>]*>|</[uo]l>", "\n", RegexOptions.IgnoreCase);
        
        // Handle tables (basic)
        html = Regex.Replace(html, @"<table[^>]*>", "\n###TABLE###\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</table>", "\n###/TABLE###\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<tr[^>]*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</tr>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<t[hd][^>]*>(.*?)</t[hd]>", "$1\t", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Remove remaining HTML tags
        html = Regex.Replace(html, @"<[^>]+>", "");
        
        // Decode HTML entities
        html = System.Net.WebUtility.HtmlDecode(html);
        
        // Split into lines and render
        var lines = html.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            
            if (trimmedLine.StartsWith("###H1###"))
            {
                var text = trimmedLine.Replace("###H1###", "").Replace("###/H1###", "");
                column.Item().PaddingTop(10).Text(text).FontSize(18).Bold();
            }
            else if (trimmedLine.StartsWith("###H2###"))
            {
                var text = trimmedLine.Replace("###H2###", "").Replace("###/H2###", "");
                column.Item().PaddingTop(8).Text(text).FontSize(14).Bold();
            }
            else if (trimmedLine.StartsWith("###H3###"))
            {
                var text = trimmedLine.Replace("###H3###", "").Replace("###/H3###", "");
                column.Item().PaddingTop(6).Text(text).FontSize(12).Bold();
            }
            else if (trimmedLine == "---")
            {
                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            }
            else if (trimmedLine.StartsWith("###TABLE###"))
            {
                // Table marker - skip
            }
            else if (trimmedLine.StartsWith("###/TABLE###"))
            {
                // End table marker - skip
            }
            else if (trimmedLine.Contains('\t'))
            {
                // Table row
                var cells = trimmedLine.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                column.Item().Row(row =>
                {
                    foreach (var cell in cells)
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cell.Trim()).FontSize(9);
                    }
                });
            }
            else
            {
                column.Item().Text(trimmedLine);
            }
        }
    }

    #endregion

    #region Query Methods

    public async Task<List<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(Guid companyId, string? referenceEntity = null, Guid? referenceId = null, string? documentType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.GeneratedDocuments.Where(d => d.CompanyId == companyId);

        if (!string.IsNullOrEmpty(referenceEntity))
            query = query.Where(d => d.ReferenceEntity == referenceEntity);

        if (referenceId.HasValue)
            query = query.Where(d => d.ReferenceId == referenceId.Value);

        if (!string.IsNullOrEmpty(documentType))
            query = query.Where(d => d.DocumentType == documentType);

        var documents = await query.OrderByDescending(d => d.GeneratedAt).ToListAsync(cancellationToken);
        return documents.Select(MapToDto).ToList();
    }

    public async Task<GeneratedDocumentDto?> GetGeneratedDocumentByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default)
    {
        var document = await _context.GeneratedDocuments
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == companyId, cancellationToken);
        return document == null ? null : MapToDto(document);
    }

    #endregion

    #region Helper Methods

    private async Task<FileDto> SavePdfToStorageAsync(
        byte[] pdfBytes, string fileName, Guid companyId, Guid userId,
        string module, Guid entityId, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(pdfBytes);
        var formFile = new FormFile(stream, 0, pdfBytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var uploadDto = new FileUploadDto
        {
            File = formFile,
            Module = module,
            EntityId = entityId,
            EntityType = module
        };

        return await _fileService.UploadFileAsync(uploadDto, companyId, userId, cancellationToken);
    }

    private GeneratedDocumentDto MapToDto(GeneratedDocument document)
    {
        return new GeneratedDocumentDto
        {
            Id = document.Id,
            CompanyId = document.CompanyId,
            DocumentType = document.DocumentType,
            ReferenceEntity = document.ReferenceEntity,
            ReferenceId = document.ReferenceId,
            TemplateId = document.TemplateId,
            FileId = document.FileId,
            Format = document.Format,
            GeneratedAt = document.GeneratedAt,
            GeneratedByUserId = document.GeneratedByUserId,
            MetadataJson = document.MetadataJson
        };
    }

    private void ValidateEntitySetExists<TEntity>(DbSet<TEntity> set, string entityName) where TEntity : class
    {
        if (_context.Model.FindEntityType(typeof(TEntity)) == null)
        {
            throw new InvalidOperationException(
                $"Entity type '{entityName}' is not configured in ApplicationDbContext. " +
                $"Document generation for {entityName} is not available.");
        }
    }

    private async Task<byte[]> ExecutePdfGenerationWithRetryAsync(
        Func<Task<byte[]>> generationFunc,
        string engineName,
        Guid templateId,
        string documentType,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await generationFunc();
            }
            catch (Exception ex) when (IsRetryablePdfGenerationError(ex))
            {
                attempt++;
                if (attempt > PdfGenerationMaxRetries)
                {
                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "PDF generation failed (attempt {Attempt}/{MaxAttempts}) for {DocumentType} using {Engine} template {TemplateId}",
                    attempt,
                    PdfGenerationMaxRetries + 1,
                    documentType,
                    engineName,
                    templateId);

                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static bool IsRetryablePdfGenerationError(Exception ex)
    {
        return ex is not (InvalidOperationException or ArgumentException or KeyNotFoundException or OperationCanceledException);
    }

    #endregion
}
