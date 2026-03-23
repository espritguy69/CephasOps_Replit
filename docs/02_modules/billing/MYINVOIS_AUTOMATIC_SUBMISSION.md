# MyInvois e-Invoice Automatic Submission Integration

CephasOps MyInvois API Integration – Full Specification

**Date:** December 8, 2025  
**Status:** 📋 Specification Complete - Ready for Implementation  
**Module:** Billing & e-Invoice Automation  
**Priority:** High (Government Compliance Requirement)

---

## 1. Purpose

This specification defines the automatic integration with Malaysia's MyInvois e-Invoice system (LHDN/IRBM) to enable **fully automated invoice submission** with minimal manual intervention from the finance team. 

**Business Requirement:**
- All invoices must be submitted to MyInvois as per government policy
- Finance team should not need to manually submit each invoice
- System should automatically submit when invoice status changes to "Sent"
- System should handle errors, retries, and status tracking automatically
- Finance team should have simple visibility into submission status

**Current State:**
- ✅ Invoice submission tracking exists (`InvoiceSubmissionHistory`)
- ✅ Manual "Submit to Portal" button exists in UI
- ❌ No actual API integration with MyInvois
- ❌ No automatic submission
- ❌ Manual intervention required for every invoice

**Target State:**
- ✅ Automatic submission when invoice status = "Sent"
- ✅ Background job processing for submissions
- ✅ Status polling to track submission progress
- ✅ Automatic retry on failures
- ✅ Simple UI showing submission status
- ✅ Minimal finance team intervention

---

## 2. Overview

### 2.1 Integration Architecture

```
Invoice Status Change → "Sent"
    ↓
Workflow Engine / Event Handler
    ↓
Background Job Queue
    ↓
MyInvois API Service
    ↓
Submit Invoice (JSON/XML)
    ↓
Store Submission ID & Status
    ↓
Poll Status (Background)
    ↓
Update Invoice Status
```

### 2.2 Key Components

1. **MyInvois API Provider** - Infrastructure service to call MyInvois API
2. **E-Invoice Service** - Application service to handle submission logic
3. **Background Job Processor** - Process submissions asynchronously
4. **Status Polling Service** - Check submission status periodically
5. **Configuration Management** - Store API credentials securely
6. **Error Handling & Retry Logic** - Handle failures gracefully

---

## 3. MyInvois API Integration

### 3.1 API Endpoints (IRBM MyInvois)

**Base URLs:**
- **Sandbox:** `https://sandbox-api.myinvois.hasil.gov.my`
- **Production:** `https://api.myinvois.hasil.gov.my`

**Key Endpoints:**
1. **Submit Invoice:** `POST /api/v1.0/invoices`
2. **Get Submission Status:** `GET /api/v1.0/submissions/{submissionId}`
3. **Cancel Invoice:** `POST /api/v1.0/invoices/{invoiceId}/cancel`
4. **Get Invoice Status:** `GET /api/v1.0/invoices/{invoiceId}`

**Authentication:**
- OAuth 2.0 Client Credentials flow
- Access token required for all API calls
- Token expires (typically 1 hour)
- Auto-refresh token before expiry

**Documentation:**
- IRBM SDK: `https://dev-sdk.myinvois.hasil.gov.my`
- API Guidelines: `https://www.hasil.gov.my/media/fzagbaj2/irbm-e-invoice-guideline.pdf`

### 3.2 Invoice Data Format

**Required Fields (JSON):**
```json
{
  "invoiceNumber": "INV-2025-001",
  "invoiceDate": "2025-12-08",
  "supplier": {
    "tin": "123456789012",
    "name": "Cephas Sdn. Bhd.",
    "address": "...",
    "postcode": "50000",
    "city": "Kuala Lumpur",
    "state": "WP",
    "country": "MY"
  },
  "buyer": {
    "tin": "987654321098",
    "name": "Partner Company",
    "address": "...",
    "postcode": "50000",
    "city": "Kuala Lumpur",
    "state": "WP",
    "country": "MY"
  },
  "lineItems": [
    {
      "description": "Service Charge",
      "quantity": 1,
      "unitPrice": 1000.00,
      "taxAmount": 60.00,
      "totalAmount": 1060.00
    }
  ],
  "subTotal": 1000.00,
  "taxAmount": 60.00,
  "totalAmount": 1060.00,
  "currency": "MYR"
}
```

**Digital Signature:**
- All invoices must be digitally signed
- Use IRBM-issued digital certificate
- Signature included in API request

