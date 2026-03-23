# Email Parser End-to-End Testing Guide
**Date**: December 3, 2025  
**Purpose**: Complete testing workflow for email → order creation  
**Prerequisites**: Backend and frontend services restarted

---

## 🎯 Testing Objectives

1. ✅ Verify email management UI is accessible in navigation
2. ✅ Test manual email polling ("Poll All" button)
3. ✅ Verify emails are fetched and displayed
4. ✅ Test parser extracts order data correctly
5. ✅ Test order approval from parsed draft
6. ✅ Verify order appears in Orders list and Scheduler
7. ✅ Complete full order lifecycle

---

## 📋 Pre-Test Checklist

### Backend Status
- [ ] Backend restarted successfully
- [ ] Listening on http://localhost:5000
- [ ] Database connected (Supabase PostgreSQL)
- [ ] Background worker running
- [ ] Carbone enabled
- [ ] Health check endpoint accessible

### Frontend Status
- [ ] Frontend running on http://localhost:5173
- [ ] Login successful (simon@cephas.com.my)
- [ ] Department selector showing GPON
- [ ] No console errors

### Email Configuration
- [ ] 2 mailboxes configured:
  - admin@cephas.com.my
  - simon@cephas.com.my
- [ ] 8 parser templates active
- [ ] Mail server accessible (mail.cephas.com.my)

---

## 🧪 Test Scenario 1: Email Management Navigation

### Steps:
1. Login to CephasOps
2. Look at sidebar navigation
3. Find "Email" under "Tools" section
4. Click "Email" link

### Expected Results:
- ✅ "Email" menu item visible in sidebar under "Tools"
- ✅ Clicking navigates to `/email`
- ✅ Email Management page loads without errors
- ✅ Shows "Inbox (4)" and "Sent (0)" tabs
- ✅ 4 emails displayed in inbox

### Success Criteria:
- Navigation item present
- Page loads correctly
- Emails visible

---

## 🧪 Test Scenario 2: Manual Email Polling

### Steps:
1. Navigate to `/settings/email#mailboxes`
2. Scroll down to mailbox table
3. Note current `lastPolledAt` values (should be null or old timestamp)
4. Click "Poll All" button
5. Wait for success toast notification
6. Refresh page or check mailbox table
7. Verify `lastPolledAt` is updated

### Expected Results:
- ✅ "Poll All" button visible and clickable
- ✅ Button shows loading state during polling
- ✅ Success toast: "Emails fetched successfully" or similar
- ✅ `lastPolledAt` timestamp updated for both mailboxes
- ✅ New emails appear in `/email` inbox (if any)

### API Verification:
```bash
GET http://localhost:5000/api/email-accounts
# Check lastPolledAt field is not null
```

### Success Criteria:
- Poll completes without errors
- Timestamps updated
- New emails fetched (if available)

---

## 🧪 Test Scenario 3: View Inbox Emails

### Steps:
1. Navigate to `/email`
2. Verify "Inbox" tab is active
3. Review email list
4. Click on first email (TIME partner email)
5. Review email details in right panel
6. Check for attachment indicator
7. Verify parser status badge (if present)

### Expected Results:
- ✅ Email list displays correctly
- ✅ Shows sender, subject, date
- ✅ Attachment icon visible for emails with attachments
- ✅ Clicking email shows detail panel
- ✅ Email content displayed (From, To, CC, Date, Body)
- ✅ Reply/Forward buttons visible

### Email Details to Verify:
- **Email 1**: No-Reply@time.com.my - "Modification-Outdoor relocation FTTH"
  - Has attachment: ✅
  - Date: 11/26/2025
  - Should match TIME_MOD_OUTDOOR template

### Success Criteria:
- All 4 emails visible
- Email details accessible
- UI responsive and functional

---

## 🧪 Test Scenario 4: Parser Review Queue

### Steps:
1. Navigate to `/orders/parser`
2. Check status summary at top
3. Note: "X drafts pending review • Y processed"
4. If drafts pending > 0:
   - Review draft list
   - Click on a draft to view details
5. If drafts pending = 0:
   - Check "Show processed" checkbox
   - View processed drafts

### Expected Results:
- ✅ Parser Review Queue page loads
- ✅ Status summary displays
- ✅ Draft list shows (if any pending)
- ✅ "Show processed" checkbox works
- ✅ Can view draft details

### Data to Verify:
- Total parse sessions: 41
- Pending drafts: 0 or more
- Processed drafts: Available when checkbox enabled

### Success Criteria:
- Page loads without errors
- Can view parsed drafts
- Status summary accurate

