# Flexible External API Integration Architecture

CephasOps Configuration-Driven API Integration System – Full Specification

**Date:** December 8, 2025  
**Status:** 📋 Specification Complete - Ready for Implementation  
**Module:** Infrastructure & External Services  
**Priority:** High (Foundation for Future Integrations)

---

## 1. Purpose

This specification defines a **flexible, configuration-driven architecture** for integrating external APIs into CephasOps without hard-coding. The system allows adding new APIs, switching providers, and managing credentials through configuration (database/UI) rather than code changes.

**Business Requirements:**
- Add new external APIs without code deployment
- Switch between API providers (e.g., Twilio → another SMS provider) via configuration
- Manage API credentials securely through UI
- Enable/disable APIs without code changes
- Support multiple environments (Sandbox, Production)
- Future-proof architecture that scales to many APIs

**Current State:**
- ✅ Some APIs use configuration (Carbone uses `appsettings.json`)
- ✅ GlobalSettings system exists for key-value storage
- ❌ No unified pattern for external API integration
- ❌ Credentials hard-coded in `appsettings.json` or environment variables
- ❌ Adding new API requires code changes and deployment

**Target State:**
- ✅ Unified configuration-driven API integration system
- ✅ All API credentials stored in database (encrypted)
- ✅ UI for managing API configurations
- ✅ Factory pattern for dynamic API client creation
- ✅ Add new APIs through configuration only (no code)
- ✅ Switch providers by changing configuration

---

## 2. Core Concept

### 2.1 Configuration-Driven vs Code-Driven

**Old Way (Hard-Coded):**
```
1. Create MyInvoisApiClient.cs
2. Hard-code base URL: "https://api.myinvois.hasil.gov.my"
3. Hard-code credentials in appsettings.json
4. Deploy code changes
5. To switch provider → Change code → Redeploy
```

**New Way (Configuration-Driven):**
```
1. Admin adds MyInvois config in UI
2. System stores in GlobalSettings
3. MyInvoisProvider reads config dynamically
4. No deployment needed
5. To switch provider → Change config in UI → Done
```

### 2.2 Key Principles

1. **Configuration Over Code:** APIs are configured, not coded
2. **Interface-Based:** All APIs use interfaces (abstraction)
3. **Factory Pattern:** Dynamic client creation from configuration
4. **Secure Storage:** Credentials encrypted in database
5. **UI Management:** Admins configure APIs through Settings page
6. **Provider Agnostic:** Easy to switch providers

---

## 3. Architecture Overview

### 3.1 System Layers

```
┌─────────────────────────────────────────┐
│         UI Layer (Settings Page)        │
│  Admin configures APIs through UI       │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│      Configuration Storage Layer        │
│  GlobalSettings or ExternalApiConfig     │
│  Stores: Base URL, Credentials, etc.    │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│      Application Service Layer          │
│  IExternalApiConfigurationService       │
│  Manages configurations                 │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│      Factory Layer                      │
│  IExternalApiClientFactory              │
│  Creates API clients from config        │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│      Provider Implementation Layer       │
│  MyInvoisProvider, TwilioProvider, etc. │
│  Implements provider-specific logic     │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│      HTTP Client Layer                  │
│  HttpClient configured dynamically      │
│  Based on configuration                 │
└─────────────────────────────────────────┘
```

### 3.2 Data Flow

**Adding New API:**
1. Admin opens Settings → External API Configurations
2. Clicks "Add New API"
3. Selects provider type (MyInvois, Twilio, Custom, etc.)
4. Enters configuration (Base URL, credentials, etc.)
5. System stores in GlobalSettings (encrypted)
6. Factory automatically creates API client
7. API is immediately available for use

**Using API:**
1. Service calls `IEInvoiceProvider` interface
2. Factory provides configured client
3. Client reads configuration from GlobalSettings
4. Makes API call with configured credentials
5. Returns result

---

## 4. Data Model

### 4.1 Option 1: Use GlobalSettings (Recommended for MVP)

**Existing Table:** `GlobalSettings`

**Configuration Pattern:**
```
Key: ExternalApi_{ProviderName}_{SettingName}
Value: Setting value (encrypted for sensitive data)
ValueType: String, Bool, Int, Json
Module: "ExternalApi"
```

