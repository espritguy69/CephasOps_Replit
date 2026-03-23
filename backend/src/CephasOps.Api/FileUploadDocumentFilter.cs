using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace CephasOps.Api;

/// <summary>
/// Document filter to manually add file upload endpoints that were excluded from auto-generation
/// This runs after document generation to add endpoints with proper file upload schema
/// </summary>
public class FileUploadDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Manually add ExcelToPdfController.ConvertToPdf endpoint
        if (!swaggerDoc.Paths.ContainsKey("/api/excel-to-pdf/convert"))
        {
            swaggerDoc.Paths.Add("/api/excel-to-pdf/convert", new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Post] = new OpenApiOperation
                    {
                        Summary = "Convert Excel file to PDF",
                        Description = "Automatically uses Syncfusion (primary) or ExcelDataReader+QuestPDF (fallback) for corrupted files",
                        OperationId = "ExcelToPdf_ConvertToPdf",
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = "ExcelToPdf" } },
                        RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["multipart/form-data"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema>
                                        {
                                            ["file"] = new OpenApiSchema
                                            {
                                                Type = "string",
                                                Format = "binary",
                                                Description = "Excel file to convert (.xls, .xlsx, .xlsm)"
                                            }
                                        },
                                        Required = new HashSet<string> { "file" }
                                    }
                                }
                            }
                        },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Description = "PDF file",
                                Content = new Dictionary<string, OpenApiMediaType>
                                {
                                    ["application/pdf"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "string",
                                            Format = "binary"
                                        }
                                    }
                                }
                            },
                            ["400"] = new OpenApiResponse { Description = "Bad Request" },
                            ["500"] = new OpenApiResponse { Description = "Internal Server Error" }
                        }
                    }
                }
            });
        }

        // Manually add ParserController.UploadFilesForParsing endpoint
        if (!swaggerDoc.Paths.ContainsKey("/api/parser/upload"))
        {
            swaggerDoc.Paths.Add("/api/parser/upload", new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    [OperationType.Post] = new OpenApiOperation
                    {
                        Summary = "Upload files for order parsing",
                        Description = "Upload files for order parsing (PDF, Excel, Outlook MSG)",
                        OperationId = "Parser_UploadFilesForParsing",
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Parser" } },
                        RequestBody = new OpenApiRequestBody
                        {
                            Required = true,
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["multipart/form-data"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchema>
                                        {
                                            ["files"] = new OpenApiSchema
                                            {
                                                Type = "array",
                                                Items = new OpenApiSchema
                                                {
                                                    Type = "string",
                                                    Format = "binary"
                                                },
                                                Description = "Files to parse (PDF, Excel, Outlook MSG)"
                                            }
                                        },
                                        Required = new HashSet<string> { "files" }
                                    }
                                }
                            }
                        },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Description = "Parse session with extracted order drafts",
                                Content = new Dictionary<string, OpenApiMediaType>
                                {
                                    ["application/json"] = new OpenApiMediaType
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = "object"
                                        }
                                    }
                                }
                            },
                            ["400"] = new OpenApiResponse { Description = "Bad Request" },
                            ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                            ["500"] = new OpenApiResponse { Description = "Internal Server Error" }
                        }
                    }
                }
            });
        }
    }
}
