using CephasOps.Domain.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds document placeholder definitions for all document types
/// </summary>
public class DocumentPlaceholderSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentPlaceholderSeeder> _logger;

    public DocumentPlaceholderSeeder(ApplicationDbContext context, ILogger<DocumentPlaceholderSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding document placeholder definitions...");

        try
        {
            // Get all existing placeholders with AsNoTracking for better performance
            var existingPlaceholders = await _context.DocumentPlaceholderDefinitions
                .AsNoTracking()
                .Select(p => new { p.DocumentType, p.Key })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} existing document placeholders", existingPlaceholders.Count);

            var existingKeys = new HashSet<string>(
                existingPlaceholders.Select(p => $"{p.DocumentType}|{p.Key}"),
                StringComparer.Ordinal
            );

            var placeholders = new List<DocumentPlaceholderDefinition>();

            // Invoice placeholders
            placeholders.AddRange(GetInvoicePlaceholders());

            // Job Docket placeholders
            placeholders.AddRange(GetJobDocketPlaceholders());

            // RMA Form placeholders
            placeholders.AddRange(GetRmaFormPlaceholders());

            // Purchase Order placeholders
            placeholders.AddRange(GetPurchaseOrderPlaceholders());

            // Quotation placeholders
            placeholders.AddRange(GetQuotationPlaceholders());

            // BOQ placeholders
            placeholders.AddRange(GetBoqPlaceholders());

            // Delivery Order placeholders
            placeholders.AddRange(GetDeliveryOrderPlaceholders());

            // Payment Receipt placeholders
            placeholders.AddRange(GetPaymentReceiptPlaceholders());

            _logger.LogInformation("Generated {Count} placeholder definitions from seed data", placeholders.Count);

            // Filter out existing placeholders
            var newPlaceholders = placeholders
                .Where(p => !existingKeys.Contains($"{p.DocumentType}|{p.Key}"))
                .ToList();

            if (newPlaceholders.Count == 0)
            {
                _logger.LogInformation("All document placeholders already seeded ({Count} total records). Skipping.", existingPlaceholders.Count);
                return;
            }

            _logger.LogInformation("Adding {NewCount} new placeholder definitions...", newPlaceholders.Count);

            // Insert in smaller batches to avoid constraint violations in bulk inserts
            const int batchSize = 50;
            var inserted = 0;
            var skipped = 0;

            for (int i = 0; i < newPlaceholders.Count; i += batchSize)
            {
                var batch = newPlaceholders.Skip(i).Take(batchSize).ToList();
                
                try
                {
                    await _context.DocumentPlaceholderDefinitions.AddRangeAsync(batch, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    inserted += batch.Count;
                }
                catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    // Duplicate key error - some records in this batch already exist
                    // Clear the tracked entities and try inserting one by one
                    _context.ChangeTracker.Clear();
                    
                    foreach (var placeholder in batch)
                    {
                        try
                        {
                            _context.DocumentPlaceholderDefinitions.Add(placeholder);
                            await _context.SaveChangesAsync(cancellationToken);
                            inserted++;
                        }
                        catch (DbUpdateException)
                        {
                            // This specific record already exists, skip it
                            _context.ChangeTracker.Clear();
                            skipped++;
                        }
                    }
                }
            }

            _logger.LogInformation("Document placeholder seeding complete: {Inserted} inserted, {Skipped} skipped (Total: {Total})", 
                inserted, skipped, existingPlaceholders.Count + inserted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding document placeholders. This is not critical - the system will continue.");
        }
    }

    private static List<DocumentPlaceholderDefinition> GetInvoicePlaceholders() => new()
    {
        // Invoice fields
        new() { DocumentType = "Invoice", Key = "{{invoice.id}}", Description = "Invoice unique identifier", ExampleValue = "550e8400-e29b-41d4-a716-446655440000" },
        new() { DocumentType = "Invoice", Key = "{{invoice.number}}", Description = "Invoice number", ExampleValue = "INV-2024-0001", IsRequired = true },
        new() { DocumentType = "Invoice", Key = "{{invoice.date}}", Description = "Invoice date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "Invoice", Key = "{{invoice.dueDate}}", Description = "Payment due date", ExampleValue = "2024-02-15" },
        new() { DocumentType = "Invoice", Key = "{{invoice.status}}", Description = "Invoice status", ExampleValue = "Pending" },
        new() { DocumentType = "Invoice", Key = "{{invoice.subTotal}}", Description = "Subtotal before tax", ExampleValue = "1000.00" },
        new() { DocumentType = "Invoice", Key = "{{invoice.taxAmount}}", Description = "Tax amount", ExampleValue = "60.00" },
        new() { DocumentType = "Invoice", Key = "{{invoice.totalAmount}}", Description = "Total amount", ExampleValue = "1060.00", IsRequired = true },
        new() { DocumentType = "Invoice", Key = "{{invoice.notes}}", Description = "Invoice notes", ExampleValue = "Payment within 30 days" },
        
        // Partner fields
        new() { DocumentType = "Invoice", Key = "{{partner.id}}", Description = "Partner unique identifier" },
        new() { DocumentType = "Invoice", Key = "{{partner.name}}", Description = "Partner name", ExampleValue = "TIME dotCom", IsRequired = true },
        new() { DocumentType = "Invoice", Key = "{{partner.code}}", Description = "Partner code", ExampleValue = "TIME" },
        
        // Line items (array)
        new() { DocumentType = "Invoice", Key = "{{#each lineItems}}", Description = "Start of line items loop", IsRequired = true },
        new() { DocumentType = "Invoice", Key = "{{lineItems.description}}", Description = "Line item description", ExampleValue = "GPON Installation" },
        new() { DocumentType = "Invoice", Key = "{{lineItems.quantity}}", Description = "Line item quantity", ExampleValue = "1" },
        new() { DocumentType = "Invoice", Key = "{{lineItems.unitPrice}}", Description = "Line item unit price", ExampleValue = "250.00" },
        new() { DocumentType = "Invoice", Key = "{{lineItems.total}}", Description = "Line item total", ExampleValue = "250.00" },
        new() { DocumentType = "Invoice", Key = "{{/each}}", Description = "End of line items loop" },
        
        // Helpers
        new() { DocumentType = "Invoice", Key = "{{currency invoice.totalAmount}}", Description = "Format as currency (RM X.XX)", ExampleValue = "RM 1,060.00" },
        new() { DocumentType = "Invoice", Key = "{{date invoice.date \"dd MMM yyyy\"}}", Description = "Format date", ExampleValue = "15 Jan 2024" },
        new() { DocumentType = "Invoice", Key = "{{generatedAt}}", Description = "Document generation timestamp", ExampleValue = "15 Jan 2024 10:30" }
    };

    private static List<DocumentPlaceholderDefinition> GetJobDocketPlaceholders() => new()
    {
        // Order fields
        new() { DocumentType = "JobDocket", Key = "{{order.id}}", Description = "Order unique identifier" },
        new() { DocumentType = "JobDocket", Key = "{{order.serviceId}}", Description = "Service ID / TBBN", ExampleValue = "TBBN12345678", IsRequired = true },
        new() { DocumentType = "JobDocket", Key = "{{order.ticketId}}", Description = "Ticket ID", ExampleValue = "TKT-2024-001" },
        new() { DocumentType = "JobDocket", Key = "{{order.status}}", Description = "Order status", ExampleValue = "Assigned" },
        new() { DocumentType = "JobDocket", Key = "{{order.priority}}", Description = "Order priority", ExampleValue = "Normal" },
        new() { DocumentType = "JobDocket", Key = "{{order.appointmentDate}}", Description = "Appointment date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "JobDocket", Key = "{{order.appointmentWindowFrom}}", Description = "Appointment window start", ExampleValue = "09:00" },
        new() { DocumentType = "JobDocket", Key = "{{order.appointmentWindowTo}}", Description = "Appointment window end", ExampleValue = "12:00" },
        new() { DocumentType = "JobDocket", Key = "{{order.partnerNotes}}", Description = "Notes from partner", ExampleValue = "Customer prefers morning" },
        new() { DocumentType = "JobDocket", Key = "{{order.orderNotesInternal}}", Description = "Internal notes" },
        
        // Customer fields
        new() { DocumentType = "JobDocket", Key = "{{customer.name}}", Description = "Customer name", ExampleValue = "Ahmad bin Ali", IsRequired = true },
        new() { DocumentType = "JobDocket", Key = "{{customer.phone}}", Description = "Customer phone", ExampleValue = "0123456789", IsRequired = true },
        new() { DocumentType = "JobDocket", Key = "{{customer.email}}", Description = "Customer email", ExampleValue = "ahmad@email.com" },
        
        // Location fields
        new() { DocumentType = "JobDocket", Key = "{{location.buildingName}}", Description = "Building name", ExampleValue = "Menara ABC" },
        new() { DocumentType = "JobDocket", Key = "{{location.unitNo}}", Description = "Unit number", ExampleValue = "A-12-03" },
        new() { DocumentType = "JobDocket", Key = "{{location.fullAddress}}", Description = "Full address", ExampleValue = "A-12-03, Menara ABC, Jalan 1/2, 50000 KL" },
        
        // Installer fields
        new() { DocumentType = "JobDocket", Key = "{{installer.name}}", Description = "Installer name", ExampleValue = "John Doe" },
        new() { DocumentType = "JobDocket", Key = "{{installer.phone}}", Description = "Installer phone", ExampleValue = "0198765432" },
        
        // Partner fields
        new() { DocumentType = "JobDocket", Key = "{{partner.name}}", Description = "Partner name", ExampleValue = "TIME dotCom" },
        
        // Order type
        new() { DocumentType = "JobDocket", Key = "{{orderType.name}}", Description = "Order type name", ExampleValue = "New Activation" },
        
        // Helpers
        new() { DocumentType = "JobDocket", Key = "{{time order.appointmentWindowFrom}}", Description = "Format time (HH:mm)", ExampleValue = "09:00" },
        new() { DocumentType = "JobDocket", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };

    private static List<DocumentPlaceholderDefinition> GetRmaFormPlaceholders() => new()
    {
        // RMA fields
        new() { DocumentType = "RmaForm", Key = "{{rma.id}}", Description = "RMA request unique identifier" },
        new() { DocumentType = "RmaForm", Key = "{{rma.number}}", Description = "RMA number", ExampleValue = "RMA-2024-0001", IsRequired = true },
        new() { DocumentType = "RmaForm", Key = "{{rma.requestDate}}", Description = "RMA request date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "RmaForm", Key = "{{rma.reason}}", Description = "RMA reason", ExampleValue = "Defective unit" },
        new() { DocumentType = "RmaForm", Key = "{{rma.status}}", Description = "RMA status", ExampleValue = "Pending" },
        
        // Partner fields
        new() { DocumentType = "RmaForm", Key = "{{partner.id}}", Description = "Partner unique identifier" },
        new() { DocumentType = "RmaForm", Key = "{{partner.name}}", Description = "Partner name", ExampleValue = "TIME dotCom", IsRequired = true },
        
        // Items (array)
        new() { DocumentType = "RmaForm", Key = "{{#each items}}", Description = "Start of items loop", IsRequired = true },
        new() { DocumentType = "RmaForm", Key = "{{items.index}}", Description = "Item index (0-based)", ExampleValue = "0" },
        new() { DocumentType = "RmaForm", Key = "{{items.serialisedItemId}}", Description = "Serialised item ID" },
        new() { DocumentType = "RmaForm", Key = "{{items.notes}}", Description = "Item notes", ExampleValue = "Power LED not working" },
        new() { DocumentType = "RmaForm", Key = "{{items.result}}", Description = "RMA result", ExampleValue = "Pending" },
        new() { DocumentType = "RmaForm", Key = "{{/each}}", Description = "End of items loop" },
        
        // Helpers
        new() { DocumentType = "RmaForm", Key = "{{rowIndex @index}}", Description = "Row number (1-based)", ExampleValue = "1" },
        new() { DocumentType = "RmaForm", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };

    private static List<DocumentPlaceholderDefinition> GetPurchaseOrderPlaceholders() => new()
    {
        // PO fields
        new() { DocumentType = "PurchaseOrder", Key = "{{po.id}}", Description = "Purchase order unique identifier" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.number}}", Description = "PO number", ExampleValue = "PO-2024-0001", IsRequired = true },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.date}}", Description = "PO date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.expectedDeliveryDate}}", Description = "Expected delivery date", ExampleValue = "2024-01-30" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.status}}", Description = "PO status", ExampleValue = "Approved" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.deliveryAddress}}", Description = "Delivery address" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.subTotal}}", Description = "Subtotal before tax", ExampleValue = "5000.00" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.taxAmount}}", Description = "Tax amount", ExampleValue = "300.00" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.discountAmount}}", Description = "Discount amount", ExampleValue = "0.00" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.totalAmount}}", Description = "Total amount", ExampleValue = "5300.00", IsRequired = true },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.currency}}", Description = "Currency", ExampleValue = "MYR" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.paymentTerms}}", Description = "Payment terms", ExampleValue = "Net 30" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.termsAndConditions}}", Description = "Terms and conditions" },
        new() { DocumentType = "PurchaseOrder", Key = "{{po.notes}}", Description = "Notes" },
        
        // Supplier fields
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.id}}", Description = "Supplier unique identifier" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.name}}", Description = "Supplier name", ExampleValue = "ABC Supplies Sdn Bhd", IsRequired = true },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.code}}", Description = "Supplier code", ExampleValue = "ABC" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.contactPerson}}", Description = "Contact person", ExampleValue = "Mr. Lee" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.phone}}", Description = "Supplier phone", ExampleValue = "03-12345678" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.email}}", Description = "Supplier email", ExampleValue = "sales@abc.com" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.address}}", Description = "Supplier address" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.city}}", Description = "Supplier city", ExampleValue = "Kuala Lumpur" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.state}}", Description = "Supplier state", ExampleValue = "Wilayah Persekutuan" },
        new() { DocumentType = "PurchaseOrder", Key = "{{supplier.postcode}}", Description = "Supplier postcode", ExampleValue = "50000" },
        
        // Items (array)
        new() { DocumentType = "PurchaseOrder", Key = "{{#each items}}", Description = "Start of items loop", IsRequired = true },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.index}}", Description = "Item index (0-based)" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.lineNumber}}", Description = "Line number", ExampleValue = "1" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.description}}", Description = "Item description", ExampleValue = "ONT Router ZTE F660" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.sku}}", Description = "SKU / Part number", ExampleValue = "ZTE-F660" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.unit}}", Description = "Unit of measure", ExampleValue = "pcs" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.quantity}}", Description = "Quantity", ExampleValue = "100" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.unitPrice}}", Description = "Unit price", ExampleValue = "50.00" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.discountPercent}}", Description = "Discount %", ExampleValue = "0" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.taxPercent}}", Description = "Tax %", ExampleValue = "6" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.total}}", Description = "Line total", ExampleValue = "5000.00" },
        new() { DocumentType = "PurchaseOrder", Key = "{{items.notes}}", Description = "Item notes" },
        new() { DocumentType = "PurchaseOrder", Key = "{{/each}}", Description = "End of items loop" },
        
        // Helpers
        new() { DocumentType = "PurchaseOrder", Key = "{{currency po.totalAmount}}", Description = "Format as currency", ExampleValue = "RM 5,300.00" },
        new() { DocumentType = "PurchaseOrder", Key = "{{rowIndex @index}}", Description = "Row number (1-based)" },
        new() { DocumentType = "PurchaseOrder", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };

    private static List<DocumentPlaceholderDefinition> GetQuotationPlaceholders() => new()
    {
        // Quotation fields
        new() { DocumentType = "Quotation", Key = "{{quotation.id}}", Description = "Quotation unique identifier" },
        new() { DocumentType = "Quotation", Key = "{{quotation.number}}", Description = "Quotation number", ExampleValue = "QT-2024-0001", IsRequired = true },
        new() { DocumentType = "Quotation", Key = "{{quotation.date}}", Description = "Quotation date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "Quotation", Key = "{{quotation.validUntil}}", Description = "Valid until date", ExampleValue = "2024-02-15" },
        new() { DocumentType = "Quotation", Key = "{{quotation.status}}", Description = "Quotation status", ExampleValue = "Sent" },
        new() { DocumentType = "Quotation", Key = "{{quotation.subject}}", Description = "Subject / title", ExampleValue = "Solar Panel Installation" },
        new() { DocumentType = "Quotation", Key = "{{quotation.introduction}}", Description = "Introduction text" },
        new() { DocumentType = "Quotation", Key = "{{quotation.subTotal}}", Description = "Subtotal before tax", ExampleValue = "10000.00" },
        new() { DocumentType = "Quotation", Key = "{{quotation.taxAmount}}", Description = "Tax amount", ExampleValue = "600.00" },
        new() { DocumentType = "Quotation", Key = "{{quotation.discountAmount}}", Description = "Discount amount", ExampleValue = "500.00" },
        new() { DocumentType = "Quotation", Key = "{{quotation.totalAmount}}", Description = "Total amount", ExampleValue = "10100.00", IsRequired = true },
        new() { DocumentType = "Quotation", Key = "{{quotation.currency}}", Description = "Currency", ExampleValue = "MYR" },
        new() { DocumentType = "Quotation", Key = "{{quotation.paymentTerms}}", Description = "Payment terms", ExampleValue = "50% upfront, 50% on completion" },
        new() { DocumentType = "Quotation", Key = "{{quotation.deliveryTerms}}", Description = "Delivery terms", ExampleValue = "2 weeks from order confirmation" },
        new() { DocumentType = "Quotation", Key = "{{quotation.termsAndConditions}}", Description = "Terms and conditions" },
        new() { DocumentType = "Quotation", Key = "{{quotation.notes}}", Description = "Notes" },
        
        // Customer fields
        new() { DocumentType = "Quotation", Key = "{{customer.name}}", Description = "Customer name", ExampleValue = "Ahmad bin Ali", IsRequired = true },
        new() { DocumentType = "Quotation", Key = "{{customer.phone}}", Description = "Customer phone", ExampleValue = "0123456789" },
        new() { DocumentType = "Quotation", Key = "{{customer.email}}", Description = "Customer email", ExampleValue = "ahmad@email.com" },
        new() { DocumentType = "Quotation", Key = "{{customer.address}}", Description = "Customer address" },
        
        // Partner fields (if applicable)
        new() { DocumentType = "Quotation", Key = "{{partner.id}}", Description = "Partner unique identifier" },
        new() { DocumentType = "Quotation", Key = "{{partner.name}}", Description = "Partner name" },
        
        // Items (array)
        new() { DocumentType = "Quotation", Key = "{{#each items}}", Description = "Start of items loop", IsRequired = true },
        new() { DocumentType = "Quotation", Key = "{{items.index}}", Description = "Item index (0-based)" },
        new() { DocumentType = "Quotation", Key = "{{items.lineNumber}}", Description = "Line number", ExampleValue = "1" },
        new() { DocumentType = "Quotation", Key = "{{items.itemType}}", Description = "Item type", ExampleValue = "Material" },
        new() { DocumentType = "Quotation", Key = "{{items.description}}", Description = "Item description", ExampleValue = "Solar Panel 400W" },
        new() { DocumentType = "Quotation", Key = "{{items.sku}}", Description = "SKU / Part number", ExampleValue = "SP-400W" },
        new() { DocumentType = "Quotation", Key = "{{items.unit}}", Description = "Unit of measure", ExampleValue = "pcs" },
        new() { DocumentType = "Quotation", Key = "{{items.quantity}}", Description = "Quantity", ExampleValue = "10" },
        new() { DocumentType = "Quotation", Key = "{{items.unitPrice}}", Description = "Unit price", ExampleValue = "800.00" },
        new() { DocumentType = "Quotation", Key = "{{items.discountPercent}}", Description = "Discount %", ExampleValue = "5" },
        new() { DocumentType = "Quotation", Key = "{{items.taxPercent}}", Description = "Tax %", ExampleValue = "6" },
        new() { DocumentType = "Quotation", Key = "{{items.total}}", Description = "Line total", ExampleValue = "8000.00" },
        new() { DocumentType = "Quotation", Key = "{{items.notes}}", Description = "Item notes" },
        new() { DocumentType = "Quotation", Key = "{{/each}}", Description = "End of items loop" },
        
        // Helpers
        new() { DocumentType = "Quotation", Key = "{{currency quotation.totalAmount}}", Description = "Format as currency", ExampleValue = "RM 10,100.00" },
        new() { DocumentType = "Quotation", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };

    private static List<DocumentPlaceholderDefinition> GetBoqPlaceholders() => new()
    {
        // Project fields
        new() { DocumentType = "BOQ", Key = "{{project.id}}", Description = "Project unique identifier" },
        new() { DocumentType = "BOQ", Key = "{{project.code}}", Description = "Project code", ExampleValue = "PRJ-2024-001", IsRequired = true },
        new() { DocumentType = "BOQ", Key = "{{project.name}}", Description = "Project name", ExampleValue = "Solar Installation - Menara ABC", IsRequired = true },
        new() { DocumentType = "BOQ", Key = "{{project.description}}", Description = "Project description" },
        new() { DocumentType = "BOQ", Key = "{{project.projectType}}", Description = "Project type", ExampleValue = "Solar" },
        new() { DocumentType = "BOQ", Key = "{{project.status}}", Description = "Project status", ExampleValue = "Planning" },
        new() { DocumentType = "BOQ", Key = "{{project.customerName}}", Description = "Customer name", ExampleValue = "ABC Corporation" },
        new() { DocumentType = "BOQ", Key = "{{project.customerPhone}}", Description = "Customer phone" },
        new() { DocumentType = "BOQ", Key = "{{project.customerEmail}}", Description = "Customer email" },
        new() { DocumentType = "BOQ", Key = "{{project.siteAddress}}", Description = "Site address" },
        new() { DocumentType = "BOQ", Key = "{{project.city}}", Description = "City", ExampleValue = "Kuala Lumpur" },
        new() { DocumentType = "BOQ", Key = "{{project.state}}", Description = "State" },
        new() { DocumentType = "BOQ", Key = "{{project.postcode}}", Description = "Postcode" },
        new() { DocumentType = "BOQ", Key = "{{project.startDate}}", Description = "Start date" },
        new() { DocumentType = "BOQ", Key = "{{project.endDate}}", Description = "End date" },
        new() { DocumentType = "BOQ", Key = "{{project.budgetAmount}}", Description = "Budget amount" },
        new() { DocumentType = "BOQ", Key = "{{project.contractValue}}", Description = "Contract value" },
        new() { DocumentType = "BOQ", Key = "{{project.currency}}", Description = "Currency", ExampleValue = "MYR" },
        
        // Partner fields
        new() { DocumentType = "BOQ", Key = "{{partner.id}}", Description = "Partner unique identifier" },
        new() { DocumentType = "BOQ", Key = "{{partner.name}}", Description = "Partner name" },
        
        // Materials (array)
        new() { DocumentType = "BOQ", Key = "{{#each materials}}", Description = "Start of materials loop" },
        new() { DocumentType = "BOQ", Key = "{{materials.index}}", Description = "Item index (0-based)" },
        new() { DocumentType = "BOQ", Key = "{{materials.lineNumber}}", Description = "Line number" },
        new() { DocumentType = "BOQ", Key = "{{materials.section}}", Description = "Section", ExampleValue = "Solar Panels" },
        new() { DocumentType = "BOQ", Key = "{{materials.description}}", Description = "Description", ExampleValue = "400W Monocrystalline Panel" },
        new() { DocumentType = "BOQ", Key = "{{materials.sku}}", Description = "SKU" },
        new() { DocumentType = "BOQ", Key = "{{materials.unit}}", Description = "Unit", ExampleValue = "pcs" },
        new() { DocumentType = "BOQ", Key = "{{materials.quantity}}", Description = "Quantity", ExampleValue = "20" },
        new() { DocumentType = "BOQ", Key = "{{materials.unitRate}}", Description = "Unit rate", ExampleValue = "800.00" },
        new() { DocumentType = "BOQ", Key = "{{materials.total}}", Description = "Total", ExampleValue = "16000.00" },
        new() { DocumentType = "BOQ", Key = "{{materials.markupPercent}}", Description = "Markup %", ExampleValue = "15" },
        new() { DocumentType = "BOQ", Key = "{{materials.sellingPrice}}", Description = "Selling price", ExampleValue = "18400.00" },
        new() { DocumentType = "BOQ", Key = "{{materials.isOptional}}", Description = "Is optional", ExampleValue = "false" },
        new() { DocumentType = "BOQ", Key = "{{/each}}", Description = "End of materials loop" },
        
        // Labor (array)
        new() { DocumentType = "BOQ", Key = "{{#each labor}}", Description = "Start of labor loop" },
        new() { DocumentType = "BOQ", Key = "{{labor.description}}", Description = "Labor description", ExampleValue = "Installation labor" },
        new() { DocumentType = "BOQ", Key = "{{labor.unit}}", Description = "Unit", ExampleValue = "day" },
        new() { DocumentType = "BOQ", Key = "{{labor.quantity}}", Description = "Quantity", ExampleValue = "5" },
        new() { DocumentType = "BOQ", Key = "{{labor.unitRate}}", Description = "Unit rate", ExampleValue = "500.00" },
        new() { DocumentType = "BOQ", Key = "{{labor.total}}", Description = "Total", ExampleValue = "2500.00" },
        new() { DocumentType = "BOQ", Key = "{{/each}}", Description = "End of labor loop" },
        
        // Other items (array)
        new() { DocumentType = "BOQ", Key = "{{#each otherItems}}", Description = "Start of other items loop" },
        new() { DocumentType = "BOQ", Key = "{{otherItems.itemType}}", Description = "Item type", ExampleValue = "Equipment" },
        new() { DocumentType = "BOQ", Key = "{{otherItems.description}}", Description = "Description" },
        new() { DocumentType = "BOQ", Key = "{{otherItems.total}}", Description = "Total" },
        new() { DocumentType = "BOQ", Key = "{{/each}}", Description = "End of other items loop" },
        
        // Summary
        new() { DocumentType = "BOQ", Key = "{{summary.materialTotal}}", Description = "Total materials cost" },
        new() { DocumentType = "BOQ", Key = "{{summary.laborTotal}}", Description = "Total labor cost" },
        new() { DocumentType = "BOQ", Key = "{{summary.otherTotal}}", Description = "Total other costs" },
        new() { DocumentType = "BOQ", Key = "{{summary.grandTotal}}", Description = "Grand total (cost)" },
        new() { DocumentType = "BOQ", Key = "{{summary.sellingTotal}}", Description = "Total selling price" },
        
        // Helpers
        new() { DocumentType = "BOQ", Key = "{{currency summary.grandTotal}}", Description = "Format as currency" },
        new() { DocumentType = "BOQ", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };

    private static List<DocumentPlaceholderDefinition> GetDeliveryOrderPlaceholders() => new()
    {
        // DO fields
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.id}}", Description = "Delivery order unique identifier" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.number}}", Description = "DO number", ExampleValue = "DO-2024-0001", IsRequired = true },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.date}}", Description = "DO date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.type}}", Description = "DO type", ExampleValue = "Outbound" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.status}}", Description = "DO status", ExampleValue = "InTransit" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.expectedDeliveryDate}}", Description = "Expected delivery date" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.actualDeliveryDate}}", Description = "Actual delivery date" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.deliveryPerson}}", Description = "Delivery person", ExampleValue = "John Driver" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.vehicleNumber}}", Description = "Vehicle number", ExampleValue = "WXY 1234" },
        new() { DocumentType = "DeliveryOrder", Key = "{{deliveryOrder.notes}}", Description = "Notes" },
        
        // Recipient fields
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.name}}", Description = "Recipient name", ExampleValue = "Ahmad bin Ali", IsRequired = true },
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.phone}}", Description = "Recipient phone", ExampleValue = "0123456789" },
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.email}}", Description = "Recipient email" },
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.address}}", Description = "Delivery address", ExampleValue = "123 Jalan ABC", IsRequired = true },
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.city}}", Description = "City", ExampleValue = "Kuala Lumpur" },
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.state}}", Description = "State" },
        new() { DocumentType = "DeliveryOrder", Key = "{{recipient.postcode}}", Description = "Postcode", ExampleValue = "50000" },
        
        // Items (array)
        new() { DocumentType = "DeliveryOrder", Key = "{{#each items}}", Description = "Start of items loop", IsRequired = true },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.index}}", Description = "Item index (0-based)" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.lineNumber}}", Description = "Line number" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.description}}", Description = "Item description", ExampleValue = "ONT Router ZTE F660" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.sku}}", Description = "SKU", ExampleValue = "ZTE-F660" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.unit}}", Description = "Unit", ExampleValue = "pcs" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.quantity}}", Description = "Quantity to deliver", ExampleValue = "10" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.quantityDelivered}}", Description = "Quantity delivered", ExampleValue = "10" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.serialNumbers}}", Description = "Serial numbers", ExampleValue = "SN001, SN002, SN003" },
        new() { DocumentType = "DeliveryOrder", Key = "{{items.notes}}", Description = "Item notes" },
        new() { DocumentType = "DeliveryOrder", Key = "{{/each}}", Description = "End of items loop" },
        
        // Helpers
        new() { DocumentType = "DeliveryOrder", Key = "{{rowIndex @index}}", Description = "Row number (1-based)" },
        new() { DocumentType = "DeliveryOrder", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };

    private static List<DocumentPlaceholderDefinition> GetPaymentReceiptPlaceholders() => new()
    {
        // Payment fields
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.id}}", Description = "Payment unique identifier" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.number}}", Description = "Payment/receipt number", ExampleValue = "RCP-2024-0001", IsRequired = true },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.date}}", Description = "Payment date", ExampleValue = "2024-01-15", IsRequired = true },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.type}}", Description = "Payment type", ExampleValue = "Income" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.method}}", Description = "Payment method", ExampleValue = "BankTransfer" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.amount}}", Description = "Payment amount", ExampleValue = "1060.00", IsRequired = true },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.currency}}", Description = "Currency", ExampleValue = "MYR" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.bankAccount}}", Description = "Bank account used" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.bankReference}}", Description = "Bank reference", ExampleValue = "TRX123456" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.chequeNumber}}", Description = "Cheque number" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.description}}", Description = "Description", ExampleValue = "Payment for Invoice INV-2024-0001" },
        new() { DocumentType = "PaymentReceipt", Key = "{{payment.notes}}", Description = "Notes" },
        
        // Payer/Payee fields
        new() { DocumentType = "PaymentReceipt", Key = "{{payer.name}}", Description = "Payer/Payee name", ExampleValue = "TIME dotCom Berhad", IsRequired = true },
        
        // Helpers
        new() { DocumentType = "PaymentReceipt", Key = "{{currency payment.amount}}", Description = "Format as currency", ExampleValue = "RM 1,060.00" },
        new() { DocumentType = "PaymentReceipt", Key = "{{uppercase payment.method}}", Description = "Uppercase text", ExampleValue = "BANKTRANSFER" },
        new() { DocumentType = "PaymentReceipt", Key = "{{generatedAt}}", Description = "Document generation timestamp" }
    };
}

