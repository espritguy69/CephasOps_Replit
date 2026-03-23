using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace CephasOps.Api;

/// <summary>
/// Parameter filter to handle IFormFile parameters with [FromForm] attribute
/// This prevents Swagger from trying to generate parameters for file uploads
/// </summary>
public class FileUploadParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        var paramInfo = context.ParameterInfo;
        
        if (paramInfo != null)
        {
            var hasFromForm = paramInfo.GetCustomAttributes(typeof(FromFormAttribute), false).Any();
            var isFormFile = paramInfo.ParameterType == typeof(IFormFile);
            var isFormFileList = paramInfo.ParameterType.IsGenericType && 
                                paramInfo.ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                                paramInfo.ParameterType.GetGenericArguments()[0] == typeof(IFormFile);

            if (hasFromForm && (isFormFile || isFormFileList))
            {
                // Mark parameter as file upload type to prevent Swagger parameter generation errors
                // The operation filter will handle converting this to a request body
                parameter.Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
                parameter.Description = isFormFileList ? "Array of files to upload" : "File to upload";
                parameter.In = ParameterLocation.Query; // Temporary - operation filter will remove it
            }
        }
    }
}

