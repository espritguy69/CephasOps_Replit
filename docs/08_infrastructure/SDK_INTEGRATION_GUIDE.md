# External API SDK Integration Guide

CephasOps Full-Stack SDK Integration – Complete Guide

**Date:** December 8, 2025  
**Status:** 📋 Complete Integration Guide  
**Purpose:** How to add and integrate external API SDKs into CephasOps  
**Next Improvement Plan:** ✅ OneDrive File Sync Integration (optional enhancement) - See Phase 4 in this document  
**Priority:** High - External API integration

---

## 1. Overview

This guide explains how to integrate external API SDKs (like MyInvois SDK, Twilio SDK, WhatsApp Business SDK, etc.) into CephasOps following Clean Architecture principles.

**Key Concepts:**
- **Backend SDKs:** NuGet packages (C#/.NET)
- **Frontend SDKs:** npm packages (JavaScript/TypeScript)
- **Integration Pattern:** Configuration-driven, interface-based
- **Location:** Infrastructure layer for backend, API/services layer for frontend

---

## 2. Backend SDK Integration (.NET)

### 2.1 Where SDKs Are Located

**SDK Packages are NuGet Packages:**
- **Repository:** NuGet.org (public) or private NuGet feeds
- **Storage:** Referenced in `.csproj` files
- **Installation:** Via `dotnet add package` or Visual Studio Package Manager

**Current SDKs in CephasOps:**
- Syncfusion packages (Excel, PDF, Word)
- Entity Framework Core
- MailKit (Email)
- JWT Authentication
- And more...

**Location in Project:**
```
backend/
  src/
    CephasOps.Infrastructure/
      CephasOps.Infrastructure.csproj  ← Add SDK packages here
    CephasOps.Application/
      CephasOps.Application.csproj      ← Add SDK packages here (if needed)
    CephasOps.Api/
      CephasOps.Api.csproj             ← Add SDK packages here (if needed)
```

### 2.2 How to Add a New SDK Package

#### Step 1: Find the SDK Package

**For MyInvois:**
- Search NuGet.org: "MyInvois" or "IRBM"
- Or check IRBM documentation: `https://dev-sdk.myinvois.hasil.gov.my`
- Package name might be: `MyInvois.SDK` or `IRBM.MyInvois` (example)

**For Twilio:**
- Package: `Twilio` (official Twilio .NET SDK)
- NuGet: `https://www.nuget.org/packages/Twilio`

**For WhatsApp Business:**
- Package: `WhatsAppBusiness.CloudAPI` or `Twilio` (Twilio supports WhatsApp)
- NuGet: Search "WhatsApp Business API"

#### Step 2: Install the Package

**Method 1: Command Line (Recommended)**
```powershell
# Navigate to Infrastructure project
cd backend/src/CephasOps.Infrastructure

# Add package
dotnet add package Twilio --version 6.0.0

# Or for MyInvois (if available)
dotnet add package MyInvois.SDK --version 1.0.0
```

**Method 2: Edit .csproj File**
```xml
<ItemGroup>
  <!-- Add new SDK package -->
  <PackageReference Include="Twilio" Version="6.0.0" />
  <PackageReference Include="MyInvois.SDK" Version="1.0.0" />
</ItemGroup>
```

Then run:
```powershell
dotnet restore
```

#### Step 3: Verify Installation

```powershell
# Check if package is installed
dotnet list package

# Or check .csproj file
# Should see new PackageReference entry
```

### 2.3 Integration Architecture

**Following Clean Architecture:**

```
┌─────────────────────────────────────┐
│   Infrastructure Layer              │
│   (Where SDKs Live)                 │
│                                     │
│   - TwilioSmsProvider.cs           │
│   - MyInvoisApiProvider.cs         │
│   - WhatsAppBusinessProvider.cs    │
│   (Uses SDK packages)              │
└─────────────────────────────────────┘
              ↓ implements
┌─────────────────────────────────────┐
│   Application Layer                  │
│   (Interfaces)                       │
│                                     │
│   - ISmsProvider.cs                 │
│   - IEInvoiceProvider.cs            │
│   - IWhatsAppProvider.cs            │
│   (No SDK dependencies)             │
└─────────────────────────────────────┘
              ↓ used by
┌─────────────────────────────────────┐
│   API Layer                          │
│   (Controllers)                      │
│                                     │
│   - Uses interfaces only            │
│   (No SDK dependencies)            │
└─────────────────────────────────────┘
```

### 2.4 Implementation Pattern

**Example: Adding Twilio SMS SDK**

**Step 1: Install Package**
```powershell
cd backend/src/CephasOps.Infrastructure
dotnet add package Twilio --version 6.0.0
```

**Step 2: Create Interface (Application Layer)**
```csharp
// Application/Notifications/Services/ISmsProvider.cs
public interface ISmsProvider
{
    Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default);
    Task<SmsStatus> GetStatusAsync(string messageId, CancellationToken cancellationToken = default);
}
```

**Step 3: Implement Provider (Infrastructure Layer)**
```csharp
// Infrastructure/Services/External/TwilioSmsProvider.cs
using Twilio;
using Twilio.Rest.Api.V2010.Account;

public class TwilioSmsProvider : ISmsProvider
{
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(
        IOptions<TwilioSettings> settings,
        ILogger<TwilioSmsProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        // Initialize Twilio SDK
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
    }

    public async Task<SmsResult> SendSmsAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        // Use Twilio SDK
        var twilioMessage = await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_settings.FromNumber),
            to: new Twilio.Types.PhoneNumber(to)
        );

        return new SmsResult
        {
            Success = true,
            MessageId = twilioMessage.Sid,
            Status = twilioMessage.Status.ToString()
        };
    }
}
```

**Step 4: Register in Dependency Injection**
```csharp
// Api/Program.cs
builder.Services.AddScoped<ISmsProvider, TwilioSmsProvider>();
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));
```

**Step 5: Use in Services**
```csharp
// Application/Notifications/Services/NotificationService.cs
public class NotificationService
{
    private readonly ISmsProvider _smsProvider; // Interface, not SDK directly

    public NotificationService(ISmsProvider smsProvider)
    {
        _smsProvider = smsProvider;
    }

    public async Task SendSmsNotificationAsync(string phoneNumber, string message)
    {
        await _smsProvider.SendSmsAsync(phoneNumber, message);
    }
}
```

### 2.5 Configuration Management

**Store SDK Credentials Securely:**

**Option 1: appsettings.json (Development)**
```json
{
  "Twilio": {
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "FromNumber": "+1234567890"
  }
}
```

**Option 2: GlobalSettings (Production - Recommended)**
```
ExternalApi_Twilio_AccountSid = "your-account-sid"
ExternalApi_Twilio_AuthToken = "encrypted-token"
ExternalApi_Twilio_FromNumber = "+1234567890"
ExternalApi_Twilio_Enabled = "true"
```

**Option 3: Environment Variables**
```powershell
# Set environment variable
$env:TWILIO_ACCOUNT_SID = "your-account-sid"
$env:TWILIO_AUTH_TOKEN = "your-auth-token"
```

**Option 4: User Secrets (Development)**
```powershell
cd backend/src/CephasOps.Api
dotnet user-secrets set "Twilio:AccountSid" "your-account-sid"
dotnet user-secrets set "Twilio:AuthToken" "your-auth-token"
```

---

## 3. Frontend SDK Integration (React/TypeScript)

### 3.1 Where SDKs Are Located

**SDK Packages are npm Packages:**
- **Repository:** npmjs.com (public) or private npm registry
- **Storage:** Referenced in `package.json`
- **Installation:** Via `npm install` or `yarn add`

**Current SDKs in CephasOps:**
- Syncfusion React packages
- TanStack Query
- React Router
- And more...

**Location in Project:**
```
frontend/
  package.json  ← Add SDK packages here
  src/
    api/        ← API client code (uses SDKs)
    services/   ← Service wrappers (uses SDKs)
```

### 3.2 How to Add a New SDK Package

#### Step 1: Find the SDK Package

**For Twilio (Frontend):**
- Usually, Twilio is backend-only (security)
- Frontend might use Twilio Client SDK for voice/video: `twilio-client`

**For WhatsApp Business (Frontend):**
- Usually backend-only
- Frontend might use WhatsApp Web API wrapper

**For Maps/Geolocation:**
- `@react-google-maps/api` - Google Maps
- `leaflet` - OpenStreetMap
- `@mapbox/mapbox-gl-js` - Mapbox

#### Step 2: Install the Package

```powershell
# Navigate to frontend directory
cd frontend

# Install package
npm install twilio-client --save

# Or for TypeScript
npm install @types/twilio-client --save-dev
```

#### Step 3: Verify Installation

```powershell
# Check package.json
# Should see new entry in dependencies

# Or check installed packages
npm list
```

### 3.3 Integration Pattern

**Frontend SDK Usage:**

**Example: Adding Google Maps SDK**

**Step 1: Install Package**
```powershell
cd frontend
npm install @react-google-maps/api --save
```

**Step 2: Create Service Wrapper**
```typescript
// frontend/src/services/maps.ts
import { GoogleMap, LoadScript } from '@react-google-maps/api';

const GOOGLE_MAPS_API_KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY;

export const MapsService = {
  loadGoogleMaps: () => {
    return LoadScript({
      googleMapsApiKey: GOOGLE_MAPS_API_KEY
    });
  },
  
  createMap: (center: { lat: number; lng: number }) => {
    return GoogleMap({
      center,
      zoom: 15
    });
  }
};
```

**Step 3: Use in Components**
```typescript
// frontend/src/components/maps/MapComponent.tsx
import { MapsService } from '../../services/maps';

export const MapComponent: React.FC = () => {
  return (
    <MapsService.loadGoogleMaps>
      <MapsService.createMap center={{ lat: 3.1390, lng: 101.6869 }} />
    </MapsService.loadGoogleMaps>
  );
};
```

---

## 4. Common SDK Integration Examples

### 4.1 MyInvois SDK Integration

**Backend (.NET):**

**Step 1: Check if SDK exists**
- Visit: `https://dev-sdk.myinvois.hasil.gov.my`
- Download SDK or check NuGet for package
- If no official SDK, use HTTP client (REST API)

**Step 2: Install (if SDK available)**
```powershell
cd backend/src/CephasOps.Infrastructure
dotnet add package MyInvois.SDK --version 1.0.0
```

**Step 3: Implement Provider**
```csharp
// Infrastructure/Services/External/MyInvoisApiProvider.cs
public class MyInvoisApiProvider : IEInvoiceProvider
{
    private readonly MyInvoisClient _client; // SDK client
    private readonly MyInvoisSettings _settings;

    public MyInvoisApiProvider(MyInvoisSettings settings)
    {
        _settings = settings;
        _client = new MyInvoisClient(
            baseUrl: _settings.BaseUrl,
            clientId: _settings.ClientId,
            clientSecret: _settings.ClientSecret
        );
    }

    public async Task<MyInvoisSubmissionResponse> SubmitInvoiceAsync(
        MyInvoisInvoiceDto invoice, 
        CancellationToken cancellationToken)
    {
        // Use SDK method
        return await _client.SubmitInvoiceAsync(invoice, cancellationToken);
    }
}
```

**If No Official SDK:**
- Use `HttpClient` to call REST API directly
- Follow API documentation
- Implement OAuth authentication manually

### 4.2 Twilio SDK Integration

**Backend (.NET):**

**Step 1: Install Package**
```powershell
cd backend/src/CephasOps.Infrastructure
dotnet add package Twilio --version 6.0.0
```

**Step 2: Create Settings Class**
```csharp
// Application/Settings/TwilioSettings.cs
public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
```

**Step 3: Implement Provider**
```csharp
// Infrastructure/Services/External/TwilioSmsProvider.cs
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

public class TwilioSmsProvider : ISmsProvider
{
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(
        IOptions<TwilioSettings> settings,
        ILogger<TwilioSmsProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        if (_settings.Enabled)
        {
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }
    }

    public async Task<SmsResult> SendSmsAsync(
        string to, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Twilio SMS is not enabled");
        }

        try
        {
            var twilioMessage = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_settings.FromNumber),
                to: new PhoneNumber(to)
            );

            return new SmsResult
            {
                Success = true,
                MessageId = twilioMessage.Sid,
                Status = twilioMessage.Status.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS via Twilio");
            throw;
        }
    }
}
```

**Step 4: Register in DI**
```csharp
// Api/Program.cs
builder.Services.Configure<TwilioSettings>(
    builder.Configuration.GetSection("Twilio"));
builder.Services.AddScoped<ISmsProvider, TwilioSmsProvider>();
```

### 4.3 WhatsApp Business API Integration

**Backend (.NET):**

**Option 1: Use Twilio (Supports WhatsApp)**
```powershell
dotnet add package Twilio --version 6.0.0
```

**Option 2: Use Facebook Graph API (Official)**
- No official .NET SDK
- Use `HttpClient` with Graph API
- Or use community package: `Facebook.GraphAPI`

**Implementation:**
```csharp
// Infrastructure/Services/External/WhatsAppBusinessProvider.cs
public class WhatsAppBusinessProvider : IWhatsAppProvider
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;

    public WhatsAppBusinessProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient("WhatsAppBusiness");
        _settings = settings.Value;
        
        _httpClient.BaseAddress = new Uri($"https://graph.facebook.com/v18.0/");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
    }

    public async Task<WhatsAppResult> SendMessageAsync(
        string to, 
        string message, 
        CancellationToken cancellationToken)
    {
        var request = new
        {
            messaging_product = "whatsapp",
            to = to,
            type = "text",
            text = new { body = message }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.PhoneNumberId}/messages", 
            request, 
            cancellationToken);

        // Handle response...
    }
}
```

---

## 5. SDK Integration Checklist

### 5.1 Before Adding SDK

- [ ] Check if official SDK exists (NuGet/npm)
- [ ] Review SDK documentation
- [ ] Check SDK version compatibility (.NET 10, React 18)
- [ ] Review license (free, paid, open source)
- [ ] Check security vulnerabilities
- [ ] Review SDK maintenance status (actively maintained?)

### 5.2 During Integration

- [ ] Install SDK package
- [ ] Create interface in Application layer
- [ ] Implement provider in Infrastructure layer
- [ ] Add configuration settings
- [ ] Register in Dependency Injection
- [ ] Add error handling
- [ ] Add logging
- [ ] Write unit tests

### 5.3 After Integration

- [ ] Test with sandbox/test credentials
- [ ] Test error scenarios
- [ ] Test rate limiting
- [ ] Test retry logic
- [ ] Document configuration
- [ ] Update API documentation
- [ ] Add to Settings UI (if configurable)

---

## 6. Best Practices

### 6.1 Architecture Principles

1. **Interface-Based Design:**
   - Always create interface first
   - Implementation uses SDK
   - Services depend on interface, not SDK

2. **Configuration-Driven:**
   - Store credentials in GlobalSettings or appsettings.json
   - Never hard-code credentials
   - Support multiple environments

3. **Error Handling:**
   - Wrap SDK calls in try-catch
   - Log errors appropriately
   - Return domain-friendly error types

4. **Dependency Injection:**
   - Register providers in DI container
   - Use factory pattern for multiple providers
   - Support switching providers via configuration

### 6.2 Security

1. **Credential Storage:**
   - Encrypt sensitive credentials
   - Use user-secrets for development
   - Use Azure Key Vault for production

2. **API Keys:**
   - Never commit API keys to git
   - Use environment variables
   - Rotate keys regularly

3. **SDK Updates:**
   - Keep SDKs updated (security patches)
   - Test after updating
   - Review changelog for breaking changes

### 6.3 Testing

1. **Unit Tests:**
   - Mock SDK interfaces
   - Test error scenarios
   - Test configuration loading

2. **Integration Tests:**
   - Use sandbox/test credentials
   - Test real API calls
   - Test rate limiting

3. **Staging Tests:**
   - Test with production-like environment
   - Verify all features work
   - Performance testing

---

## 7. Common SDK Packages Reference

### 7.1 Backend (.NET) SDKs

**SMS:**
- `Twilio` - Twilio SMS/WhatsApp
- `Nexmo` - Vonage SMS
- `MessageBird` - MessageBird SMS

**Email:**
- `MailKit` - ✅ Already installed (SMTP/IMAP)
- `SendGrid` - SendGrid email
- `Amazon.SES` - AWS SES

**Payment:**
- `Stripe.net` - Stripe payments
- `PayPal` - PayPal SDK
- `iPay88` - iPay88 (Malaysia)

**E-Invoice:**
- `MyInvois.SDK` - (Check if available)
- Or use `HttpClient` with REST API

**WhatsApp:**
- `Twilio` - Twilio WhatsApp
- Or use `HttpClient` with Facebook Graph API

**Maps/Geolocation:**
- `Google.Maps` - Google Maps .NET SDK
- `Mapbox` - Mapbox SDK

### 7.2 Frontend (React) SDKs

**Maps:**
- `@react-google-maps/api` - Google Maps
- `leaflet` - OpenStreetMap
- `@mapbox/mapbox-gl-js` - Mapbox

**Charts/Analytics:**
- `@syncfusion/ej2-react-charts` - ✅ Already installed
- `recharts` - React charts
- `chart.js` - Chart.js

**File Upload:**
- `react-dropzone` - File upload
- `@uploadcare/react-uploader` - Uploadcare

**Communication:**
- `socket.io-client` - WebSocket client
- `twilio-client` - Twilio voice/video

---

## 8. Step-by-Step: Adding MyInvois SDK

### 8.1 Check SDK Availability

**Visit IRBM SDK Portal:**
- URL: `https://dev-sdk.myinvois.hasil.gov.my`
- Check for .NET SDK download
- Review API documentation

**If SDK Available:**
1. Download SDK package
2. Extract to `backend/src/CephasOps.Infrastructure/Libs/MyInvois/`
3. Add reference in `.csproj`

**If No SDK (Use REST API):**
1. Use `HttpClient` directly
2. Follow API documentation
3. Implement OAuth manually

### 8.2 Implementation Steps

**Step 1: Create Interface**
```csharp
// Application/Billing/Services/IEInvoiceProvider.cs
public interface IEInvoiceProvider
{
    Task<MyInvoisSubmissionResponse> SubmitInvoiceAsync(
        MyInvoisInvoiceDto invoice, 
        CancellationToken cancellationToken);
    
    Task<MyInvoisStatusResponse> GetSubmissionStatusAsync(
        string submissionId, 
        CancellationToken cancellationToken);
}
```

**Step 2: Implement Provider**
```csharp
// Infrastructure/Services/External/MyInvoisApiProvider.cs
public class MyInvoisApiProvider : IEInvoiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly MyInvoisSettings _settings;
    private readonly ILogger<MyInvoisApiProvider> _logger;

    public MyInvoisApiProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<MyInvoisSettings> settings,
        ILogger<MyInvoisApiProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("MyInvois");
        _settings = settings.Value;
        _logger = logger;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<MyInvoisSubmissionResponse> SubmitInvoiceAsync(
        MyInvoisInvoiceDto invoice, 
        CancellationToken cancellationToken)
    {
        // Get OAuth token
        var token = await GetAccessTokenAsync(cancellationToken);
        
        // Set authorization header
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        // Submit invoice
        var response = await _httpClient.PostAsJsonAsync(
            "/api/v1.0/invoices", 
            invoice, 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<MyInvoisSubmissionResponse>();
        return result!;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // OAuth 2.0 Client Credentials flow
        var request = new
        {
            grant_type = "client_credentials",
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/oauth/token", 
            request, 
            cancellationToken);
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>();
        return tokenResponse!.access_token;
    }
}
```

**Step 3: Register in DI**
```csharp
// Api/Program.cs

// Register HTTP client
builder.Services.AddHttpClient("MyInvois", client =>
{
    // Base configuration
});

// Register settings
builder.Services.Configure<MyInvoisSettings>(
    builder.Configuration.GetSection("MyInvois"));

// Register provider
builder.Services.AddScoped<IEInvoiceProvider, MyInvoisApiProvider>();
```

**Step 4: Add Configuration**
```json
// appsettings.json
{
  "MyInvois": {
    "BaseUrl": "https://api.myinvois.hasil.gov.my",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Environment": "Production",
    "TimeoutSeconds": 30
  }
}
```

---

## 9. Troubleshooting

### 9.1 Common Issues

**Issue: Package not found**
```
Solution: Check package name, version, and NuGet source
```

**Issue: Version conflict**
```
Solution: Update all related packages to compatible versions
```

**Issue: SDK requires .NET version mismatch**
```
Solution: Check SDK requirements, upgrade .NET if needed
```

**Issue: Authentication fails**
```
Solution: Verify credentials, check token expiry, review SDK docs
```

### 9.2 Debugging Tips

1. **Enable SDK Logging:**
   - Most SDKs support logging
   - Enable in appsettings.json
   - Review logs for errors

2. **Test with Sandbox:**
   - Always test with sandbox first
   - Verify credentials work
   - Test all API endpoints

3. **Check SDK Documentation:**
   - Review official examples
   - Check GitHub issues
   - Review API changelog

---

## 10. Maintenance

### 10.1 Keeping SDKs Updated

**Regular Updates:**
- Check for updates monthly
- Review security advisories
- Test updates in development first

**Update Process:**
```powershell
# Check outdated packages
dotnet list package --outdated

# Update specific package
dotnet add package Twilio --version 6.1.0

# Update all packages (careful!)
dotnet add package Twilio
```

### 10.2 Version Pinning

**Recommendation:**
- Pin major versions (e.g., `6.0.0`)
- Allow minor/patch updates (e.g., `6.0.*`)
- Test before updating major versions

---

## 13. SMS & WhatsApp Hybrid Architecture

### 13.1 Current State

**What's Already Implemented:**
- ✅ SMS Templates entity (`SmsTemplate`) - Full CRUD operations
- ✅ WhatsApp Templates entity (`WhatsAppTemplate`) - Full CRUD operations
- ✅ Template services (`ISmsTemplateService`, `IWhatsAppTemplateService`)
- ✅ Template controllers (`SmsTemplatesController`, `WhatsAppTemplatesController`)
- ✅ Frontend UI pages (`SmsTemplatesPageEnhanced`, `WhatsAppTemplatesPageEnhanced`)
- ✅ Template rendering with placeholders (`RenderMessageAsync`)

**What's Missing:**
- ❌ SMS delivery implementation (no actual sending)
- ❌ WhatsApp delivery implementation (no actual sending)
- ❌ Integration with workflow (order status changes)
- ❌ Provider abstraction (Twilio, etc.)

### 13.2 Hybrid Architecture Design

**Core Principle:** SMS/WhatsApp delivery is optional and fail-proof

**Architecture Flow:**
```
Order Status Change → Workflow Engine → Notification Service
    ↓
Check if SMS/WhatsApp enabled (GlobalSettings)
    ↓
IF Enabled: Get template → Render → Send via provider
IF Disabled: Skip (no-op, log only)
IF Provider fails: Log error, continue workflow (don't block)
```

**Key Features:**
- ✅ Optional integration (can be enabled/disabled)
- ✅ Fail-proof (workflow never blocked)
- ✅ Settings-based configuration
- ✅ Template-driven messages
- ✅ Multiple provider support (Twilio, Nexmo, etc.)

### 13.3 Template Integration Pattern

**How Templates Connect to Delivery:**

1. **Order Status Change:**
   - Order status: `Assigned` → `OnTheWay`
   - Workflow engine triggers notification

2. **Template Selection:**
   - Map status to template code: `OnTheWay` → `"OTW"`
   - Get template from database: `SmsTemplate.Code = "OTW"`

3. **Template Rendering:**
   - Replace placeholders with order data:
     - `{customerName}` → "John Doe"
     - `{appointmentDate}` → "08 Dec 2025"
     - `{installerName}` → "SI Name"

4. **Delivery (Hybrid):**
   - IF SMS enabled: Send via provider
   - IF SMS disabled: Skip (no-op)
   - IF provider fails: Log error, continue

### 13.4 Implementation Approach

**Step 1: Install SMS/WhatsApp SDKs**
- Install Twilio NuGet package in Infrastructure project
- Or use HttpClient for REST API approach

**Step 2: Create Provider Interfaces**
- Define what SMS providers must do (send message, check status)
- Define what WhatsApp providers must do (send message, check status)
- Interfaces live in Application layer (no SDK dependencies)

**Step 3: Implement Providers (Hybrid Pattern)**
- Twilio provider: Actually sends messages when enabled
- Null provider: Does nothing when disabled (safe fallback)
- Both implement the same interface
- Factory chooses which one based on settings

**Step 4: Create Notification Service**
- Connects templates to providers
- Gets template based on order status
- Renders template with order data
- Sends via provider (if enabled)
- Handles errors gracefully (don't block workflow)

**Step 5: Integrate with Workflow**
- Hook into workflow engine status change events
- Fire-and-forget pattern (don't block main workflow)
- Map order status to template codes
- Send notifications asynchronously

### 13.5 GlobalSettings Configuration

**SMS Settings:**
```
SMS_Enabled (Bool) - Master switch for SMS integration
SMS_Provider (String) - "Twilio", "Nexmo", "MessageBird", "None"
SMS_Twilio_AccountSid (String, Encrypted)
SMS_Twilio_AuthToken (String, Encrypted)
SMS_Twilio_FromNumber (String)
SMS_AutoSendOnStatusChange (Bool) - Auto-send on order status change
SMS_RetryAttempts (Int) - Number of retries on failure
SMS_RetryDelaySeconds (Int) - Delay between retries
```

**WhatsApp Settings:**
```
WhatsApp_Enabled (Bool) - Master switch for WhatsApp integration
WhatsApp_Provider (String) - "Twilio", "Facebook", "None"
WhatsApp_Twilio_AccountSid (String, Encrypted)
WhatsApp_Twilio_AuthToken (String, Encrypted)
WhatsApp_Twilio_FromNumber (String)
WhatsApp_AutoSendOnStatusChange (Bool) - Auto-send on order status change
WhatsApp_RetryAttempts (Int) - Number of retries on failure
WhatsApp_RetryDelaySeconds (Int) - Delay between retries
```

**Template Mapping Settings:**
```
Notification_OrderAssigned_SmsTemplateCode (String) - "ASSIGNED"
Notification_OrderAssigned_WhatsAppTemplateCode (String) - "ASSIGNED"
Notification_OnTheWay_SmsTemplateCode (String) - "OTW"
Notification_OnTheWay_WhatsAppTemplateCode (String) - "OTW"
Notification_Completed_SmsTemplateCode (String) - "COMPLETED"
Notification_Completed_WhatsAppTemplateCode (String) - "COMPLETED"
```

### 13.6 Status-to-Template Mapping

**Order Status → Template Code Mapping:**

| Order Status | SMS Template Code | WhatsApp Template Code | When to Send |
|-------------|-------------------|------------------------|--------------|
| `Assigned` | `ASSIGNED` | `ASSIGNED` | When SI assigned |
| `OnTheWay` | `OTW` | `OTW` | When SI marks "On the Way" |
| `MetCustomer` | `MET_CUSTOMER` | `MET_CUSTOMER` | When SI marks "Met Customer" |
| `InProgress` | `IN_PROGRESS` | `IN_PROGRESS` | When installation starts |
| `Completed` | `COMPLETED` | `COMPLETED` | When job completed |
| `Cancelled` | `CANCELLED` | `CANCELLED` | When job cancelled |
| `Rescheduled` | `RESCHEDULED` | `RESCHEDULED` | When appointment rescheduled |

**Note:** Templates must exist in database with matching codes. If template not found, notification is skipped (fail-proof).

### 13.7 Provider Factory Pattern

**Factory Design:**
- Factory reads `SMS_Provider` from GlobalSettings
- Returns appropriate provider instance:
  - `"Twilio"` → `TwilioSmsProvider`
  - `"Nexmo"` → `NexmoSmsProvider`
  - `"None"` → `NullSmsProvider` (does nothing, safe fallback)
- Same pattern for WhatsApp

**Benefits:**
- Easy to switch providers (change setting, no code change)
- Easy to disable (set provider to "None")
- Easy to add new providers (implement interface, register in factory)

### 13.8 Error Handling & Fail-Proof Design

**Error Scenarios:**

1. **Provider Disabled:**
   - Check `SMS_Enabled` or `WhatsApp_Enabled`
   - If false, skip notification (no-op)
   - Log info: "SMS disabled, skipping notification"

2. **Provider Fails:**
   - Catch exception from provider
   - Log error with details
   - Continue workflow (don't throw, don't block)
   - Optionally retry (if configured)

3. **Template Not Found:**
   - If template code doesn't exist in database
   - Log warning: "Template not found: {code}"
   - Skip notification (fail-proof)

4. **Invalid Phone Number:**
   - Validate phone number format
   - If invalid, log warning, skip
   - Don't throw exception

**Fail-Proof Principle:**
- **Never block workflow** due to notification failure
- **Always log** errors/warnings for debugging
- **Always continue** order processing even if notification fails
- **Allow manual retry** via UI if needed

### 13.9 Workflow Integration Points

**Where Notifications Are Triggered:**

1. **Order Status Change (Automatic):**
   - Workflow engine fires `OrderStatusChanged` event
   - Notification service listens to event
   - Checks if auto-send enabled (`SMS_AutoSendOnStatusChange`)
   - Sends notification if enabled

2. **Manual Trigger (UI):**
   - Finance/Admin can manually send notification
   - Via "Send SMS" or "Send WhatsApp" button
   - Uses same template rendering logic

3. **Scheduled Notifications (Future):**
   - Reminder notifications (e.g., appointment tomorrow)
   - Background job checks for pending reminders
   - Sends notifications asynchronously

### 13.10 Implementation Checklist

**Backend Tasks:**
- [ ] Create `ISmsProvider` interface (Application layer)
- [ ] Create `IWhatsAppProvider` interface (Application layer)
- [ ] Implement `TwilioSmsProvider` (Infrastructure layer)
- [ ] Implement `TwilioWhatsAppProvider` (Infrastructure layer)
- [ ] Implement `NullSmsProvider` (Infrastructure layer, no-op)
- [ ] Implement `NullWhatsAppProvider` (Infrastructure layer, no-op)
- [ ] Create `SmsProviderFactory` (Application layer)
- [ ] Create `WhatsAppProviderFactory` (Application layer)
- [ ] Create `CustomerNotificationService` (Application layer)
- [ ] Integrate with workflow engine (hook into status change events)
- [ ] Add GlobalSettings for SMS/WhatsApp configuration
- [ ] Add error handling and logging
- [ ] Add retry logic (if configured)

**Frontend Tasks:**
- [ ] Add SMS/WhatsApp settings UI (if not exists)
- [ ] Add "Send SMS" / "Send WhatsApp" buttons (manual trigger)
- [ ] Show notification status/history in order detail page
- [ ] Add provider selection dropdown in settings

**Testing Tasks:**
- [ ] Test with SMS enabled (Twilio sandbox)
- [ ] Test with SMS disabled (should skip, no errors)
- [ ] Test with invalid template code (should skip, log warning)
- [ ] Test with provider failure (should log error, continue workflow)
- [ ] Test template rendering with various placeholders
- [ ] Test status-to-template mapping

### 13.11 Related Documentation

- **SMS Templates:** `docs/02_modules/settings/SMS_TEMPLATES.md` (if exists)
- **WhatsApp Templates:** `docs/02_modules/settings/WHATSAPP_TEMPLATES.md` (if exists)
- **Workflow Engine:** `docs/01_system/WORKFLOW_ENGINE.md`
- **Global Settings:** `docs/02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md`
- **Flexible API Integration:** `docs/02_modules/integrations/FLEXIBLE_API_INTEGRATION.md`

---

## 11. Related Documentation

- **Flexible API Integration:** `docs/02_modules/integrations/FLEXIBLE_API_INTEGRATION.md`
- **MyInvois Integration:** `docs/02_modules/billing/MYINVOIS_AUTOMATIC_SUBMISSION.md`
- **Clean Architecture:** `docs/01_system/ARCHITECTURE_BOOK.md`
- **Configuration Management:** `docs/02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md`

---

## 12. Quick Reference

### 12.1 Backend SDK Installation

```powershell
# Navigate to Infrastructure project
cd backend/src/CephasOps.Infrastructure

# Add package
dotnet add package [PackageName] --version [Version]

# Restore packages
dotnet restore

# Build to verify
dotnet build
```

### 12.2 Frontend SDK Installation

```powershell
# Navigate to frontend
cd frontend

# Add package
npm install [package-name] --save

# Or with version
npm install [package-name]@[version] --save

# Install dependencies
npm install
```

### 12.3 Project File Locations

**Backend:**
- `backend/src/CephasOps.Infrastructure/CephasOps.Infrastructure.csproj`
- `backend/src/CephasOps.Application/CephasOps.Application.csproj`
- `backend/src/CephasOps.Api/CephasOps.Api.csproj`

**Frontend:**
- `frontend/package.json`

---

**Document Version:** 1.0  
**Last Updated:** December 8, 2025  
**Author:** CephasOps Development Team  
**Status:** 📋 Complete Integration Guide