---

## 🧪 Test Scenario 5: View Parsed Draft Details

### Steps:
1. In Parser Review Queue, select a parsed draft
2. Review extracted data:
   - Customer Name
   - Customer Phone
   - Customer Email
   - Service ID (TBBN)
   - Address (Line 1, Line 2, City, State, Postcode)
   - Appointment Date & Time
   - Building
   - Partner
   - Order Type
   - Technical Details
3. Verify data accuracy against original email/attachment
4. Check for any parsing errors or missing fields

### Expected Results:
- ✅ Draft detail view displays
- ✅ All extracted fields visible
- ✅ Data appears accurate
- ✅ Can edit fields if needed
- ✅ Approve/Reject buttons visible

### Data Quality Checks:
- [ ] Customer name extracted correctly
- [ ] Phone number formatted properly
- [ ] Address complete and accurate
- [ ] Appointment date/time valid
- [ ] Service ID (TBBN) present
- [ ] Building matched (if auto-match enabled)

### Success Criteria:
- Draft details accessible
- Data extraction quality >90%
- UI allows editing before approval

---

## 🧪 Test Scenario 6: Approve Parsed Draft → Create Order

### Steps:
1. Select a pending draft in Parser Review Queue
2. Review all extracted data
3. Make any necessary edits
4. Click "Approve & Create Order" button
5. Wait for success notification
6. Navigate to `/orders`
7. Verify new order appears in list
8. Navigate to `/scheduler`
9. Verify order appears in unassigned orders

### Expected Results:
- ✅ Approval succeeds with success toast
- ✅ Draft status changes to "Approved"
- ✅ New Order created with status "Pending"
- ✅ Order visible in Orders list
- ✅ Order visible in Scheduler (unassigned)
- ✅ Order has all data from draft:
  - Customer details
  - Address
  - Appointment date
  - Service ID
  - Building reference
  - Partner reference

### API Verification:
```bash
GET http://localhost:5000/api/orders
# New order should appear with status "Pending"
```

### Success Criteria:
- Order created successfully
- All data transferred correctly
- Order appears in both Orders list and Scheduler

---

## 🧪 Test Scenario 7: Complete Order Lifecycle

### Steps:
1. **Assign Order**:
   - In Scheduler, drag order to an installer
   - Verify status changes to "Assigned"
   
2. **SI Updates Status** (simulate):
   - Use API or UI to update status:
     - On the Way
     - Met Customer
     - Order Completed
   
3. **Upload Docket**:
   - Navigate to order detail
   - Upload docket PDF
   - Verify status changes to "Docket Uploaded"
   
4. **Generate Invoice**:
   - Navigate to `/billing/invoices`
   - Create invoice for the order
   - Link order to invoice line item
   
5. **Check P&L**:
   - Navigate to `/pnl/drilldown`
   - Filter by order
   - Verify revenue and costs recorded

### Expected Results:
- ✅ Order lifecycle completes end-to-end
- ✅ All status transitions work
- ✅ Invoice generated correctly
- ✅ P&L data calculated

### Success Criteria:
- Complete workflow without errors
- Data flows through all modules
- P&L reflects order profitability

---

## 🧪 Test Scenario 8: Background Worker Health Check

### Steps:
1. Open API testing tool (Postman, curl, or browser)
2. Get fresh JWT token (login)
3. Call health check endpoint:
   ```
   GET http://localhost:5000/api/background-jobs/health
   Authorization: Bearer {token}
   ```
4. Review response

### Expected Response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-12-03T...",
  "application": {
    "isRunning": true,
    "uptime": "..."
  },
  "backgroundWorker": {
    "isRunning": true,
    "recentJobsCount": 10,
    "recentJobs": [...]
  },
  "emailPolling": {
    "activeAccountsCount": 2,
    "accounts": [
      {
        "id": "...",
        "name": "Admin CEPHAS",
        "email": "admin@cephas.com.my",
        "lastPolledAt": "2025-12-03T...",
        "minutesSinceLastPoll": 2,
        "status": "Healthy"
      },
      {
        "id": "...",
        "name": "Simon",
        "email": "simon@cephas.com.my",
        "lastPolledAt": "2025-12-03T...",
        "minutesSinceLastPoll": 2,
        "status": "Healthy"
      }
    ]
  }
}
```

### Success Criteria:
- Status: "Healthy"
- Both mailboxes showing recent poll timestamps
- Background worker running

---

## 🧪 Test Scenario 9: Parser Template Matching

### Steps:
1. Review configured parser templates:
   - Navigate to `/settings/email#parsers`
   - Note template patterns:
     - TIME_MOD_OUTDOOR: *@time.com.my + "Modification" + "Outdoor"
     - TIME_MOD_INDOOR: *@time.com.my + "Modification" + "Indoor"
     - TIME_FTTH: *@time.com.my + "FTTH" + "Activation"

