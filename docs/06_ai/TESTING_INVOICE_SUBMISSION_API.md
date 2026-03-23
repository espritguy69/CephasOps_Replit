# 🧪 Testing Invoice Submission API

**Date:** December 2025  
**Purpose:** Test the InvoiceSubmissionHistory API endpoints

---

## 📋 API Endpoints Created

### 1. Get Submission History
```http
GET /api/invoices/{invoiceId}/submission-history
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "id": "...",
    "invoiceId": "...",
    "submissionId": "SUB-001",
    "submittedAt": "2025-12-06T10:00:00Z",
    "status": "Submitted",
    "portalType": "MyInvois",
    "isActive": true,
    "paymentStatus": null,
    "createdAt": "2025-12-06T10:00:00Z"
  }
]
```

---

### 2. Get Active Submission
```http
GET /api/invoices/{invoiceId}/submission-history/active
Authorization: Bearer {token}
```

**Response:**
```json
{
  "id": "...",
  "invoiceId": "...",
  "submissionId": "SUB-001",
  "submittedAt": "2025-12-06T10:00:00Z",
  "status": "Submitted",
  "isActive": true
}
```

---

### 3. Record Submission
```http
POST /api/invoices/{invoiceId}/submission-history
Authorization: Bearer {token}
Content-Type: application/json

{
  "submissionId": "SUB-001",
  "portalType": "MyInvois",
  "responseMessage": "Submission successful",
  "responseCode": "200"
}
```

**Response:**
```json
{
  "id": "...",
  "invoiceId": "...",
  "submissionId": "SUB-001",
  "submittedAt": "2025-12-06T10:00:00Z",
  "status": "Submitted",
  "isActive": true
}
```

---

### 4. Update Submission Status
```http
PUT /api/invoices/submission-history/{submissionHistoryId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Rejected",
  "rejectionReason": "Payment rejected by bank",
  "paymentStatus": "Rejected",
  "paymentReference": "REF-123"
}
```

**Response:**
```json
{
  "id": "...",
  "status": "Rejected",
  "rejectionReason": "Payment rejected by bank",
  "paymentStatus": "Rejected"
}
```

---

## 🧪 Test Scenarios

### Test 1: Record First Submission

1. **Create or get an invoice ID**
2. **Record submission:**
   ```http
   POST /api/invoices/{invoiceId}/submission-history
   {
     "submissionId": "TEST-SUB-001",
     "portalType": "MyInvois"
   }
   ```
3. **Verify:**
   - Response returns 201 Created
   - SubmissionId is "TEST-SUB-001"
   - IsActive = true
   - Invoice.SubmissionId updated

---

### Test 2: Record Second Submission (Resubmission)

1. **Record first submission** (from Test 1)
2. **Record second submission:**
   ```http
   POST /api/invoices/{invoiceId}/submission-history
   {
     "submissionId": "TEST-SUB-002",
     "portalType": "MyInvois"
   }
   ```
3. **Verify:**
   - First submission (SUB-001) has IsActive = false
   - Second submission (SUB-002) has IsActive = true
   - Both records exist in history

**Check via SQL:**
```sql
SELECT "SubmissionId", "IsActive", "SubmittedAt"
FROM "InvoiceSubmissionHistory"
WHERE "InvoiceId" = '{invoiceId}'
ORDER BY "SubmittedAt" DESC;
```

---

### Test 3: Get Submission History

1. **Get all submissions:**
   ```http
   GET /api/invoices/{invoiceId}/submission-history
   ```
2. **Verify:**
   - Returns list of all submissions
   - Ordered by SubmittedAt (newest first)
   - Includes all fields

---

### Test 4: Get Active Submission

1. **Get active submission:**
   ```http
   GET /api/invoices/{invoiceId}/submission-history/active
   ```
2. **Verify:**
   - Returns only active submission
   - IsActive = true
   - Returns 404 if no active submission

---

### Test 5: Update Submission Status (Payment Rejection)

1. **Record submission** (from Test 1)
2. **Update status to Rejected:**
   ```http
   PUT /api/invoices/submission-history/{submissionHistoryId}
   {
     "status": "Rejected",
     "rejectionReason": "Payment rejected - insufficient funds",
     "paymentStatus": "Rejected"
   }
   ```
3. **Verify:**
   - Status = "Rejected"
   - RejectionReason populated
   - PaymentStatus = "Rejected"

---

### Test 6: Complete Payment Rejection Flow

1. **Create invoice and record submission**
2. **Reject payment via Agent Mode:**
   ```http
   POST /api/agent/handle-payment-rejection
   {
     "paymentId": "...",
     "invoiceId": "...",
     "rejectionReason": "Test rejection",
     "rejectionCode": "TEST",
     "submissionId": "TEST-SUB-001"
   }
   ```
3. **Verify:**
   - InvoiceSubmissionHistory updated (Status = "Rejected")
   - Order status → "Reinvoice"
   - OrderStatusLog created with SubmissionId in reason

---

## 🔍 Verification Queries

### Check Submission History
```sql
SELECT 
    i."InvoiceNumber",
    h."SubmissionId",
    h."Status",
    h."IsActive",
    h."SubmittedAt",
    h."RejectionReason",
    h."PaymentStatus"
FROM "InvoiceSubmissionHistory" h
JOIN "Invoices" i ON i."Id" = h."InvoiceId"
WHERE h."InvoiceId" = '{invoiceId}'
ORDER BY h."SubmittedAt" DESC;
```

### Check Active Submissions
```sql
SELECT COUNT(*) 
FROM "InvoiceSubmissionHistory" 
WHERE "IsActive" = true;
```

### Check Rejected Submissions
```sql
SELECT 
    "SubmissionId",
    "Status",
    "RejectionReason",
    "PaymentStatus"
FROM "InvoiceSubmissionHistory"
WHERE "Status" = 'Rejected'
ORDER BY "SubmittedAt" DESC;
```

---

## ✅ Success Criteria

- [ ] Can record first submission
- [ ] Can record second submission (deactivates first)
- [ ] Can get all submission history
- [ ] Can get active submission
- [ ] Can update submission status
- [ ] Payment rejection updates submission history
- [ ] Order transitions to Reinvoice on payment rejection
- [ ] Previous SubmissionIds preserved in audit trail

---

## 🐛 Troubleshooting

### Issue: 404 Not Found
**Solution:** Verify invoice ID exists

### Issue: 401 Unauthorized
**Solution:** Check JWT token is valid

### Issue: Submission not deactivating previous
**Solution:** Check InvoiceSubmissionService.RecordSubmissionAsync logic

### Issue: Payment rejection not updating submission
**Solution:** Verify AgentModeService is calling InvoiceSubmissionService

---

**Ready for testing!** Use Postman, curl, or your API client to test these endpoints.

