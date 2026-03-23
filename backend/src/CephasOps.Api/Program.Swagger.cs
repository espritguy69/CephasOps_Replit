using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CephasOps.Api;

/// <summary>
/// Swagger/OpenAPI configuration
/// </summary>
public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CephasOps API",
                Version = "v1",
                Description = "CephasOps Operations Platform API",
                Contact = new OpenApiContact
                {
                    Name = "CephasOps Support",
                    Email = "support@cephasops.com"
                }
            });

            // Include XML comments
            try
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                
                // Also check in bin/Debug/net10.0 folder if not found in base directory
                if (!File.Exists(xmlPath))
                {
                    var binPath = Path.Combine(AppContext.BaseDirectory, "bin", "Debug", "net10.0", xmlFile);
                    if (File.Exists(binPath))
                    {
                        xmlPath = binPath;
                    }
                }
                
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            }
            catch (Exception)
            {
                // If XML file doesn't exist or can't be loaded, continue without it
                // Swagger will still work without XML comments
            }

            // JWT Bearer authentication configuration
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Ignore schema errors that might occur during generation
            options.IgnoreObsoleteActions();
            options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
            
            // Handle schema generation errors gracefully
            try
            {
                options.SchemaFilter<SwaggerSchemaFilter>();
            }
            catch
            {
                // If schema filter registration fails, continue without it
            }
            
            // Suppress schema generation errors
            options.UseAllOfToExtendReferenceSchemas();
            options.UseOneOfForPolymorphism();
            
            // Configure Swagger to handle form data properly FIRST
            // This must be done before filters to prevent parameter generation errors
            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
            
            options.MapType<FileStream>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
            
            // Handle file uploads with [FromForm] and IFormFile
            // Parameter filter runs during parameter generation to configure IFormFile parameters
            options.ParameterFilter<FileUploadParameterFilter>();
            // Operation filter converts IFormFile parameters to multipart/form-data request body
            options.OperationFilter<FileUploadOperationFilter>();
            // Document filter as fallback (runs after generation to fix any remaining issues)
            options.DocumentFilter<FileUploadDocumentFilter>();
            
            // Suppress parameter generation errors - filters will handle file uploads
            options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        });

        return services;
    }

    public static WebApplication UseSwaggerConfiguration(this WebApplication app)
    {
        // Enable Swagger in Development and allow it in other environments if needed
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
            // Configure Swagger to handle errors gracefully
            c.SerializeAsV2 = false; // Use OpenAPI 3.0
        });
        
        // Add error handling middleware for Swagger generation
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger") && 
                context.Request.Path.Value?.EndsWith(".json") == true)
            {
                try
                {
                    await next();
                }
                catch (Exception ex) when (ex.Message.Contains("IFormFile") || ex.Message.Contains("FromForm"))
                {
                    // Log the error but don't expose it to the client
                    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("SwaggerConfiguration");
                    logger.LogError(ex, "Swagger generation error for file upload endpoint");
                    
                    // Return a valid but minimal Swagger document
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    
                    var minimalSwagger = @"{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""CephasOps API"",
    ""version"": ""v1"",
    ""description"": ""CephasOps Operations Platform API""
  },
  ""paths"": {}
}";
                    
                    await context.Response.WriteAsync(minimalSwagger);
                    return;
                }
            }
            else
            {
                await next();
            }
        });
        
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CephasOps API v1");
            options.RoutePrefix = "swagger"; // Swagger UI at /swagger
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.EnableTryItOutByDefault();
        });

        return app;
    }
}