---

## 4. Backend Architecture

### 4.1 Domain Layer

**No new entities required** - Use existing:
- `Invoice` entity (already has `SubmissionId`, `SubmittedAt`)
- `InvoiceSubmissionHistory` entity (tracks all submissions)

### 4.2 Application Layer

**New Service Interface:**
- `IEInvoiceProvider.cs` - Interface for MyInvois API integration
- `IEInvoiceService.cs` - Application service for e-invoice operations

**Service Methods:**
```csharp
// IEInvoiceProvider (Infrastructure)
Task<MyInvoisSubmissionResponse> SubmitInvoiceAsync(MyInvoisInvoiceDto invoice, CancellationToken cancellationToken);
Task<MyInvoisStatusResponse> GetSubmissionStatusAsync(string submissionId, CancellationToken cancellationToken);
Task<string> GetAccessTokenAsync(CancellationToken cancellationToken); // OAuth token

// IEInvoiceService (Application)
Task<EInvoiceSubmissionResult> SubmitInvoiceToMyInvoisAsync(Guid invoiceId, Guid userId, CancellationToken cancellationToken);
Task<EInvoiceStatusResult> CheckSubmissionStatusAsync(Guid invoiceId, CancellationToken cancellationToken);
Task<EInvoiceSubmissionResult> RetrySubmissionAsync(Guid invoiceId, Guid userId, CancellationToken cancellationToken);
```

**DTOs:**
- `MyInvoisInvoiceDto.cs` - Invoice data for MyInvois API
- `MyInvoisSubmissionResponse.cs` - API response
- `MyInvoisStatusResponse.cs` - Status check response
- `EInvoiceSubmissionResult.cs` - Application result
- `EInvoiceStatusResult.cs` - Status result

### 4.3 Infrastructure Layer

**MyInvois API Client:**
- `MyInvoisApiClient.cs` - HTTP client for MyInvois API
- `MyInvoisApiProvider.cs` - Implements `IEInvoiceProvider`
- Handles OAuth authentication
- Handles request/response serialization
- Handles error responses

**Configuration:**
- Store API credentials in Global Settings or environment variables:
  - `MyInvois_ClientId`
  - `MyInvois_ClientSecret`
  - `MyInvois_Environment` (Sandbox/Production)
  - `MyInvois_CompanyTin` (Company TIN number)
  - `MyInvois_DigitalCertificatePath` (Path to certificate file)
  - `MyInvois_DigitalCertificatePassword`

### 4.4 API Layer

**New Controller:**
- `EInvoiceController.cs` - REST API endpoints

**Endpoints:**
```
POST   /api/billing/einvoice/submit/{invoiceId}        → Submit invoice to MyInvois
GET    /api/billing/einvoice/status/{invoiceId}         → Get submission status
POST   /api/billing/einvoice/retry/{invoiceId}          → Retry failed submission
GET    /api/billing/einvoice/pending                     → Get pending submissions
POST   /api/billing/einvoice/batch-submit                → Batch submit multiple invoices
```

---

## 5. Automatic Submission Flow

### 5.1 Trigger: Invoice Status Change

**When invoice status changes to "Sent":**
1. Workflow Engine or Event Handler detects status change
2. Creates background job: `SubmitInvoiceToMyInvoisJob`
3. Job is queued for processing
4. Background processor picks up job
5. Calls `IEInvoiceService.SubmitInvoiceToMyInvoisAsync()`

### 5.2 Submission Process

**Step-by-Step:**
1. **Validate Invoice:**
   - Invoice must be in "Sent" status
   - Invoice must not already be submitted
   - All required fields must be present
   - Company TIN must be configured

2. **Transform Invoice Data:**
   - Convert `Invoice` entity to `MyInvoisInvoiceDto`
   - Map line items
   - Calculate totals
   - Add company/supplier details

3. **Get Access Token:**
   - Call OAuth endpoint to get access token
   - Cache token (expires in 1 hour)
   - Refresh if expired

4. **Sign Invoice:**
   - Load digital certificate
   - Sign invoice data
   - Attach signature to request

5. **Submit to MyInvois:**
   - POST to `/api/v1.0/invoices`
   - Include signed invoice data
   - Handle API response

6. **Store Submission:**
   - Save `SubmissionId` from response
   - Create `InvoiceSubmissionHistory` record
   - Update `Invoice.SubmissionId` and `Invoice.SubmittedAt`
   - Set invoice status to "SubmittedToPortal"

