# Improvement Plan - Next Week

**Date:** December 8, 2025  
**Status:** 📋 Planning Document  
**Purpose:** Tasks for SMS/WhatsApp delivery integration and MyInvois hybrid architecture  
**Next Movement:** ✅ Active development plan - See `docs/08_infrastructure/SDK_INTEGRATION_GUIDE.md` for OneDrive sync integration (optional enhancement)  
**Priority:** High - External API integration

---

## Overview

This document outlines the implementation tasks for the next week, focusing on:

1. **SMS & WhatsApp Delivery Integration** - Connect existing templates to actual delivery channels (Twilio)
2. **MyInvois Hybrid Architecture** - Complete the settings-based, fail-proof e-Invoice submission system

Both features follow the **hybrid architecture** principle: optional, settings-based, fail-proof, and never blocking the main workflow.

---

## 1. SMS & WhatsApp Delivery Integration

### 1.1 Current State

**What Exists:**
- ✅ SMS Templates entity and CRUD operations
- ✅ WhatsApp Templates entity and CRUD operations
- ✅ Template rendering service (placeholder replacement)
- ✅ Frontend UI for managing templates
- ✅ Template controllers and API endpoints

**What's Missing:**
- ❌ Actual SMS delivery (no provider implementation)
- ❌ Actual WhatsApp delivery (no provider implementation)
- ❌ Integration with workflow engine (order status changes)
- ❌ Provider abstraction (interfaces, factory pattern)
- ❌ Settings-based configuration (enable/disable, provider selection)

### 1.2 Implementation Tasks

#### Phase 1: Provider Interfaces & Abstraction

**Task 1.1: Create SMS Provider Interface**
- **File:** `backend/src/CephasOps.Application/Notifications/Services/ISmsProvider.cs`
- **Purpose:** Define contract for SMS providers
- **Methods:**
  - `SendSmsAsync(string to, string message, CancellationToken)`
  - `GetStatusAsync(string messageId, CancellationToken)`
- **Status:** ⏳ Pending

**Task 1.2: Create WhatsApp Provider Interface**
- **File:** `backend/src/CephasOps.Application/Notifications/Services/IWhatsAppProvider.cs`
- **Purpose:** Define contract for WhatsApp providers
- **Methods:**
  - `SendMessageAsync(string to, string message, CancellationToken)`
  - `GetStatusAsync(string messageId, CancellationToken)`
- **Status:** ⏳ Pending

**Task 1.3: Create Result DTOs**
- **File:** `backend/src/CephasOps.Application/Notifications/DTOs/SmsResult.cs`
- **File:** `backend/src/CephasOps.Application/Notifications/DTOs/WhatsAppResult.cs`
- **Purpose:** Standardize provider response format
- **Status:** ⏳ Pending

#### Phase 2: Provider Implementations

**Task 2.1: Install Twilio SDK**
- **Command:** `dotnet add package Twilio --version 6.0.0`
- **Location:** `backend/src/CephasOps.Infrastructure`
- **Status:** ⏳ Pending

**Task 2.2: Implement Twilio SMS Provider**
- **File:** `backend/src/CephasOps.Infrastructure/Services/External/TwilioSmsProvider.cs`
- **Purpose:** Actual SMS sending via Twilio
- **Features:**
  - Initialize Twilio client from settings
  - Send SMS with error handling
  - Return standardized result
- **Status:** ⏳ Pending

**Task 2.3: Implement Twilio WhatsApp Provider**
- **File:** `backend/src/CephasOps.Infrastructure/Services/External/TwilioWhatsAppProvider.cs`
- **Purpose:** Actual WhatsApp sending via Twilio
- **Features:**
  - Initialize Twilio client from settings
  - Send WhatsApp message with error handling
  - Return standardized result
- **Status:** ⏳ Pending

**Task 2.4: Implement Null Providers (Fail-Proof)**
- **File:** `backend/src/CephasOps.Infrastructure/Services/External/NullSmsProvider.cs`
- **File:** `backend/src/CephasOps.Infrastructure/Services/External/NullWhatsAppProvider.cs`
- **Purpose:** Safe fallback when SMS/WhatsApp disabled
- **Behavior:** No-op (does nothing, returns success)
- **Status:** ⏳ Pending

#### Phase 3: Factory Pattern

**Task 3.1: Create SMS Provider Factory**
- **File:** `backend/src/CephasOps.Application/Notifications/Services/SmsProviderFactory.cs`
- **Purpose:** Select provider based on GlobalSettings
- **Logic:**
  - Read `SMS_Provider` from GlobalSettings
  - Return `TwilioSmsProvider` if "Twilio"
  - Return `NullSmsProvider` if "None" or disabled
