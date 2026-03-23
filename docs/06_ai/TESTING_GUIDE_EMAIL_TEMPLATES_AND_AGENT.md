# 🧪 Testing Guide: Email Templates & Agent Mode

**Date:** December 2025  
**Purpose:** Complete testing guide for TODO 1 (Email Templates) and Agent Mode features

---

## 📋 Pre-Testing Checklist

### 1. Database Migration
- [ ] Apply migration: `backend/migrations/20241201_AddEmailTemplates.sql`
- [ ] Verify EmailTemplates table created
- [ ] Verify 3 initial templates inserted
- [ ] Verify EmailMessages table has Direction and SentAt columns

### 2. Email Account Configuration
- [ ] At least one email account configured in system
- [ ] SMTP settings configured (Host, Port, Username, Password)
- [ ] SMTP connection tested successfully
- [ ] Email account is Active

### 3. Backend Running
- [ ] Backend API running on `http://localhost:5000` (or configured port)
- [ ] Database connection working
- [ ] All services registered in DI

### 4. Frontend Running
- [ ] Frontend running on `http://localhost:5173` (or configured port)
- [ ] User logged in with appropriate permissions
- [ ] Email Management page accessible

---

## 🧪 Test 1: Verify Database Migration

### Step 1: Apply Migration
```sql
-- Connect to PostgreSQL database
\i backend/migrations/20241201_AddEmailTemplates.sql
```

### Step 2: Verify Tables
```sql
-- Check EmailTemplates table exists
SELECT table_name 
FROM information_schema.tables 
WHERE table_name = 'EmailTemplates';

-- Check EmailMessages has new columns
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'EmailMessages' 
AND column_name IN ('Direction', 'SentAt');
```

### Step 3: Verify Templates Inserted
```sql
-- Check 3 initial templates
SELECT "Code", "Name", "IsActive", "AutoProcessReplies"
FROM "EmailTemplates"
ORDER BY "Priority" DESC;
```

**Expected Result:**
- 3 templates: `RESCHEDULE_TIME_ONLY`, `RESCHEDULE_DATE_TIME`, `ASSURANCE_CABLE_REPULL`
- All marked as `IsActive = true`
- `RESCHEDULE_DATE_TIME` and `ASSURANCE_CABLE_REPULL` have `AutoProcessReplies = true`

---

## 🧪 Test 2: Email Templates API

### Test 2.1: Get All Templates
```http
GET http://localhost:5000/api/email-templates
Authorization: Bearer {token}
```

**Expected:** List of all email templates with details

### Test 2.2: Get Template by Code
```http
GET http://localhost:5000/api/email-templates/by-code/RESCHEDULE_TIME_ONLY
Authorization: Bearer {token}
```

**Expected:** Single template object with full details

### Test 2.3: Get Templates by Entity Type
```http
GET http://localhost:5000/api/email-templates/by-entity-type/Order
Authorization: Bearer {token}
```

**Expected:** List of active templates for Order entity type

---

## 🧪 Test 3: Send Email Directly (No Template)

### Test 3.1: Send Simple Email
```http
POST http://localhost:5000/api/email-sending/send
Authorization: Bearer {token}
Content-Type: multipart/form-data

emailAccountId: {your-email-account-id}
to: test@example.com
subject: Test Email from CephasOps
body: <html><body><h1>Test Email</h1><p>This is a test email.</p></body></html>
```

**Expected Response:**
```json
{
  "success": true,
  "emailAccountId": "...",
  "emailMessageId": "...",
  "messageId": "..."
}
```

### Test 3.2: Verify Email in Sent Folder
```http
GET http://localhost:5000/api/emails?direction=Outbound
Authorization: Bearer {token}
```

**Expected:** Email appears in list with `Direction: "Outbound"` and `SentAt` timestamp

---

## 🧪 Test 4: Send Email with Template

### Test 4.1: Get Template ID
First, get the template ID:
```http
GET http://localhost:5000/api/email-templates/by-code/RESCHEDULE_TIME_ONLY
Authorization: Bearer {token}
```

