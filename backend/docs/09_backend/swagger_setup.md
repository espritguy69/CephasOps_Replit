# Swagger/OpenAPI Setup Guide

## Overview

CephasOps API uses Swagger/OpenAPI for interactive API documentation and testing. Swagger UI is automatically enabled in Development mode.

## Accessing Swagger UI

1. **Development Environment:**
   - Navigate to: `http://localhost:5000` (or your configured port)
   - Swagger UI is available at the root path when running in Development mode

2. **Production Environment:**
   - Swagger UI is disabled by default for security
   - To enable in production, modify `Program.cs` to remove the `IsDevelopment()` check

## Authentication

Swagger UI supports JWT Bearer token authentication:

1. Click the **"Authorize"** button in Swagger UI
2. Enter your JWT token in the format: `Bearer <your-token>`
3. Click **"Authorize"** to authenticate
4. All subsequent API calls will include the token in the Authorization header

## Getting a JWT Token

To get a JWT token for testing:

1. Use the `/api/auth/login` endpoint
2. Or use your existing authentication mechanism
3. Copy the returned token

## API Documentation

All controllers and DTOs include XML documentation comments that are automatically included in Swagger:

- Controller endpoints show descriptions, parameters, and responses
- DTOs show property descriptions and types
- Request/response examples are generated automatically

## Configuration

Swagger configuration is in `Program.Swagger.cs`:

- **API Info:** Title, version, description
- **Security:** JWT Bearer token configuration
- **XML Comments:** Automatically included from generated XML files

## Customization

To customize Swagger:

1. Edit `Program.Swagger.cs`
2. Modify `AddSwaggerGen` options
3. Add custom examples, schemas, or filters as needed

## Troubleshooting

### Swagger UI Not Loading

- Check that `GenerateDocumentationFile` is enabled in `Directory.Build.props`
- Verify XML files are being generated in the build output
- Check that Swagger packages are installed: `Swashbuckle.AspNetCore`

### Authentication Not Working

- Verify JWT token format: `Bearer <token>`
- Check that token hasn't expired
- Verify API authentication middleware is configured correctly

### Missing Documentation

- Ensure XML documentation comments are present in controllers and DTOs
- Check that XML files are being generated during build
- Verify XML file path in `AddSwaggerGen` configuration