7. **Schedule Status Check:**
   - Create background job to check status in 5 minutes
   - Poll until status is "Accepted" or "Rejected"

### 5.3 Status Polling

**Background Job: `CheckEInvoiceStatusJob`**

**Process:**
1. Get `SubmissionId` from `InvoiceSubmissionHistory`
2. Call MyInvois API: `GET /api/v1.0/submissions/{submissionId}`
3. Update `InvoiceSubmissionHistory.Status`:
   - `Pending` → Still processing
   - `Accepted` → Successfully submitted
   - `Rejected` → Submission failed
4. If `Pending`, schedule another check in 5 minutes (max 10 retries)
5. If `Accepted`, update invoice status
6. If `Rejected`, log error and notify finance team

---

## 6. Error Handling & Retry Logic

### 6.1 Error Types

**API Errors:**
- **400 Bad Request** - Invalid invoice data
- **401 Unauthorized** - Token expired or invalid
- **403 Forbidden** - Insufficient permissions
- **429 Too Many Requests** - Rate limit exceeded
- **500 Internal Server Error** - MyInvois server error

**Business Errors:**
- Missing required fields
- Invalid TIN number
- Digital certificate expired
- Invoice already submitted

### 6.2 Retry Strategy

**Automatic Retries:**
- **Transient Errors** (500, 429, network errors):
  - Retry immediately: 3 times
  - Exponential backoff: 1min, 2min, 5min
  - Max retries: 3

- **Permanent Errors** (400, 401, 403):
  - No automatic retry
  - Log error
  - Notify finance team
  - Mark submission as "Failed"

**Manual Retry:**
- Finance team can click "Retry Submission" button
- System will attempt submission again
- Previous submission history is preserved

### 6.3 Error Notifications

**When submission fails:**
1. Log error with full details
2. Update `InvoiceSubmissionHistory.Status` = "Failed"
3. Store error message in `InvoiceSubmissionHistory.ResponseMessage`
4. Send notification to finance team (email/in-app)
5. Show error badge in invoice list UI

---

## 7. Configuration Management

### 7.1 Global Settings

**New Settings Keys:**
```
MyInvois_Enabled (Bool) - Enable/disable automatic submission
MyInvois_ClientId (String) - OAuth client ID
MyInvois_ClientSecret (String) - OAuth client secret (encrypted)
MyInvois_Environment (String) - "Sandbox" or "Production"
MyInvois_CompanyTin (String) - Company TIN number
MyInvois_DigitalCertificatePath (String) - Path to certificate file
MyInvois_DigitalCertificatePassword (String) - Certificate password (encrypted)
MyInvois_AutoSubmitEnabled (Bool) - Auto-submit when status = "Sent"
MyInvois_StatusPollingIntervalMinutes (Int) - How often to check status (default: 5)
MyInvois_MaxRetryAttempts (Int) - Max retries for failed submissions (default: 3)
```

### 7.2 Security

**Credential Storage:**
- Store sensitive data (ClientSecret, CertificatePassword) encrypted
- Use .NET user-secrets or Azure Key Vault for production
- Never log sensitive credentials

**Certificate Management:**
- Store certificate file in secure location
- Use file system or Azure Key Vault
- Certificate must be IRBM-issued

---

## 8. Frontend UI Simplification

### 8.1 Invoice List Page

**Enhanced Status Display:**
- Show MyInvois submission status badge:
  - ✅ **Submitted** - Green badge
  - ⏳ **Pending** - Yellow badge (checking status)
  - ❌ **Failed** - Red badge (click to retry)
  - ⚠️ **Not Submitted** - Gray badge

**Quick Actions:**
- "Submit to MyInvois" button (only if not submitted)
- "Check Status" button (if submitted)
- "Retry Submission" button (if failed)

### 8.2 Invoice Detail Page

**MyInvois Submission Card:**
```
┌─────────────────────────────────────┐
│ 📄 MyInvois e-Invoice Submission    │
├─────────────────────────────────────┤
│ Status: ✅ Submitted                │
│ Submission ID: SUB-2025-123456      │
│ Submitted At: 08 Dec 2025, 10:30 AM│
│                                     │
│ [Check Status] [View in MyInvois]   │
└─────────────────────────────────────┘
```