### Test 4.2: Send Email with Template
```http
POST http://localhost:5000/api/email-sending/send-with-template
Authorization: Bearer {token}
Content-Type: multipart/form-data

templateId: {template-id-from-step-1}
to: customer@example.com
placeholdersJson: {"CustomerName":"John Doe","OrderNumber":"ORD-12345","OldDate":"15 Dec 2025","OldTime":"09:00 - 12:00","NewTime":"14:00 - 17:00","Reason":"Customer requested"}
```

**Expected:** Email sent with template placeholders replaced

---

## 🧪 Test 5: Send Reschedule Request

### Test 5.1: Create Test Order
First, ensure you have an order in the system (or use existing order ID).

### Test 5.2: Send Reschedule Request (Time Only)
```http
POST http://localhost:5000/api/email-sending/reschedule-request
Authorization: Bearer {token}
Content-Type: application/json

{
  "orderId": "{your-order-id}",
  "newDate": "2025-12-15T00:00:00Z",
  "newWindowFrom": "14:00:00",
  "newWindowTo": "17:00:00",
  "reason": "Customer requested time change",
  "rescheduleType": "TimeOnly"
}
```

**Expected:**
- Email sent to customer
- OrderReschedule record created with Status = "Pending"
- Email linked to reschedule via ApprovalEmailId

### Test 5.3: Send Reschedule Request (Date + Time)
```http
POST http://localhost:5000/api/email-sending/reschedule-request
Authorization: Bearer {token}
Content-Type: application/json

{
  "orderId": "{your-order-id}",
  "newDate": "2025-12-20T00:00:00Z",
  "newWindowFrom": "10:00:00",
  "newWindowTo": "13:00:00",
  "reason": "Customer requested date change",
  "rescheduleType": "DateAndTime"
}
```

**Expected:**
- Email sent to TIME Internet team (or configured recipient)
- OrderReschedule record created
- Template `RESCHEDULE_DATE_TIME` used

---

## 🧪 Test 6: Email Management Page (Frontend)

### Test 6.1: Access Email Management Page
1. Navigate to `/email` in frontend
2. Verify page loads without errors
3. Check tabs: Inbox, Sent, Compose button visible

### Test 6.2: View Inbox
1. Click "Inbox" tab
2. Verify emails appear (if any exist)
3. Click on an email to view details
4. Verify email content displays correctly

### Test 6.3: View Sent Emails
1. Click "Sent" tab
2. Verify sent emails appear
3. Verify Direction = "Outbound" for sent emails
4. Click on email to view details

### Test 6.4: Compose Email
1. Click "Compose" button
2. Select email account from dropdown
3. (Optional) Select template from dropdown
4. Fill in To, Subject, Body
5. Click "Send"
6. Verify success toast appears
7. Verify email appears in "Sent" tab

### Test 6.5: Compose with Template
1. Click "Compose"
2. Select email account
3. Select template (e.g., "RESCHEDULE_TIME_ONLY")
4. Verify Subject and Body auto-filled
5. Fill in To address
6. Send email
7. Verify template placeholders are replaced

---

## 🧪 Test 7: Agent Mode - Auto-Process Email Replies

### Test 7.1: Send Approval Email (Simulate)
First, send an email using a template with `AutoProcessReplies = true`:
```http
POST http://localhost:5000/api/email-sending/reschedule-request
Authorization: Bearer {token}
Content-Type: application/json

{
  "orderId": "{order-id}",
  "newDate": "2025-12-20T00:00:00Z",
  "newWindowFrom": "10:00:00",
  "newWindowTo": "13:00:00",
  "reason": "Test",
  "rescheduleType": "DateAndTime"
}
```

### Test 7.2: Simulate Reply Email
Manually create a reply email in the database or send a test email that will be ingested:
- Subject: "Re: Reschedule Approval Request - Order {OrderNumber}"
- Body: "Approved" (matches ReplyPattern)
- From: TIME team email address

### Test 7.3: Process Reply
```http
POST http://localhost:5000/api/agent/process-email-reply/{email-message-id}
Authorization: Bearer {token}
```

