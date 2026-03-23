using CephasOps.Domain.Billing;
using System.Text;
using System.Xml.Linq;

namespace CephasOps.Application.Billing.Services;

/// <summary>
/// Builder for MyInvois invoice XML (v1.1 compliance)
/// Converts EInvoiceInvoiceDto to MyInvois XML format
/// </summary>
public class InvoiceXmlBuilder
{
    /// <summary>
    /// Build MyInvois invoice XML from DTO
    /// Follows MyInvois v1.1 schema
    /// </summary>
    public string BuildInvoiceXml(EInvoiceInvoiceDto invoice)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement("Invoice",
                // Header information
                new XElement("InvoiceNumber", invoice.InvoiceNumber),
                new XElement("InvoiceDate", invoice.InvoiceDate.ToString("yyyy-MM-dd")),
                new XElement("InvoiceType", "01"), // 01 = Tax Invoice
                new XElement("CurrencyCode", invoice.CurrencyCode),
                
                // Exchange rate (if applicable)
                invoice.ExchangeRate.HasValue 
                    ? new XElement("ExchangeRate", invoice.ExchangeRate.Value)
                    : null,

                // Supplier (Seller) information
                new XElement("Seller",
                    new XElement("Name", invoice.Supplier.Name),
                    invoice.Supplier.RegistrationNumber != null 
                        ? new XElement("RegistrationNumber", invoice.Supplier.RegistrationNumber)
                        : null,
                    invoice.Supplier.TaxId != null 
                        ? new XElement("TaxId", invoice.Supplier.TaxId)
                        : null,
                    BuildAddressElement("Address", invoice.Supplier)
                ),

                // Customer (Buyer) information
                new XElement("Buyer",
                    new XElement("Name", invoice.Customer.Name),
                    invoice.Customer.RegistrationNumber != null 
                        ? new XElement("RegistrationNumber", invoice.Customer.RegistrationNumber)
                        : null,
                    invoice.Customer.TaxId != null 
                        ? new XElement("TaxId", invoice.Customer.TaxId)
                        : null,
                    BuildAddressElement("Address", invoice.Customer)
                ),

                // Line items
                new XElement("InvoiceLines",
                    invoice.LineItems.Select((item, index) => 
                        new XElement("InvoiceLine",
                            new XElement("LineNumber", item.LineNumber),
                            new XElement("ItemCode", item.ItemCode ?? ""),
                            new XElement("ItemName", item.Description),
                            new XElement("Quantity", item.Quantity),
                            new XElement("UnitOfMeasure", item.UnitOfMeasure),
                            new XElement("UnitPrice", item.UnitPrice),
                            new XElement("LineTotal", item.LineTotal),
                            item.DiscountAmount > 0 
                                ? new XElement("DiscountAmount", item.DiscountAmount)
                                : null,
                            new XElement("Tax",
                                new XElement("TaxType", item.TaxCode ?? "SST"),
                                new XElement("TaxRate", item.TaxRate),
                                new XElement("TaxAmount", item.TaxAmount)
                            )
                        )
                    )
                ),

                // Summary
                new XElement("Summary",
                    new XElement("SubTotal", invoice.SubTotal),
                    new XElement("TaxAmount", invoice.TaxAmount),
                    new XElement("TotalAmount", invoice.TotalAmount)
                ),

                // Due date (if provided)
                invoice.DueDate.HasValue 
                    ? new XElement("DueDate", invoice.DueDate.Value.ToString("yyyy-MM-dd"))
                    : null,

                // Notes (if provided)
                !string.IsNullOrEmpty(invoice.Notes)
                    ? new XElement("Notes", invoice.Notes)
                    : null
            )
        );

        // Remove null elements
        RemoveNullElements(xml.Root!);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        xml.Save(writer);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Build MyInvois credit note XML from DTO
    /// </summary>
    public string BuildCreditNoteXml(EInvoiceCreditNoteDto creditNote)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement("CreditNote",
                new XElement("CreditNoteNumber", creditNote.CreditNoteNumber),
                new XElement("CreditNoteDate", creditNote.CreditNoteDate.ToString("yyyy-MM-dd")),
                creditNote.OriginalInvoiceNumber != null
                    ? new XElement("OriginalInvoiceNumber", creditNote.OriginalInvoiceNumber)
                    : null,
                new XElement("CurrencyCode", creditNote.CurrencyCode),
                creditNote.ExchangeRate.HasValue 
                    ? new XElement("ExchangeRate", creditNote.ExchangeRate.Value)
                    : null,

                new XElement("Seller",
                    new XElement("Name", creditNote.Supplier.Name),
                    creditNote.Supplier.RegistrationNumber != null 
                        ? new XElement("RegistrationNumber", creditNote.Supplier.RegistrationNumber)
                        : null,
                    creditNote.Supplier.TaxId != null 
                        ? new XElement("TaxId", creditNote.Supplier.TaxId)
                        : null,
                    BuildAddressElement("Address", creditNote.Supplier)
                ),

                new XElement("Buyer",
                    new XElement("Name", creditNote.Customer.Name),
                    creditNote.Customer.RegistrationNumber != null 
                        ? new XElement("RegistrationNumber", creditNote.Customer.RegistrationNumber)
                        : null,
                    creditNote.Customer.TaxId != null 
                        ? new XElement("TaxId", creditNote.Customer.TaxId)
                        : null,
                    BuildAddressElement("Address", creditNote.Customer)
                ),

                new XElement("CreditNoteLines",
                    creditNote.LineItems.Select((item, index) => 
                        new XElement("CreditNoteLine",
                            new XElement("LineNumber", item.LineNumber),
                            new XElement("ItemCode", item.ItemCode ?? ""),
                            new XElement("ItemName", item.Description),
                            new XElement("Quantity", item.Quantity),
                            new XElement("UnitOfMeasure", item.UnitOfMeasure),
                            new XElement("UnitPrice", item.UnitPrice),
                            new XElement("LineTotal", item.LineTotal),
                            item.DiscountAmount > 0 
                                ? new XElement("DiscountAmount", item.DiscountAmount)
                                : null,
                            new XElement("Tax",
                                new XElement("TaxType", item.TaxCode ?? "SST"),
                                new XElement("TaxRate", item.TaxRate),
                                new XElement("TaxAmount", item.TaxAmount)
                            )
                        )
                    )
                ),

                new XElement("Summary",
                    new XElement("SubTotal", creditNote.SubTotal),
                    new XElement("TaxAmount", creditNote.TaxAmount),
                    new XElement("TotalAmount", creditNote.TotalAmount)
                ),

                new XElement("Reason", creditNote.Reason)
            )
        );

        RemoveNullElements(xml.Root!);

        var stringBuilder = new StringBuilder();
        using var writer = new StringWriter(stringBuilder);
        xml.Save(writer);
        return stringBuilder.ToString();
    }

    private XElement BuildAddressElement(string elementName, EInvoicePartyDto party)
    {
        return new XElement(elementName,
            !string.IsNullOrEmpty(party.AddressLine1) 
                ? new XElement("AddressLine1", party.AddressLine1)
                : null,
            !string.IsNullOrEmpty(party.AddressLine2) 
                ? new XElement("AddressLine2", party.AddressLine2)
                : null,
            !string.IsNullOrEmpty(party.City) 
                ? new XElement("City", party.City)
                : null,
            !string.IsNullOrEmpty(party.State) 
                ? new XElement("State", party.State)
                : null,
            !string.IsNullOrEmpty(party.Postcode) 
                ? new XElement("Postcode", party.Postcode)
                : null,
            new XElement("CountryCode", party.CountryCode)
        );
    }

    private void RemoveNullElements(XElement element)
    {
        element.Elements()
            .Where(e => e.IsEmpty && string.IsNullOrEmpty(e.Value))
            .ToList()
            .ForEach(e => e.Remove());

        foreach (var child in element.Elements().ToList())
        {
            RemoveNullElements(child);
        }
    }
}