- **Status:** ⏳ Pending

**Task 3.2: Create WhatsApp Provider Factory**
- **File:** `backend/src/CephasOps.Application/Notifications/Services/WhatsAppProviderFactory.cs`
- **Purpose:** Select provider based on GlobalSettings
- **Logic:**
  - Read `WhatsApp_Provider` from GlobalSettings
  - Return `TwilioWhatsAppProvider` if "Twilio"
  - Return `NullWhatsAppProvider` if "None" or disabled
- **Status:** ⏳ Pending

#### Phase 4: Notification Service

**Task 4.1: Create Customer Notification Service**
- **File:** `backend/src/CephasOps.Application/Notifications/Services/CustomerNotificationService.cs`
- **Purpose:** Connect templates to providers
- **Features:**
  - Get template by code
  - Render template with order data
  - Send via SMS provider (if enabled)
  - Send via WhatsApp provider (if enabled)
  - Handle errors gracefully (don't throw)
- **Status:** ⏳ Pending

**Task 4.2: Add Status-to-Template Mapping**
- **File:** `backend/src/CephasOps.Application/Notifications/Services/NotificationTemplateMapper.cs`
- **Purpose:** Map order status to template codes
- **Mapping:**
  - `Assigned` → `ASSIGNED`
  - `OnTheWay` → `OTW`
  - `MetCustomer` → `MET_CUSTOMER`
  - `InProgress` → `IN_PROGRESS`
  - `Completed` → `COMPLETED`
  - `Cancelled` → `CANCELLED`
  - `Rescheduled` → `RESCHEDULED`
- **Status:** ⏳ Pending

#### Phase 5: Workflow Integration

**Task 5.1: Hook into Workflow Engine**
- **File:** `backend/src/CephasOps.Application/Workflow/Services/WorkflowEngine.cs` (modify)
- **Purpose:** Trigger notifications on status change
- **Logic:**
  - When order status changes, fire event
  - Notification service listens to event
  - Check if auto-send enabled (`SMS_AutoSendOnStatusChange`)
  - Send notification asynchronously (fire-and-forget)
- **Status:** ⏳ Pending

**Task 5.2: Add Notification Event Handler**
- **File:** `backend/src/CephasOps.Application/Notifications/Handlers/OrderStatusChangedNotificationHandler.cs`
- **Purpose:** Handle order status change events
- **Features:**
  - Get order details
  - Get customer phone number
  - Map status to template code
  - Send SMS/WhatsApp via notification service
  - Log errors (don't throw)
- **Status:** ⏳ Pending

#### Phase 6: GlobalSettings Configuration

**Task 6.1: Add SMS Settings to GlobalSettings**
- **Settings to Add:**
  - `SMS_Enabled` (Bool)
  - `SMS_Provider` (String: "Twilio", "Nexmo", "None")
  - `SMS_Twilio_AccountSid` (String, Encrypted)
  - `SMS_Twilio_AuthToken` (String, Encrypted)
  - `SMS_Twilio_FromNumber` (String)
  - `SMS_AutoSendOnStatusChange` (Bool)
  - `SMS_RetryAttempts` (Int)
  - `SMS_RetryDelaySeconds` (Int)
- **Status:** ⏳ Pending

**Task 6.2: Add WhatsApp Settings to GlobalSettings**
- **Settings to Add:**
  - `WhatsApp_Enabled` (Bool)
  - `WhatsApp_Provider` (String: "Twilio", "Facebook", "None")
  - `WhatsApp_Twilio_AccountSid` (String, Encrypted)
  - `WhatsApp_Twilio_AuthToken` (String, Encrypted)
  - `WhatsApp_Twilio_FromNumber` (String)
  - `WhatsApp_AutoSendOnStatusChange` (Bool)
  - `WhatsApp_RetryAttempts` (Int)
  - `WhatsApp_RetryDelaySeconds` (Int)
- **Status:** ⏳ Pending

**Task 6.3: Add Template Mapping Settings**
- **Settings to Add:**
  - `Notification_OrderAssigned_SmsTemplateCode` (String)
  - `Notification_OrderAssigned_WhatsAppTemplateCode` (String)
  - `Notification_OnTheWay_SmsTemplateCode` (String)
  - `Notification_OnTheWay_WhatsAppTemplateCode` (String)
  - `Notification_Completed_SmsTemplateCode` (String)
  - `Notification_Completed_WhatsAppTemplateCode` (String)
  - (And more for other statuses)
- **Status:** ⏳ Pending

#### Phase 7: Dependency Injection

**Task 7.1: Register Providers in DI**
- **File:** `backend/src/CephasOps.Api/Program.cs`
- **Purpose:** Wire up all providers and services
- **Registrations:**
  - Register `ISmsProvider` → Factory-based resolution
  - Register `IWhatsAppProvider` → Factory-based resolution
  - Register `SmsProviderFactory`
  - Register `WhatsAppProviderFactory`
  - Register `CustomerNotificationService`
  - Register `OrderStatusChangedNotificationHandler`
- **Status:** ⏳ Pending

#### Phase 8: Frontend Enhancements

**Task 8.1: Add SMS/WhatsApp Settings UI**
- **File:** `frontend/src/pages/settings/SmsWhatsAppSettingsPage.tsx` (new)
- **Purpose:** UI for configuring SMS/WhatsApp providers
- **Features:**
  - Enable/disable toggles
  - Provider selection dropdown
  - Credential input fields (encrypted)
  - Test send button
- **Status:** ⏳ Pending

**Task 8.2: Add Manual Send Buttons**
- **File:** `frontend/src/pages/orders/OrderDetailPage.tsx` (modify)
- **Purpose:** Allow manual SMS/WhatsApp sending
- **Features:**
  - "Send SMS" button
  - "Send WhatsApp" button
  - Show send status/history
- **Status:** ⏳ Pending

#### Phase 9: Testing

**Task 9.1: Unit Tests**
- Test provider interfaces
- Test factory pattern
- Test notification service
- Test template rendering
- Test error handling
- **Status:** ⏳ Pending

**Task 9.2: Integration Tests**
- Test with Twilio sandbox credentials
- Test with SMS disabled (should skip)
- Test with invalid template (should skip)
- Test with provider failure (should log, continue)
- **Status:** ⏳ Pending

---

## 2. MyInvois Hybrid Architecture

### 2.1 Current State

**What Exists:**
- ✅ Invoice entity with `SubmissionId` and `SubmittedAt` fields
- ✅ Invoice submission history entity
- ✅ Invoice submission service (basic structure)
- ✅ Frontend API calls (`submitEInvoice`, `getEInvoiceStatus`)
- ✅ Frontend UI for invoice submission

**What's Missing:**
- ❌ Actual MyInvois API integration (no provider implementation)
- ❌ Settings-based configuration (API credentials, enable/disable)
- ❌ Two-step submission flow (MyInvois → TIME portal)
- ❌ Invoice v1.1 compliance (listVersionID="1.1")
- ❌ SDK 1.0 features (currency exchange, digital signature)
- ❌ Background job for status polling
- ❌ Fail-proof error handling

### 2.2 Implementation Tasks

#### Phase 1: MyInvois Provider Interface

**Task 1.1: Create E-Invoice Provider Interface**
- **File:** `backend/src/CephasOps.Application/Billing/Services/IEInvoiceProvider.cs`
- **Purpose:** Define contract for e-Invoice providers
- **Methods:**
  - `SubmitInvoiceAsync(InvoiceDto, CancellationToken)` → Returns UUID, QR code
  - `GetSubmissionStatusAsync(string submissionId, CancellationToken)`
  - `CancelInvoiceAsync(string uuid, CancellationToken)`
  - `GetAccessTokenAsync(CancellationToken)` (OAuth)
- **Status:** ⏳ Pending

**Task 1.2: Create MyInvois DTOs**
- **File:** `backend/src/CephasOps.Application/Billing/DTOs/MyInvoisInvoiceDto.cs`
- **File:** `backend/src/CephasOps.Application/Billing/DTOs/MyInvoisSubmissionResponse.cs`
- **File:** `backend/src/CephasOps.Application/Billing/DTOs/MyInvoisStatusResponse.cs`
- **Purpose:** Standardize MyInvois API payloads
- **Status:** ⏳ Pending

#### Phase 2: MyInvois Provider Implementation

**Task 2.1: Research MyInvois SDK**
- **Action:** Check if official .NET SDK exists
- **URL:** `https://dev-sdk.myinvois.hasil.gov.my`
- **Alternative:** Use HttpClient with REST API
- **Status:** ⏳ Pending

**Task 2.2: Implement MyInvois API Provider**
- **File:** `backend/src/CephasOps.Infrastructure/Services/External/MyInvoisApiProvider.cs`
- **Purpose:** Actual MyInvois API integration
- **Features:**
  - OAuth 2.0 Client Credentials flow
  - Submit invoice (Invoice v1.1 with listVersionID="1.1")
  - Poll for status (UUID, QR code, validation status)
  - Handle currency exchange (non-MYR invoices)
  - Digital signature validation
  - Error handling and retry logic
- **Status:** ⏳ Pending

**Task 2.3: Implement Null E-Invoice Provider**
- **File:** `backend/src/CephasOps.Infrastructure/Services/External/NullEInvoiceProvider.cs`
- **Purpose:** Safe fallback when MyInvois disabled
- **Behavior:** No-op (returns mock UUID, doesn't call API)
- **Status:** ⏳ Pending

#### Phase 3: Invoice v1.1 Compliance

**Task 3.1: Update Invoice XML/JSON Builder**
- **File:** `backend/src/CephasOps.Application/Billing/Services/InvoiceXmlBuilder.cs` (new or modify)
- **Purpose:** Build MyInvois-compliant invoice payload
- **Requirements:**
  - Set `InvoiceTypeCode` with `listVersionID="1.1"`
  - Include all required fields per SDK 1.0
  - Handle currency exchange rates (non-MYR)
  - Include digital signature fields
- **Status:** ⏳ Pending

**Task 3.2: Add Currency Exchange Rate Logic**
- **File:** `backend/src/CephasOps.Application/Billing/Services/CurrencyExchangeService.cs` (new)
- **Purpose:** Fetch and apply exchange rates for non-MYR invoices
- **Features:**
  - Get exchange rate from MyInvois API (if available)
  - Or use configured exchange rate source
  - Apply to invoice amounts
- **Status:** ⏳ Pending

#### Phase 4: Two-Step Submission Flow

**Task 4.1: Update Invoice Submission Service**
- **File:** `backend/src/CephasOps.Application/Billing/Services/InvoiceSubmissionService.cs` (modify)
- **Purpose:** Implement two-step flow
- **Flow:**
  1. Step 1: Submit to MyInvois API → Get UUID/QR code
  2. Step 2: Generate PDF with UUID/QR code → Manual upload to TIME portal
  3. Step 3: User confirms TIME upload → Update order status to `SubmittedToPortal`
- **Status:** ⏳ Pending

**Task 4.2: Add MyInvois Status Polling**
- **File:** `backend/src/CephasOps.Application/Billing/Services/MyInvoisStatusPoller.cs` (new)
- **Purpose:** Background job to poll MyInvois for status
- **Features:**
  - Poll every 5 minutes (configurable)
  - Check for pending submissions
  - Update invoice status (Validated, Rejected, etc.)
  - Store QR code and validation status
- **Status:** ⏳ Pending

**Task 4.3: Add TIME Portal Upload Confirmation**
- **File:** `backend/src/CephasOps.Api/Controllers/InvoiceSubmissionsController.cs` (modify)
- **Purpose:** Endpoint for manual TIME portal upload confirmation
- **Endpoint:** `POST /api/billing/invoices/{invoiceId}/confirm-time-upload`
- **Status:** ⏳ Pending

#### Phase 5: GlobalSettings Configuration

**Task 5.1: Add MyInvois Settings**
- **Settings to Add:**
  - `MyInvois_Enabled` (Bool)
  - `MyInvois_BaseUrl` (String: "https://api.myinvois.hasil.gov.my")
  - `MyInvois_ClientId` (String, Encrypted)
  - `MyInvois_ClientSecret` (String, Encrypted)
  - `MyInvois_Environment` (String: "Production", "Sandbox")
  - `MyInvois_TimeoutSeconds` (Int: 30)
  - `MyInvois_RetryAttempts` (Int: 3)
  - `MyInvois_RetryDelaySeconds` (Int: 5)
  - `MyInvois_PollingIntervalMinutes` (Int: 5)
  - `MyInvois_InvoiceVersion` (String: "1.1")
- **Status:** ⏳ Pending

#### Phase 6: Background Jobs

**Task 6.1: Set Up Background Job Framework**
- **Option 1:** Hangfire (if not already installed)
- **Option 2:** Quartz.NET
- **Option 3:** .NET Background Service
- **Status:** ⏳ Pending

**Task 6.2: Create MyInvois Status Polling Job**
- **File:** `backend/src/CephasOps.Application/Billing/Jobs/MyInvoisStatusPollingJob.cs`
- **Purpose:** Recurring job to poll MyInvois for submission status
- **Schedule:** Every 5 minutes (configurable)
- **Status:** ⏳ Pending

#### Phase 7: Credit Note Support

**Task 7.1: Add Credit Note Submission**
- **File:** `backend/src/CephasOps.Application/Billing/Services/CreditNoteSubmissionService.cs` (new)
- **Purpose:** Handle daily correction flow
- **Features:**
  - Create credit note referencing original UUID
  - Submit to MyInvois
  - Adjust job status
  - Re-issue corrected invoice
- **Status:** ⏳ Pending

#### Phase 8: Frontend Updates

**Task 8.1: Update Invoice Detail Page**
- **File:** `frontend/src/pages/billing/InvoiceDetailPage.tsx` (modify)
- **Purpose:** Show two-step submission flow
- **Features:**
  - Show MyInvois submission status (UUID, QR code)
  - Show TIME portal upload status
  - "Confirm TIME Upload" button
  - Download PDF with UUID/QR code
- **Status:** ⏳ Pending

**Task 8.2: Add MyInvois Settings UI**
- **File:** `frontend/src/pages/settings/MyInvoisSettingsPage.tsx` (new)
- **Purpose:** UI for configuring MyInvois
- **Features:**
  - Enable/disable toggle
  - API credentials input (encrypted)
  - Test connection button
  - Status polling configuration
- **Status:** ⏳ Pending

#### Phase 9: Testing

**Task 9.1: Unit Tests**
- Test MyInvois provider
- Test invoice XML builder (v1.1 compliance)
- Test currency exchange logic
- Test two-step submission flow
- **Status:** ⏳ Pending

**Task 9.2: Integration Tests**
- Test with MyInvois sandbox
- Test with MyInvois disabled (should skip)
- Test with API failure (should log, continue)
- Test credit note submission
- **Status:** ⏳ Pending

---

## 3. Priority & Timeline

### Week 1 (Days 1-2): Foundation
- ✅ Create provider interfaces
- ✅ Install Twilio SDK
- ✅ Implement Twilio providers
- ✅ Implement null providers
- ✅ Create factories

### Week 1 (Days 3-4): Integration
- ✅ Create notification service
- ✅ Integrate with workflow engine
- ✅ Add GlobalSettings configuration
- ✅ Register in DI

### Week 1 (Days 5-7): MyInvois
- ✅ Research MyInvois SDK/API
- ✅ Implement MyInvois provider
- ✅ Implement invoice v1.1 builder
- ✅ Implement two-step submission flow
- ✅ Add background job for polling

### Week 2: Frontend & Testing
- ✅ Frontend UI enhancements
- ✅ Unit tests
- ✅ Integration tests
- ✅ Documentation updates

---

## 4. Success Criteria

### SMS/WhatsApp Integration
- ✅ Can enable/disable SMS/WhatsApp via settings
- ✅ Can switch providers (Twilio, None) without code change
- ✅ Notifications sent automatically on order status change
- ✅ Workflow never blocked by notification failure
- ✅ All errors logged, system continues normally

### MyInvois Integration
- ✅ Can enable/disable MyInvois via settings
- ✅ Invoices submitted automatically to MyInvois
- ✅ UUID and QR code received and stored
- ✅ PDF generated with UUID/QR code for TIME portal
- ✅ Manual TIME portal upload confirmation works
- ✅ Credit notes supported for daily correction
- ✅ Invoice v1.1 compliant (listVersionID="1.1")
- ✅ Background polling updates submission status

---

## 5. Risks & Mitigation

### Risk 1: MyInvois SDK Not Available
- **Mitigation:** Use HttpClient with REST API, follow official documentation

### Risk 2: Twilio API Changes
- **Mitigation:** Use interface abstraction, easy to swap providers

### Risk 3: Performance Impact
- **Mitigation:** Use async/await, fire-and-forget pattern, background jobs

### Risk 4: API Rate Limiting
- **Mitigation:** Implement retry logic with exponential backoff, respect rate limits

---

## 6. Related Documentation

- **SDK Integration Guide:** `docs/08_infrastructure/SDK_INTEGRATION_GUIDE.md`
- **MyInvois Specification:** `docs/02_modules/billing/MYINVOIS_AUTOMATIC_SUBMISSION.md`
- **Flexible API Integration:** `docs/02_modules/integrations/FLEXIBLE_API_INTEGRATION.md`
- **Workflow Engine:** `docs/01_system/WORKFLOW_ENGINE.md`
- **Global Settings:** `docs/02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md`

---

**Document Version:** 1.0  
**Last Updated:** December 8, 2025  
**Author:** CephasOps Development Team  
**Status:** 📋 Planning Document

