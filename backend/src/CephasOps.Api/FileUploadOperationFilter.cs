using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Collections.Generic;

namespace CephasOps.Api;

/// <summary>
/// Operation filter to handle file uploads with [FromForm] and IFormFile
/// Converts IFormFile parameters to multipart/form-data request body
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check ALL parameters, not just those with [FromForm]
        // This ensures we catch IFormFile parameters even if Swagger hasn't processed [FromForm] yet
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => 
            {
                var hasFromForm = p.GetCustomAttributes(typeof(FromFormAttribute), false).Any();
                var isFormFile = p.ParameterType == typeof(IFormFile);
                var isFormFileList = p.ParameterType.IsGenericType && 
                                    p.ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                                    p.ParameterType.GetGenericArguments()[0] == typeof(IFormFile);
                // Also check if parameter name suggests it's a file (fallback)
                var nameSuggestsFile = (p.Name?.ToLowerInvariant().Contains("file") ?? false) ||
                                      (p.Name?.ToLowerInvariant().Contains("upload") ?? false);
                return (hasFromForm && (isFormFile || isFormFileList)) ||
                       ((isFormFile || isFormFileList) && nameSuggestsFile);
            })
            .ToList();

        if (fileParameters.Any())
        {
            // Remove file upload parameters from the parameters list
            var fileParameterNames = fileParameters.Select(p => p.Name ?? "file").ToHashSet();
            operation.Parameters = operation.Parameters
                .Where(p => p.Name == null || !fileParameterNames.Contains(p.Name))
                .ToList();

            // Create request body with multipart/form-data
            var properties = new Dictionary<string, OpenApiSchema>();
            var required = new HashSet<string>();

            foreach (var param in fileParameters)
            {
                var paramName = param.Name ?? "file";
                var isList = param.ParameterType.IsGenericType && 
                            param.ParameterType.GetGenericTypeDefinition() == typeof(List<>);

                properties[paramName] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = isList ? "Array of files to upload" : "File to upload"
                };

                if (!param.IsOptional && !param.HasDefaultValue)
                {
                    required.Add(paramName);
                }
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties,
                            Required = required
                        }
                    }
                },
                Required = required.Count > 0
            };
        }
    }
}