**Status States:**
- **Not Submitted:** Show "Submit to MyInvois" button
- **Submitted (Pending):** Show status and "Check Status" button
- **Submitted (Accepted):** Show success message and submission ID
- **Submitted (Failed):** Show error message and "Retry" button

### 8.3 Batch Operations

**Bulk Submit:**
- Select multiple invoices in list
- Click "Submit Selected to MyInvois"
- System processes in background
- Show progress indicator
- Notify when complete

---

## 9. Background Jobs

### 9.1 Job Types

**1. SubmitInvoiceToMyInvoisJob**
- Triggered when invoice status = "Sent"
- Processes single invoice submission
- Retries on failure

**2. CheckEInvoiceStatusJob**
- Polls MyInvois API for submission status
- Updates invoice and submission history
- Schedules next check if still pending

**3. BatchSubmitInvoicesJob**
- Processes multiple invoices
- Submits in batches (e.g., 10 at a time)
- Handles rate limiting

### 9.2 Job Scheduling

**Using Hangfire or Quartz.NET:**
- Immediate jobs for submissions
- Scheduled jobs for status polling
- Recurring jobs for cleanup/retry

---

## 10. Implementation Phases

### Phase 1: MVP - Basic Integration (Week 1)

**Backend:**
- ✅ Create `IEInvoiceProvider` interface
- ✅ Implement `MyInvoisApiProvider` (basic API calls)
- ✅ Create `IEInvoiceService` and implementation
- ✅ Add OAuth token management
- ✅ Create `EInvoiceController` with submit endpoint
- ✅ Add configuration settings

**Frontend:**
- ✅ Update invoice detail page with submission status
- ✅ Add "Submit to MyInvois" button
- ✅ Show submission status badge

**Testing:**
- ✅ Test with MyInvois Sandbox
- ✅ Test OAuth authentication
- ✅ Test invoice submission
- ✅ Test error handling

**Timeline:** 3-5 days

---

### Phase 2: Automatic Submission (Week 2)

**Backend:**
- ✅ Add workflow event handler for invoice status change
- ✅ Create background job for automatic submission
- ✅ Implement retry logic
- ✅ Add status polling job

**Frontend:**
- ✅ Remove manual "Submit" button (or make optional)
- ✅ Show automatic submission status
- ✅ Add "Retry" button for failed submissions

**Testing:**
- ✅ Test automatic submission on status change
- ✅ Test status polling
- ✅ Test retry logic

**Timeline:** 3-5 days

---

### Phase 3: Enhanced Features (Week 3)

**Backend:**
- 🔄 Batch submission support
- 🔄 Enhanced error notifications
- 🔄 Submission analytics/reporting
- 🔄 Digital certificate management UI

**Frontend:**
- 🔄 Bulk submit multiple invoices
- 🔄 Submission history timeline
- 🔄 Error details modal
- 🔄 Configuration page for MyInvois settings

**Testing:**
- 🔄 Test batch operations
- 🔄 Test all error scenarios
- 🔄 Performance testing

**Timeline:** 3-5 days

---

## 11. Testing Checklist

### Backend Testing:
- [ ] OAuth token retrieval
- [ ] Token refresh before expiry
- [ ] Invoice data transformation
- [ ] Digital signature generation
- [ ] API submission (Sandbox)
- [ ] API submission (Production)
- [ ] Status polling
- [ ] Error handling (all error types)
- [ ] Retry logic
- [ ] Background job processing
- [ ] Configuration management

### Frontend Testing:
- [ ] Submission status display
- [ ] Submit button functionality
- [ ] Status check button
- [ ] Retry button
- [ ] Error message display
- [ ] Batch submit functionality
- [ ] Loading states

### Integration Testing:
- [ ] End-to-end: Invoice "Sent" → Auto-submit → Status update
- [ ] End-to-end: Failed submission → Retry → Success
- [ ] End-to-end: Status polling → Status update
- [ ] Error scenarios: Invalid data, expired token, network failure

### Production Readiness:
- [ ] Certificate management
- [ ] Credential security
- [ ] Logging and monitoring
- [ ] Rate limiting compliance
- [ ] Performance under load

---

## 12. Finance Team Workflow (Simplified)

### Before Integration:
1. Create invoice
2. Mark as "Sent"
3. **Manually log into MyInvois portal**
4. **Manually enter invoice data**
5. **Manually submit**
6. **Manually check status**
7. **Manually update CephasOps**