**Examples:**
```
ExternalApi_MyInvois_BaseUrl = "https://api.myinvois.hasil.gov.my"
ExternalApi_MyInvois_ClientId = "your-client-id"
ExternalApi_MyInvois_ClientSecret = "encrypted-secret"
ExternalApi_MyInvois_Enabled = "true"
ExternalApi_MyInvois_Environment = "Production"
ExternalApi_MyInvois_TimeoutSeconds = "30"

ExternalApi_Twilio_BaseUrl = "https://api.twilio.com"
ExternalApi_Twilio_AccountSid = "your-account-sid"
ExternalApi_Twilio_AuthToken = "encrypted-token"
ExternalApi_Twilio_Enabled = "true"
ExternalApi_Twilio_FromNumber = "+1234567890"
```

**Benefits:**
- Uses existing infrastructure
- No new tables needed
- Simple key-value pattern
- Already has encryption support

**Limitations:**
- Less structured than dedicated table
- Harder to query all APIs at once
- No built-in relationships

---

### 4.2 Option 2: Dedicated Table (Recommended for Phase 2)

**New Entity:** `ExternalApiConfiguration`

**Fields:**
- `Id` (Guid) - Primary key
- `CompanyId` (Guid?) - Company scope (nullable for global)
- `ProviderName` (string) - "MyInvois", "Twilio", "WhatsAppBusiness", etc.
- `ProviderType` (string) - "EInvoice", "SMS", "WhatsApp", "Payment", "Custom"
- `BaseUrl` (string) - API base URL
- `ConfigurationJson` (string) - JSON blob with all settings
- `IsEnabled` (bool) - Enable/disable this API
- `Environment` (string) - "Sandbox", "Production", "Development"
- `LastTestedAt` (DateTime?) - Last connection test
- `LastTestResult` (string?) - "Success", "Failed", "NotTested"
- `LastTestError` (string?) - Error message if test failed
- `CreatedAt`, `UpdatedAt` - Timestamps
- `CreatedByUserId`, `UpdatedByUserId` - Audit fields

**ConfigurationJson Structure:**
```json
{
  "clientId": "your-client-id",
  "clientSecret": "encrypted-secret",
  "apiKey": "encrypted-api-key",
  "timeoutSeconds": 30,
  "retryAttempts": 3,
  "customHeaders": {
    "X-Custom-Header": "value"
  },
  "customSettings": {
    "any": "custom",
    "settings": "here"
  }
}
```

**Benefits:**
- More structured
- Easy to query all APIs
- Better organization
- Supports company-scoped APIs
- Built-in test tracking

**Indexes:**
- `IX_ExternalApiConfigurations_ProviderName_CompanyId`
- `IX_ExternalApiConfigurations_ProviderType_IsEnabled`

---

## 5. Backend Architecture

### 5.1 Domain Layer

**Entity (Option 2):**
- `ExternalApiConfiguration.cs` - API configuration entity

**Value Objects:**
- `ApiCredentials.cs` - Encrypted credentials wrapper
- `ApiEndpoint.cs` - Endpoint configuration

### 5.2 Application Layer

**Core Services:**

**1. IExternalApiConfigurationService**
```csharp
Task<List<ExternalApiConfigurationDto>> GetAllConfigurationsAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
Task<ExternalApiConfigurationDto?> GetConfigurationAsync(string providerName, Guid? companyId = null, CancellationToken cancellationToken = default);
Task<ExternalApiConfigurationDto> CreateConfigurationAsync(CreateExternalApiConfigurationDto dto, Guid userId, CancellationToken cancellationToken = default);
Task<ExternalApiConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateExternalApiConfigurationDto dto, Guid userId, CancellationToken cancellationToken = default);
Task DeleteConfigurationAsync(Guid id, CancellationToken cancellationToken = default);
Task<ApiTestResult> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default);
```

**2. IExternalApiClientFactory**
```csharp
T GetClient<T>(string providerName, Guid? companyId = null) where T : class;
HttpClient GetHttpClient(string providerName, Guid? companyId = null);
bool IsProviderConfigured(string providerName, Guid? companyId = null);
```

