using CephasOps.Domain.Common;

namespace CephasOps.Domain.Settings.Entities;

/// <summary>
/// Document template entity - reusable, versioned templates for document generation
/// </summary>
public class DocumentTemplate : CompanyScopedEntity
{
    /// <summary>
    /// Template name (e.g. "TIME Invoice", "Default Docket")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Document type (Invoice, JobDocket, RmaForm, PaymentReceipt, PnlSummary, GenericLetter)
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Partner ID (nullable - if template is partner-specific)
    /// </summary>
    public Guid? PartnerId { get; set; }

    /// <summary>
    /// Whether this is the active template for (CompanyId, DocumentType, PartnerId)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Template engine (Razor, Handlebars, Liquid)
    /// </summary>
    public string Engine { get; set; } = "Handlebars";

    /// <summary>
    /// HTML body with placeholders
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON schema describing expected data shape
    /// </summary>
    public string? JsonSchema { get; set; }

    /// <summary>
    /// Optional description for UI context
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional tags stored as comma-separated string
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Version number (incremented on updates)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// User ID who created this template
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User ID who last updated this template
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Template file ID (for CarboneDocx engine - reference to uploaded DOCX/ODT file)
    /// </summary>
    public Guid? TemplateFileId { get; set; }
}

/// <summary>
/// Generated document entity - represents a single rendered document instance
/// </summary>
public class GeneratedDocument : CompanyScopedEntity
{
    /// <summary>
    /// Document type
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Reference entity type (Order, Invoice, RmaTicket, PayrollRun, PnlFact, Generic)
    /// </summary>
    public string ReferenceEntity { get; set; } = string.Empty;

    /// <summary>
    /// Reference entity ID
    /// </summary>
    public Guid ReferenceId { get; set; }

    /// <summary>
    /// Template ID used to generate this document
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// File ID (FK to File entity)
    /// </summary>
    public Guid FileId { get; set; }

    /// <summary>
    /// Format (Pdf, Html)
    /// </summary>
    public string Format { get; set; } = "Pdf";

    /// <summary>
    /// Timestamp when document was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who generated this document (or System)
    /// </summary>
    public Guid? GeneratedByUserId { get; set; }

    /// <summary>
    /// Additional metadata as JSON (partnerName, invoiceNo, etc.)
    /// </summary>
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Document placeholder definition - catalogs available placeholder variables
/// </summary>
public class DocumentPlaceholderDefinition
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Document type these placeholders apply to
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Placeholder key (e.g. "{{customer.name}}", "{{order.serviceId}}")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this placeholder represents
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Example value for UI reference
    /// </summary>
    public string? ExampleValue { get; set; }

    /// <summary>
    /// Whether this placeholder is required in templates of this type
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Timestamp when created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