2. Check email in inbox:
   - Subject: "Modification-Outdoor relocation FTTH"
   - From: No-Reply@time.com.my
   
3. Verify correct template matched:
   - Should match: TIME_MOD_OUTDOOR
   - Order type should be: "Modification"

### Expected Results:
- ✅ Parser selects correct template
- ✅ Order type assigned correctly
- ✅ Department routed correctly (GPON)

### Success Criteria:
- Template matching accuracy >95%
- Order type correct
- No parsing errors

---

## 🧪 Test Scenario 10: Email Settings Configuration

### Steps:
1. Navigate to `/settings/email`
2. Test all 4 tabs:

#### Tab 1: Email Mailboxes
- [ ] View 2 configured mailboxes
- [ ] Click "Edit" on a mailbox
- [ ] Verify all fields populated
- [ ] Click "Test" connection button
- [ ] Verify connection test succeeds
- [ ] Click "Poll" on individual mailbox
- [ ] Verify emails fetched

#### Tab 2: Email Rules
- [ ] View configured rules (if any)
- [ ] Click "Add Rule" button
- [ ] Test rule creation form
- [ ] Verify rule validation

#### Tab 3: VIP
- [ ] View VIP email list (if any)
- [ ] Click "Add VIP Email" button
- [ ] Test VIP email creation
- [ ] Verify VIP routing

#### Tab 4: Parser Templates
- [ ] View 8 parser templates
- [ ] Click on a template to view details
- [ ] Verify pattern matching rules
- [ ] Check auto-approve settings

### Success Criteria:
- All tabs functional
- CRUD operations work
- Configuration persists

---

## 📊 Success Metrics

### Email Ingestion
- ✅ Emails fetched from mailbox
- ✅ Emails stored in database
- ✅ Emails displayed in UI
- ✅ Attachments detected

### Parser Accuracy
- ✅ Template matching >95%
- ✅ Customer data extracted correctly
- ✅ Address parsed accurately
- ✅ Appointment date/time valid
- ✅ Service ID (TBBN) extracted

### Order Creation
- ✅ Draft approved successfully
- ✅ Order created with correct data
- ✅ Order appears in Orders list
- ✅ Order appears in Scheduler
- ✅ Order assigned to correct department

### End-to-End
- ✅ Email → Draft → Order → Schedule → Complete → Invoice → P&L
- ✅ No errors in any step
- ✅ Data flows correctly through all modules

---

## 🐛 Troubleshooting

### Issue 1: "Poll All" button doesn't work
**Possible Causes**:
- Backend not running
- Email server unreachable
- Invalid credentials

**Solution**:
1. Check backend logs for errors
2. Test connection on individual mailbox
3. Verify mail server credentials
4. Check firewall/network settings

### Issue 2: No emails appear after polling
**Possible Causes**:
- No new emails in mailbox
- Emails already processed
- Parser filter excluding emails

**Solution**:
1. Check mailbox directly (webmail)
2. Send test email to mailbox
3. Poll again
4. Check EmailMessage table in database

### Issue 3: Parser doesn't create drafts
**Possible Causes**:
- No matching parser template
- Email format not recognized
- Parser template inactive

**Solution**:
1. Check parser template patterns
2. Verify email matches template criteria
3. Check parser template is active
4. Review parser logs

### Issue 4: Draft approval fails
**Possible Causes**:
- Missing required fields
- Validation errors
- Building/Partner not found

**Solution**:
1. Review validation errors
2. Fill in missing required fields
3. Verify building exists in system
4. Verify partner exists in system

---

## 📝 Test Data

### Sample Email for Testing

**To**: admin@cephas.com.my  
**From**: no-reply@time.com.my  
**Subject**: FTTH Activation - Test Customer  
**Body**:
```
Service ID: M1234567
Customer Name: John Doe
Phone: +60123456789
Email: john.doe@example.com

Service Address:
123 Jalan Test
Taman Test
50000 Kuala Lumpur
Selangor

Appointment:
Date: 2025-12-05
Time: 10:00 AM - 12:00 PM

Package: 100Mbps FTTH
Installation Type: New Installation
```