**3. Provider Interfaces (Examples)**
```csharp
IEInvoiceProvider GetEInvoiceProvider(string providerName = "MyInvois", Guid? companyId = null);
ISmsProvider GetSmsProvider(string providerName = "Twilio", Guid? companyId = null);
IWhatsAppProvider GetWhatsAppProvider(string providerName = "WhatsAppBusiness", Guid? companyId = null);
```

**DTOs:**
- `ExternalApiConfigurationDto.cs`
- `CreateExternalApiConfigurationDto.cs`
- `UpdateExternalApiConfigurationDto.cs`
- `ApiTestResult.cs`

### 5.3 Infrastructure Layer

**1. ExternalApiClientFactory**
- Reads configuration from GlobalSettings or ExternalApiConfiguration table
- Creates HttpClient instances dynamically
- Configures authentication (OAuth, API Key, Basic Auth, etc.)
- Handles timeouts, retries, error handling

**2. Provider Implementations**
- `MyInvoisApiProvider.cs` - Implements `IEInvoiceProvider`
- `TwilioSmsProvider.cs` - Implements `ISmsProvider`
- `WhatsAppBusinessProvider.cs` - Implements `IWhatsAppProvider`
- Each reads configuration dynamically

**3. Encryption Service**
- `ICredentialEncryptionService.cs` - Encrypt/decrypt credentials
- Uses .NET Data Protection API or Azure Key Vault
- Never stores plain-text credentials

### 5.4 API Layer

**Controller:**
- `ExternalApiConfigurationsController.cs` - Manage API configurations

**Endpoints:**
```
GET    /api/external-api-configurations              → Get all configurations
GET    /api/external-api-configurations/{id}         → Get single configuration
POST   /api/external-api-configurations              → Create new configuration
PUT    /api/external-api-configurations/{id}         → Update configuration
DELETE /api/external-api-configurations/{id}         → Delete configuration
POST   /api/external-api-configurations/{id}/test    → Test API connection
GET    /api/external-api-configurations/providers    → Get available provider types
```

---

## 6. Frontend Architecture

### 6.1 Settings Page

**Route:** `/settings/external-api-configurations`

**Features:**
- List all configured APIs
- Add new API configuration
- Edit existing configuration
- Delete API configuration
- Test API connection
- Enable/disable APIs
- View API status (last test result)

### 6.2 Add/Edit API Form

**Fields:**
- **Provider Name:** Text input (e.g., "MyInvois", "Twilio")
- **Provider Type:** Dropdown (EInvoice, SMS, WhatsApp, Payment, Custom)
- **Base URL:** Text input (validated URL)
- **Environment:** Dropdown (Sandbox, Production, Development)
- **Credentials Section:**
  - Client ID / API Key (if applicable)
  - Client Secret / Auth Token (masked, encrypted)
  - Additional credentials (dynamic based on provider type)
- **Settings Section:**
  - Timeout (seconds)
  - Retry attempts
  - Custom headers (JSON)
  - Custom settings (JSON)
- **Status:**
  - Enabled/Disabled toggle
  - Test Connection button

### 6.3 API Status Display

**Status Indicators:**
- ✅ **Configured & Tested** - Green badge
- ⚠️ **Configured, Not Tested** - Yellow badge
- ❌ **Test Failed** - Red badge (show error)
- ⚪ **Not Configured** - Gray badge

**Last Test Info:**
- Last tested: "2 hours ago"
- Test result: "Success" or "Failed: Invalid credentials"
- Test button to re-test

---

## 7. Provider Types & Templates

### 7.1 Pre-Defined Provider Types

**E-Invoice Providers:**
- MyInvois (Malaysia)
- e-Invoice (Other countries)
- Custom e-Invoice provider

**SMS Providers:**
- Twilio
- Nexmo/Vonage
- Custom SMS gateway

**WhatsApp Providers:**
- WhatsApp Business API
- Twilio WhatsApp
- Custom WhatsApp provider

**Payment Providers:**
- Stripe
- PayPal
- iPay88 (Malaysia)
- Custom payment gateway

**Document/PDF Providers:**
- Carbone (already implemented)
- PDFShift
- Custom document service

### 7.2 Provider Templates

