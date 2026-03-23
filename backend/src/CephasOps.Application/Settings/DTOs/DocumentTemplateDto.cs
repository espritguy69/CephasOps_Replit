namespace CephasOps.Application.Settings.DTOs;

/// <summary>
/// DTO for document template
/// </summary>
public class DocumentTemplateDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? PartnerId { get; set; }
    public bool IsActive { get; set; }
    public string Engine { get; set; } = "Handlebars";
    public string HtmlBody { get; set; } = string.Empty;
    public string? JsonSchema { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Template file ID (for CarboneDocx engine - reference to uploaded DOCX/ODT file)
    /// </summary>
    public Guid? TemplateFileId { get; set; }
    
    /// <summary>
    /// Template file name (populated from Files table when TemplateFileId is set)
    /// </summary>
    public string? TemplateFileName { get; set; }
}

/// <summary>
/// DTO for generated document
/// </summary>
public class GeneratedDocumentDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; } // Company feature removed - now nullable
    public string DocumentType { get; set; } = string.Empty;
    public string ReferenceEntity { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public Guid TemplateId { get; set; }
    public Guid FileId { get; set; }
    public string Format { get; set; } = "Pdf";
    public DateTime GeneratedAt { get; set; }
    public Guid? GeneratedByUserId { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>
/// DTO for document placeholder definition
/// </summary>
public class DocumentPlaceholderDefinitionDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExampleValue { get; set; }
    public bool IsRequired { get; set; }
}

/// <summary>
/// DTO for creating document template
/// </summary>
public class CreateDocumentTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public Guid? PartnerId { get; set; }
    public string Engine { get; set; } = "Handlebars";
    public string HtmlBody { get; set; } = string.Empty;
    public string? JsonSchema { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Template file ID (for CarboneDocx engine)
    /// </summary>
    public Guid? TemplateFileId { get; set; }
}

/// <summary>
/// DTO for updating document template
/// </summary>
public class UpdateDocumentTemplateDto
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public string? Engine { get; set; }
    public string? HtmlBody { get; set; }
    public string? JsonSchema { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// Template file ID (for CarboneDocx engine)
    /// </summary>
    public Guid? TemplateFileId { get; set; }
}

/// <summary>
/// DTO for generating document
/// </summary>
public class GenerateDocumentDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string ReferenceEntity { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public Guid? TemplateId { get; set; }
    public string Format { get; set; } = "Pdf";
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// DTO for test render requests
/// </summary>
public class TestRenderRequestDto
{
    public string TemplateContent { get; set; } = string.Empty;
    public string OutputType { get; set; } = "PDF";
    public Dictionary<string, object>? DataJson { get; set; }
}

/// <summary>
/// DTO for test render responses
/// </summary>
public class TestRenderResponseDto
{
    public string RenderedHtml { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}