**Expected:**
- Reschedule status updated to "Approved"
- Order appointment date/time updated
- Order status changed from "ReschedulePendingApproval" to "Assigned" (if applicable)

---

## 🧪 Test 8: Agent Mode - Payment Rejection

### Test 8.1: Create Test Payment and Invoice
Ensure you have:
- An invoice with Status = "SubmittedToPortal"
- A payment linked to that invoice
- Order linked to that invoice

### Test 8.2: Handle Payment Rejection
```http
POST http://localhost:5000/api/agent/handle-payment-rejection
Authorization: Bearer {token}
Content-Type: application/json

{
  "paymentId": "{payment-id}",
  "invoiceId": "{invoice-id}",
  "rejectionReason": "Payment rejected by bank - insufficient funds",
  "rejectionCode": "INSUFFICIENT_FUNDS",
  "submissionId": "SUB-12345"
}
```

**Expected:**
- Invoice status → "Rejected"
- Order status → "Reinvoice"
- OrderStatusLog entry created
- WorkflowJob created for audit
- SubmissionId preserved

### Test 8.3: Verify Reinvoice Status
```http
GET http://localhost:5000/api/order-statuses
Authorization: Bearer {token}
```

**Expected:** "Reinvoice" status appears in list (Order 14)

---

## 🧪 Test 9: Agent Mode - Intelligent Order Routing

### Test 9.1: Route Order
```http
POST http://localhost:5000/api/agent/route-order/{order-id}
Authorization: Bearer {token}
```

**Expected Response:**
```json
{
  "success": true,
  "orderId": "...",
  "recommendedDepartmentId": "...",
  "recommendedSiId": "...",
  "routingReason": "Location: Kuala Lumpur, Partner: TIME Internet",
  "confidenceScore": 0.8,
  "routingFactors": {
    "PartnerId": "...",
    "LocationMatch": "Kuala Lumpur",
    "OrderType": "ACTIVATION"
  }
}
```

---

## 🧪 Test 10: Agent Mode - Smart KPI Calculations

### Test 10.1: Calculate KPIs (All Departments)
```http
GET http://localhost:5000/api/agent/calculate-kpis?fromDate=2025-11-01&toDate=2025-12-01
Authorization: Bearer {token}
```

**Expected Response:**
```json
{
  "success": true,
  "fromDate": "2025-11-01T00:00:00Z",
  "toDate": "2025-12-01T00:00:00Z",
  "metrics": {
    "TotalOrders": 150,
    "CompletedOrders": 120,
    "InProgressOrders": 20,
    "CancelledOrders": 10,
    "CompletionRate": 80.0,
    "AvgCompletionDays": 5.5,
    "RescheduleCount": 25,
    "OrdersWithReschedules": 20,
    "KpiBreachedOrders": 5
  },
  "insights": {
    "LowCompletionRate": "Completion rate is below 80%. Review workflow bottlenecks."
  }
}
```

### Test 10.2: Calculate KPIs (Specific Department)
```http
GET http://localhost:5000/api/agent/calculate-kpis?departmentId={dept-id}&fromDate=2025-11-01&toDate=2025-12-01
Authorization: Bearer {token}
```

**Expected:** Same structure but filtered by department

---

## 🧪 Test 11: Agent Mode - Auto-Approve Reschedule

### Test 11.1: Create Same-Day Time Change Reschedule
Create a reschedule with:
- Same date (OriginalDate.Date == NewDate.Date)
- Different time only
- Status = "Pending"

### Test 11.2: Auto-Approve
```http
POST http://localhost:5000/api/agent/auto-approve-reschedule/{reschedule-id}
Authorization: Bearer {token}
```

**Expected:**
- Reschedule status → "Approved"
- Order appointment time updated
- Order status updated (if was "ReschedulePendingApproval")

### Test 11.3: Try Auto-Approve Date Change (Should Fail)
Create a reschedule with date change, then try auto-approve:
```http
POST http://localhost:5000/api/agent/auto-approve-reschedule/{reschedule-id}
Authorization: Bearer {token}
```

**Expected:**
- `success: false`
- `errorMessage: "Reschedule requires manual approval (date change or complex scenario)"`