**Template Structure:**
Each provider type has a template defining:
- Required fields
- Optional fields
- Field validation rules
- Default values
- Help text/descriptions

**Example - MyInvois Template:**
```json
{
  "providerType": "EInvoice",
  "requiredFields": [
    "baseUrl",
    "clientId",
    "clientSecret",
    "companyTin"
  ],
  "optionalFields": [
    "timeoutSeconds",
    "digitalCertificatePath",
    "digitalCertificatePassword"
  ],
  "fieldDescriptions": {
    "baseUrl": "MyInvois API base URL (Sandbox or Production)",
    "clientId": "OAuth Client ID from IRBM",
    "clientSecret": "OAuth Client Secret (encrypted)",
    "companyTin": "Company TIN number"
  },
  "defaults": {
    "timeoutSeconds": 30,
    "environment": "Production"
  }
}
```

---

## 8. Security & Encryption

### 8.1 Credential Encryption

**Encryption Strategy:**
- Use .NET Data Protection API (DPAPI) for local development
- Use Azure Key Vault for production
- Never store plain-text credentials
- Encrypt on save, decrypt on use

**Implementation:**
```csharp
public interface ICredentialEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
    bool IsEncrypted(string value);
}
```

### 8.2 Access Control

**Permissions:**
- Only admins can configure APIs
- Regular users cannot view credentials
- Audit log of who changed what
- Role-based access control

### 8.3 Secure Storage

**Sensitive Data:**
- Client secrets
- API keys
- Auth tokens
- Certificate passwords
- Private keys

**Storage Location:**
- Database: Encrypted in GlobalSettings or ExternalApiConfiguration
- File System: Certificate files in secure location
- Azure Key Vault: For production (optional)

---

## 9. Factory Pattern Implementation

### 9.1 Client Factory

**Purpose:**
- Dynamically create API clients from configuration
- Handle different provider types
- Manage HttpClient lifecycle
- Cache clients for performance

**Implementation:**
```csharp
public class ExternalApiClientFactory : IExternalApiClientFactory
{
    private readonly IExternalApiConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly Dictionary<string, object> _clientCache;

    public T GetClient<T>(string providerName, Guid? companyId = null) where T : class
    {
        // 1. Get configuration
        var config = _configService.GetConfigurationAsync(providerName, companyId).Result;
        
        // 2. Check cache
        var cacheKey = $"{providerName}_{companyId}";
        if (_clientCache.ContainsKey(cacheKey))
            return _clientCache[cacheKey] as T;
        
        // 3. Create HttpClient
        var httpClient = _httpClientFactory.CreateClient(providerName);
        ConfigureHttpClient(httpClient, config);
        
        // 4. Create provider instance
        var provider = CreateProviderInstance<T>(providerName, httpClient, config);
        
        // 5. Cache and return
        _clientCache[cacheKey] = provider;
        return provider;
    }
}
```

### 9.2 Provider Registration

**Dynamic Registration:**
- Providers registered via dependency injection
- Factory resolves provider type from string
- Supports multiple implementations of same interface

**Example:**
```csharp
// Register providers
services.AddScoped<MyInvoisApiProvider>();
services.AddScoped<TwilioSmsProvider>();
services.AddScoped<WhatsAppBusinessProvider>();

// Factory resolves based on provider name
var provider = factory.GetClient<IEInvoiceProvider>("MyInvois");
```

---

## 10. Usage Examples

### 10.1 Adding MyInvois API

**Step 1: Admin Configuration (UI)**
1. Go to Settings → External API Configurations
2. Click "Add New API"
3. Select Provider Type: "E-Invoice"
4. Provider Name: "MyInvois"
5. Base URL: "https://api.myinvois.hasil.gov.my"
6. Client ID: "your-client-id"
7. Client Secret: "your-secret" (encrypted automatically)
8. Company TIN: "123456789012"
9. Environment: "Production"
10. Click "Save"

**Step 2: System Automatically**
- Stores configuration in database (encrypted)
- Registers API client factory entry
- Makes API available for use

**Step 3: Using the API**
```csharp
// Service code (no changes needed)
var eInvoiceProvider = _apiFactory.GetClient<IEInvoiceProvider>("MyInvois");
var result = await eInvoiceProvider.SubmitInvoiceAsync(invoiceData);
```