### After Integration:
1. Create invoice
2. Mark as "Sent"
3. ✅ **System automatically submits to MyInvois**
4. ✅ **System automatically checks status**
5. ✅ **System automatically updates invoice**
6. Finance team only needs to review if there's an error

**Intervention Only Required:**
- If submission fails (system will notify)
- To retry failed submissions (one click)
- To review submission history

---

## 13. Security & Compliance

### 13.1 Data Security
- Encrypt sensitive credentials
- Secure certificate storage
- HTTPS for all API calls
- No sensitive data in logs

### 13.2 Compliance
- Follow IRBM e-Invoice guidelines
- Maintain audit trail (InvoiceSubmissionHistory)
- Digital signature compliance
- Data retention policies

### 13.3 Error Handling
- Never expose API credentials in errors
- Log errors securely
- Notify finance team of failures
- Maintain submission history for audit

---

## 14. Monitoring & Alerts

### 14.1 Metrics to Track
- Submission success rate
- Average submission time
- Failed submission count
- Status polling frequency
- API error rates

### 14.2 Alerts
- High failure rate (> 10% failures)
- API rate limit warnings
- Certificate expiry warnings (30 days before)
- Token refresh failures

---

## 15. Future Enhancements

### 15.1 Advanced Features
- 🔄 Credit note submission
- 🔄 Debit note submission
- 🔄 Invoice cancellation via API
- 🔄 Real-time webhook notifications from MyInvois
- 🔄 Multi-company support (different TINs)

### 15.2 Integration Improvements
- 🔄 Pre-submission validation
- 🔄 Invoice template mapping
- 🔄 Automatic tax calculation
- 🔄 Multi-currency support

---

## 16. Related Documentation

- **Billing Module:** `docs/02_modules/billing/OVERVIEW.md`
- **Invoice Submission:** `backend/src/CephasOps.Application/Billing/Services/InvoiceSubmissionService.cs`
- **MyInvois SDK:** `https://dev-sdk.myinvois.hasil.gov.my`
- **IRBM Guidelines:** `https://www.hasil.gov.my/media/fzagbaj2/irbm-e-invoice-guideline.pdf`

---

## 17. Implementation Status

| Component | Status | Completeness |
|-----------|--------|--------------|
| Specification | ✅ Complete | 100% |
| MyInvois API Client | ⏳ Pending | 0% |
| E-Invoice Service | ⏳ Pending | 0% |
| Automatic Submission | ⏳ Pending | 0% |
| Status Polling | ⏳ Pending | 0% |
| Frontend UI | ⏳ Pending | 0% |
| Configuration | ⏳ Pending | 0% |
| Testing | ⏳ Pending | 0% |

**Overall Status:** 📋 Specification Complete - Ready for Implementation

---

## 18. Next Steps

1. **Obtain MyInvois Credentials:**
   - Register with IRBM for API access
   - Obtain OAuth Client ID and Secret
   - Obtain digital certificate from IRBM
   - Get Sandbox access for testing

2. **Review IRBM Documentation:**
   - Study e-Invoice Guideline (Version 4.5)
   - Review SDK documentation
   - Understand data format requirements
   - Understand digital signature requirements

3. **Begin Implementation:**
   - Start with Phase 1 (MVP)
   - Test with Sandbox environment
   - Iterate based on testing results
   - Move to Production after validation

4. **Finance Team Training:**
   - Train on new automated workflow
   - Explain error handling
   - Show how to retry failed submissions
   - Document new processes

---

## 19. Questions & Decisions Needed

1. **Digital Certificate:**
   - Do you already have IRBM-issued digital certificate?
   - Where should certificate be stored? (File system, Azure Key Vault, etc.)

2. **MyInvois Access:**
   - Do you have MyInvois API credentials?
   - Sandbox or Production environment?
   - Which company TIN numbers need to be configured?

3. **Auto-Submit Behavior:**
   - Should ALL invoices auto-submit when "Sent"?
   - Or only invoices above certain amount?
   - Or configurable per company?

4. **Error Handling:**
   - Who should be notified on submission failures?
   - Email notifications or in-app only?
   - Escalation process for repeated failures?

5. **Batch Processing:**
   - How many invoices can be submitted per batch?
   - What's the rate limit from MyInvois?
   - Should batch submission be scheduled (e.g., daily at 9 AM)?

---

**Document Version:** 1.0  
**Last Updated:** December 8, 2025  
**Author:** CephasOps Development Team  
**Status:** 📋 Ready for Implementation  
**Priority:** 🔴 High (Government Compliance)