---

## 🧪 Test 12: Agent Mode - Process Pending Tasks

### Test 12.1: Process All Pending Tasks
```http
POST http://localhost:5000/api/agent/process-pending-tasks
Authorization: Bearer {token}
```

**Expected Response:**
```json
[
  {
    "success": true,
    "action": "EmailReplyProcessed",
    "entityId": "...",
    "entityType": "EmailMessage"
  },
  {
    "success": true,
    "action": "RescheduleAutoApproved",
    "entityId": "...",
    "entityType": "OrderReschedule"
  }
]
```

**Expected Behavior:**
- Processes up to 10 pending email replies
- Processes up to 10 pending reschedules (same-day time changes)
- Returns list of processing results

---

## 🧪 Test 13: Email Inbox Polling

### Test 13.1: Poll Email Account
```http
POST http://localhost:5000/api/email-accounts/{account-id}/poll
Authorization: Bearer {token}
```

**Expected:**
- Fetches new emails from mailbox
- Creates EmailMessage records
- Processes attachments (Excel files)
- Creates ParseSessions

### Test 13.2: Verify Inbox Updated
```http
GET http://localhost:5000/api/emails?direction=Inbound
Authorization: Bearer {token}
```

**Expected:** New emails appear in inbox

---

## 📊 Test Results Template

Use this template to track your test results:

```
Test 1: Database Migration
[ ] Migration applied successfully
[ ] Tables created
[ ] Templates inserted

Test 2: Email Templates API
[ ] Get all templates
[ ] Get template by code
[ ] Get templates by entity type

Test 3: Send Email Directly
[ ] Email sent successfully
[ ] Email appears in Sent folder

Test 4: Send Email with Template
[ ] Template loaded
[ ] Placeholders replaced
[ ] Email sent

Test 5: Send Reschedule Request
[ ] Time-only reschedule sent
[ ] Date+time reschedule sent
[ ] OrderReschedule created

Test 6: Email Management Page
[ ] Page loads
[ ] Inbox displays
[ ] Sent displays
[ ] Compose works
[ ] Template selection works

Test 7: Agent - Email Reply Processing
[ ] Reply detected
[ ] Approval pattern matched
[ ] Reschedule auto-approved

Test 8: Agent - Payment Rejection
[ ] Payment rejected
[ ] Order → Reinvoice
[ ] SubmissionId preserved

Test 9: Agent - Order Routing
[ ] Routing recommendation generated
[ ] Confidence score calculated

Test 10: Agent - KPI Calculations
[ ] KPIs calculated
[ ] Insights generated

Test 11: Agent - Auto-Approve Reschedule
[ ] Same-day time change approved
[ ] Date change rejected (correctly)

Test 12: Agent - Process Pending Tasks
[ ] Batch processing works
[ ] Multiple tasks processed

Test 13: Email Polling
[ ] Emails fetched
[ ] Inbox updated
```

---

## 🐛 Troubleshooting

### Issue: Migration fails
**Solution:** Check PostgreSQL connection and permissions

### Issue: Email sending fails
**Solution:** 
- Verify SMTP settings
- Test SMTP connection via Email Accounts page
- Check firewall/network settings

### Issue: Templates not found
**Solution:**
- Verify migration applied
- Check EmailTemplates table has data
- Verify template codes match exactly

### Issue: Agent processing fails
**Solution:**
- Check email reply format matches expected pattern
- Verify OrderReschedule exists and is in "Pending" status
- Check logs for detailed error messages

---

## ✅ Success Criteria

All tests pass when:
1. ✅ Migration applies without errors
2. ✅ All API endpoints return expected responses
3. ✅ Emails send successfully via SMTP
4. ✅ Templates render with placeholders replaced
5. ✅ Email Management page displays correctly
6. ✅ Agent Mode processes replies and rejections
7. ✅ Order routing provides recommendations
8. ✅ KPI calculations return accurate metrics
9. ✅ Auto-approval works for eligible reschedules

---

**Ready to test!** Follow the tests in order, or jump to specific features you want to verify first.