### 10.2 Switching SMS Providers

**Scenario:** Switch from Twilio to Nexmo

**Old Way:**
1. Change code in `TwilioSmsProvider.cs`
2. Update `appsettings.json`
3. Deploy code changes
4. Test in production

**New Way:**
1. Admin opens Settings → External API Configurations
2. Finds "Twilio" configuration
3. Changes Provider Name to "Nexmo"
4. Updates Base URL and credentials
5. Clicks "Save"
6. Done! (No deployment)

**Code remains unchanged:**
```csharp
// Same code works with different provider
var smsProvider = _apiFactory.GetClient<ISmsProvider>("Nexmo"); // Just change name
await smsProvider.SendSmsAsync(phoneNumber, message);
```

### 10.3 Multi-Environment Support

**Scenario:** Different credentials for Sandbox and Production

**Configuration:**
- `ExternalApi_MyInvois_Sandbox_BaseUrl` = "https://sandbox-api.myinvois.hasil.gov.my"
- `ExternalApi_MyInvois_Sandbox_ClientId` = "sandbox-client-id"
- `ExternalApi_MyInvois_Production_BaseUrl` = "https://api.myinvois.hasil.gov.my"
- `ExternalApi_MyInvois_Production_ClientId` = "production-client-id"

**Usage:**
```csharp
// Use environment variable or setting to determine which to use
var environment = _configService.GetValueAsync<string>("MyInvois_Environment");
var provider = _apiFactory.GetClient<IEInvoiceProvider>($"MyInvois_{environment}");
```

---

## 11. Implementation Phases

### Phase 1: Foundation (Week 1)

**Backend:**
- ✅ Create `ExternalApiConfiguration` entity (or use GlobalSettings)
- ✅ Create `IExternalApiConfigurationService`
- ✅ Create `IExternalApiClientFactory`
- ✅ Implement credential encryption service
- ✅ Create API controller for configurations

**Frontend:**
- ✅ Create Settings page for API configurations
- ✅ Create Add/Edit form
- ✅ Create API list view
- ✅ Add test connection functionality

**Timeline:** 3-5 days

---

### Phase 2: Provider Integration (Week 2)

**Backend:**
- ✅ Implement factory pattern
- ✅ Create provider interfaces (IEInvoiceProvider, ISmsProvider, etc.)
- ✅ Migrate existing APIs (Carbone, etc.) to new system
- ✅ Implement MyInvois provider using new system
- ✅ Implement Twilio provider (if needed)

**Frontend:**
- ✅ Provider templates/forms
- ✅ Dynamic form fields based on provider type
- ✅ Connection testing UI
- ✅ Status indicators

**Timeline:** 3-5 days

---

### Phase 3: Advanced Features (Week 3)

**Backend:**
- 🔄 Provider discovery/registration
- 🔄 API health monitoring
- 🔄 Usage analytics
- 🔄 Rate limiting per API
- 🔄 Webhook support

**Frontend:**
- 🔄 API usage dashboard
- 🔄 Health status monitoring
- 🔄 Bulk configuration import/export
- 🔄 Provider marketplace (pre-configured templates)

**Timeline:** 3-5 days

---

## 12. Migration Strategy

### 12.1 Migrating Existing APIs

**Carbone (Example):**
1. Extract configuration from `appsettings.json`
2. Create ExternalApiConfiguration entry via UI
3. Update `CarboneRenderer` to use factory
4. Remove hard-coded configuration
5. Test and verify

**Benefits:**
- Configuration now manageable via UI
- Can switch providers easily
- Supports multiple environments

### 12.2 Backward Compatibility

**During Migration:**
- Support both old (appsettings.json) and new (database) configuration
- Old config takes precedence if exists
- Gradually migrate APIs one by one
- No breaking changes

---

## 13. Testing Checklist

### Backend Testing:
- [ ] Create API configuration
- [ ] Update API configuration
- [ ] Delete API configuration
- [ ] Test connection functionality
- [ ] Factory creates clients correctly
- [ ] Credentials encrypted/decrypted properly
- [ ] Multiple providers work simultaneously
- [ ] Company-scoped configurations
- [ ] Environment switching (Sandbox/Production)

