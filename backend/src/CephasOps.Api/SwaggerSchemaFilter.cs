using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace CephasOps.Api;

/// <summary>
/// Swagger schema filter to handle schema generation errors
/// </summary>
public class SwaggerSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        try
        {
            // Handle potential circular references
            if (schema.Properties != null && schema.Properties.Count > 0)
            {
                foreach (var property in schema.Properties.ToList())
                {
                    try
                    {
                        // If a property references itself or creates a cycle, simplify it
                        if (property.Value.Reference != null && 
                            property.Value.Reference.Id == context.Type.Name)
                        {
                            property.Value.Reference = null;
                            property.Value.Type = "object";
                        }
                    }
                    catch
                    {
                        // Ignore individual property errors
                    }
                }
            }
        }
        catch
        {
            // If schema generation fails, continue with default schema
        }
    }
}