**Expected Parser Result**:
- Template Matched: TIME_FTTH
- Order Type: Activation
- Customer Name: John Doe
- Phone: +60123456789
- Service ID: M1234567
- Address: 123 Jalan Test, Taman Test, 50000 Kuala Lumpur, Selangor
- Appointment: 2025-12-05 10:00

---

## 🎯 Key Areas to Test

### 1. Email Management UI
- [x] Navigation item added to sidebar
- [ ] Page loads without errors
- [ ] Inbox/Sent tabs work
- [ ] Email list displays
- [ ] Email detail panel works
- [ ] Compose email modal
- [ ] Poll inbox button
- [ ] Reply/Forward functionality

### 2. Email Settings UI
- [ ] All 4 tabs accessible
- [ ] Mailbox CRUD operations
- [ ] Parser template configuration
- [ ] VIP email management
- [ ] Email rules configuration
- [ ] Test connection feature
- [ ] Poll All functionality

### 3. Parser Functionality
- [ ] Email polling works
- [ ] Parser extracts data correctly
- [ ] Template matching accurate
- [ ] Draft creation successful
- [ ] Data quality >90%

### 4. Order Creation
- [ ] Draft approval works
- [ ] Order created correctly
- [ ] Order appears in list
- [ ] Order appears in scheduler
- [ ] All data transferred

### 5. Integration
- [ ] Order → Schedule
- [ ] Order → Complete
- [ ] Order → Invoice
- [ ] Order → P&L
- [ ] End-to-end flow complete

---

## 📊 Test Results Template

### Test Execution Log

| Scenario | Status | Notes | Issues |
|----------|--------|-------|--------|
| 1. Email Navigation | ⏳ | | |
| 2. Manual Polling | ⏳ | | |
| 3. View Inbox | ⏳ | | |
| 4. Parser Review | ⏳ | | |
| 5. Draft Details | ⏳ | | |
| 6. Approve Draft | ⏳ | | |
| 7. Order Lifecycle | ⏳ | | |
| 8. Health Check | ⏳ | | |
| 9. Template Matching | ⏳ | | |
| 10. Email Settings | ⏳ | | |

### Overall Results
- **Total Tests**: 10
- **Passed**: ___ / 10
- **Failed**: ___ / 10
- **Blocked**: ___ / 10
- **Success Rate**: ____%

---

## 🚀 Quick Start Commands

### Check Backend Health
```bash
# Get fresh token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"Email":"simon@cephas.com.my","Password":"J@saw007"}'

# Check health
curl http://localhost:5000/api/background-jobs/health \
  -H "Authorization: Bearer {token}"

# Check email accounts
curl http://localhost:5000/api/email-accounts \
  -H "Authorization: Bearer {token}"

# Check parser sessions
curl http://localhost:5000/api/parser/sessions \
  -H "Authorization: Bearer {token}"
```

### Frontend URLs
- Dashboard: http://localhost:5173/dashboard
- Email Management: http://localhost:5173/email
- Email Settings: http://localhost:5173/settings/email
- Parser Review: http://localhost:5173/orders/parser
- Orders List: http://localhost:5173/orders
- Scheduler: http://localhost:5173/scheduler

---

## ✅ Final Checklist

Before marking testing complete:

- [ ] All 10 test scenarios executed
- [ ] No critical errors found
- [ ] Email polling working
- [ ] Parser accuracy verified
- [ ] Order creation successful
- [ ] End-to-end workflow complete
- [ ] Health check endpoint working
- [ ] Documentation updated
- [ ] Issues logged (if any)
- [ ] Sign-off obtained

---

## 📞 Support

### If Issues Found:
1. Check console logs (browser F12)
2. Check backend logs (terminal output)
3. Review `EMAIL_PARSER_AUDIT.md`
4. Check `TROUBLESHOOTING.md` (if exists)
5. Contact development team

### Key Files for Reference:
- `EMAIL_PARSER_AUDIT.md` - Full audit report
- `ALL_ISSUES_FIXED.md` - Issues fixed summary
- `NEXT_STEPS.md` - Overall testing guide
- `CARBONE_CONFIGURATION.md` - Document generation setup

---

**Testing Guide Created By**: AI Assistant (Claude Sonnet 4.5)  
**Date**: December 3, 2025  
**Status**: ✅ **READY FOR TESTING**

---

## 🎉 You're All Set!

The system is ready for comprehensive end-to-end testing. After you restart the backend and frontend:

1. ✅ Login to CephasOps
2. ✅ Look for "Email" in sidebar under "Tools"
3. ✅ Start with Test Scenario 1
4. ✅ Work through all 10 scenarios
5. ✅ Report any issues found

**Good luck with testing!** 🚀