### Frontend Testing:
- [ ] Add new API configuration
- [ ] Edit existing configuration
- [ ] Delete configuration
- [ ] Test connection button
- [ ] Status indicators display correctly
- [ ] Form validation
- [ ] Credential masking
- [ ] Provider type templates

### Integration Testing:
- [ ] End-to-end: Add config → Use API → Verify works
- [ ] End-to-end: Switch provider → Verify new provider works
- [ ] End-to-end: Update credentials → Verify new credentials used
- [ ] Multi-provider: Multiple APIs work simultaneously

---

## 14. Benefits Summary

### 14.1 For Developers
- ✅ No code changes to add new APIs
- ✅ Easy to test with different providers
- ✅ Clean separation of concerns
- ✅ Reusable patterns

### 14.2 For Admins
- ✅ Configure APIs through UI
- ✅ No deployment needed
- ✅ Easy to switch providers
- ✅ Test connections before using

### 14.3 For Business
- ✅ Faster time to market for new integrations
- ✅ Lower development costs
- ✅ Easy to adapt to changing requirements
- ✅ Better security (encrypted credentials)

---

## 15. Future Enhancements

### 15.1 Advanced Features
- 🔄 API versioning support
- 🔄 Automatic API discovery
- 🔄 Provider marketplace
- 🔄 API usage analytics
- 🔄 Cost tracking per API
- 🔄 Webhook management

### 15.2 Integration Improvements
- 🔄 GraphQL API support
- 🔄 gRPC API support
- 🔄 Custom authentication methods
- 🔄 API response caching
- 🔄 Request/response transformation

---

## 16. Related Documentation

- **Global Settings:** `docs/02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md`
- **MyInvois Integration:** `docs/02_modules/billing/MYINVOIS_AUTOMATIC_SUBMISSION.md`
- **External Portals:** `docs/02_modules/external_portals/SPECIFICATION.md`
- **Carbone Integration:** `backend/src/CephasOps.Application/Settings/Services/CarboneRenderer.cs`

---

## 17. Implementation Status

| Component | Status | Completeness |
|-----------|--------|--------------|
| Specification | ✅ Complete | 100% |
| Configuration Entity | ⏳ Pending | 0% |
| Factory Pattern | ⏳ Pending | 0% |
| Encryption Service | ⏳ Pending | 0% |
| Configuration Service | ⏳ Pending | 0% |
| Frontend UI | ⏳ Pending | 0% |
| Provider Migrations | ⏳ Pending | 0% |
| Testing | ⏳ Pending | 0% |

**Overall Status:** 📋 Specification Complete - Ready for Implementation

---

## 18. Next Steps

1. **Decision: Configuration Storage**
   - Use GlobalSettings (simpler, faster) or ExternalApiConfiguration table (more structured)?
   - Recommendation: Start with GlobalSettings for MVP, migrate to table in Phase 2

2. **Decision: Encryption Method**
   - .NET Data Protection API (local) or Azure Key Vault (production)?
   - Recommendation: Both - DPAPI for dev, Key Vault for production

3. **Begin Implementation:**
   - Start with Phase 1 (Foundation)
   - Create factory pattern
   - Migrate one existing API (Carbone) as proof of concept
   - Build UI for configuration management
   - Test thoroughly

4. **Provider Migration Plan:**
   - List all existing external APIs
   - Prioritize migration order
   - Migrate one at a time
   - Test each migration

---

## 19. Questions & Decisions Needed

1. **Configuration Storage:**
   - Use GlobalSettings or dedicated table? (Recommend: Start with GlobalSettings)

2. **Encryption:**
   - Which encryption method? (Recommend: DPAPI for dev, Key Vault for production)

3. **Provider Types:**
   - Which provider types to support initially? (Recommend: EInvoice, SMS, WhatsApp)

4. **Company Scope:**
   - Should APIs be company-scoped or global? (Recommend: Both - global default, company override)

5. **UI Location:**
   - Where in Settings should API configurations live? (Recommend: New section "External APIs")

---

**Document Version:** 1.0  
**Last Updated:** December 8, 2025  
**Author:** CephasOps Development Team  
**Status:** 📋 Ready for Implementation  
**Priority:** 🔴 High (Foundation for Future Integrations)

