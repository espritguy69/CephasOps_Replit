# End-to-End Test Execution Guide

This guide provides step-by-step instructions for executing the complete email-to-order assignment test flow.

## Quick Start

1. **Verify Prerequisites** (5 minutes)
   ```bash
   psql -h localhost -U postgres -d cephasops -f scripts/test/verify_test_prerequisites.sql
   ```

2. **Create Test Data** (if needed)
   ```bash
   psql -h localhost -U postgres -d cephasops -f scripts/test/create_test_data.sql
   ```

3. **Start Backend**
   ```bash
   cd backend/src/CephasOps.Api
   dotnet watch run
   ```

4. **Start Frontend**
   ```bash
   cd frontend
   npm run dev
   ```

5. **Follow Test Phases** (see below)

---

## Prerequisites Setup

### 1. Database Verification

Run the verification script:
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/verify_test_prerequisites.sql
```

**Expected Output:**
- All prerequisites should show `true` or count >= 1
- If any are missing, run `create_test_data.sql`

### 2. Email Account Configuration

#### Option A: Via API

```http
POST http://localhost:5000/api/email-accounts
Authorization: Bearer {your-token}
Content-Type: application/json

{
  "name": "Test Email Account",
  "provider": "POP3",
  "host": "mail.example.com",
  "port": 110,
  "useSsl": false,
  "username": "test@example.com",
  "password": "your-password",
  "pollIntervalSec": 60,
  "isActive": true,
  "defaultDepartmentId": "{department-guid}",
  "defaultParserTemplateId": "{parser-template-guid}"
}
```

**Response:** Save the `id` from the response.

#### Option B: Via Frontend UI

1. Navigate to **Settings → Email → Email Accounts**
2. Click **Add Email Account**
3. Fill in:
   - Name: "Test Email Account"
   - Provider: POP3 or IMAP
   - Host: Your mail server
   - Port: 110 (POP3) or 143 (IMAP)
   - Username: Your email address
   - Password: Your email password
   - Poll Interval: 60 seconds
   - Is Active: ✓
4. Click **Save**

### 3. Verify Backend Services

Check backend logs for:
```
Email ingestion scheduler service started
Background job processor service started
```

If not present, verify `Program.cs` has:
```csharp
builder.Services.AddHostedService<BackgroundJobProcessorService>();
builder.Services.AddHostedService<EmailIngestionSchedulerService>();
```

### 4. Verify Frontend Access

- Parser Review: `http://localhost:5173/orders/parser` or `/parser/review`
- Scheduler: `http://localhost:5173/scheduler` or `/scheduler/calendar`

---

## Test Execution

### Phase 1: Email Ingestion (Scheduler)

**Objective:** Verify scheduler automatically creates email ingestion jobs

**Steps:**

1. **Check Scheduler Service**
   - Open backend logs
   - Look for: `"Email ingestion scheduler service started"`
   - Should appear on backend startup

2. **Verify Email Account**
   ```http
   GET http://localhost:5000/api/email-accounts
   Authorization: Bearer {token}
   ```
   - Confirm at least one account has `isActive: true`
   - Note the `pollIntervalSec` value

3. **Monitor Background Jobs**
   ```bash
   # Run in separate terminal
   watch -n 5 'psql -h localhost -U postgres -d cephasops -c "SELECT \"Id\", \"State\", \"CreatedAt\" FROM \"BackgroundJobs\" WHERE \"JobType\" = '\''EmailIngest'\'' ORDER BY \"CreatedAt\" DESC LIMIT 5;"'
   ```
   
   Or use the monitoring script:
   ```bash
   psql -h localhost -U postgres -d cephasops -f scripts/test/check_background_jobs.sql
   ```

4. **Wait and Verify**
   - Wait 30-60 seconds
   - Check for new `EmailIngest` jobs being created
   - Jobs should transition: Queued → Running → Succeeded

5. **Verify LastPolledAt Updates**
   ```sql
   SELECT "Name", "LastPolledAt", "PollIntervalSec" 
   FROM "EmailAccounts" 
   WHERE "IsActive" = true;
   ```

**Success Criteria:**
- ✅ Scheduler creates jobs every 30 seconds (if accounts need polling)
- ✅ Jobs are processed successfully
- ✅ EmailAccount.LastPolledAt updates after successful poll

---

### Phase 2: Email Parsing

**Objective:** Verify emails are parsed into ParseSessions and ParsedOrderDrafts

**Steps:**

1. **Prepare Test Email**
   - Subject: "Test Order - M1234567"
   - From: Address matching your ParserTemplate.FromPattern
   - Attachment: Excel file (.xls or .xlsx) with:
     - Service ID (e.g., M1234567)
     - Customer name, phone, address
     - Appointment date/time
     - Package/bandwidth details

2. **Send Test Email**
   - Send to your configured email account
   - Wait for email to arrive in mailbox

3. **Trigger Email Poll** (if not automatic)
   ```http
   POST http://localhost:5000/api/email-accounts/{emailAccountId}/poll
   Authorization: Bearer {token}
   ```

4. **Verify EmailMessage Created**
   ```http
   GET http://localhost:5000/api/parser/email-messages
   Authorization: Bearer {token}
   ```
   - Find email by subject
   - Verify `ParserStatus: "Pending"`

5. **Verify ParseSession Created**
   ```http
   GET http://localhost:5000/api/parser/sessions
   Authorization: Bearer {token}
   ```
   - Find session linked to email
   - Verify `Status: "Pending"` or `"Completed"`

6. **Verify ParsedOrderDraft Created**
   ```http
   GET http://localhost:5000/api/parser/sessions/{sessionId}/drafts
   Authorization: Bearer {token}
   ```
   - Verify draft contains extracted data
   - Check `ConfidenceScore` (should be > 0.5)

**Or use monitoring script:**
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/monitor_email_flow.sql
```

**Success Criteria:**
- ✅ EmailMessage record created
- ✅ ParseSession created
- ✅ ParsedOrderDraft(s) created with extracted data
- ✅ Building auto-matched (if address matches existing building)

---

### Phase 3: Parse Review

**Objective:** Verify manual review and approval workflow

**Steps:**

1. **Navigate to Parse Review Page**
   - Frontend: `http://localhost:5173/orders/parser`
   - Verify ParseSession list displays

2. **Select ParseSession**
   - Click on session to view details
   - Verify ParsedOrderDrafts are listed

3. **Review Draft Details**
   - Click "View" or "Details" on a draft
   - Verify all extracted fields displayed

4. **Edit Draft (if needed)**
   ```http
   PUT http://localhost:5000/api/parser/drafts/{draftId}
   Authorization: Bearer {token}
   Content-Type: application/json
   
   {
     "customerName": "Updated Name",
     "customerPhone": "0123456789"
   }
   ```

5. **Approve Draft**
   - Click "Approve" button in UI
   - If building not matched, Quick Add Building modal appears
   - Create or select building if needed
   - Confirm approval

**Success Criteria:**
- ✅ ParseSession list displays correctly
- ✅ Draft details show all extracted data
- ✅ Draft can be edited and saved
- ✅ Approval creates Order
- ✅ Building resolution works

---

### Phase 4: Order Creation

**Objective:** Verify orders are created from approved drafts

**Steps:**

1. **Verify Order Created**
   ```http
   GET http://localhost:5000/api/orders/{orderId}
   Authorization: Bearer {token}
   ```
   - OrderId from approval response
   - Verify order contains all required fields

2. **Verify ParsedOrderDraft Updated**
   ```http
   GET http://localhost:5000/api/parser/drafts/{draftId}
   Authorization: Bearer {token}
   ```
   - Verify `CreatedOrderId` is set
   - Verify `ValidationStatus: "Valid"`

3. **Check Order in Orders List**
   - Frontend: `http://localhost:5173/orders`
   - Verify order appears in list
   - Verify status and appointment date

**Success Criteria:**
- ✅ Order created with all data from draft
- ✅ Order status is "Pending" or "Scheduled"
- ✅ ParsedOrderDraft linked to order
- ✅ Order visible in Orders list

---

### Phase 5: Order Assignment

**Objective:** Verify orders can be assigned to service installers

**Steps:**

1. **Navigate to Scheduler Page**
   - Frontend: `http://localhost:5173/scheduler`
   - Verify unassigned orders are displayed

2. **View Unassigned Orders**
   ```http
   GET http://localhost:5000/api/scheduler/unassigned-orders
   Authorization: Bearer {token}
   ```
   - Verify order appears in list
   - Check appointment date and time

3. **View Available Service Installers**
   ```http
   GET http://localhost:5000/api/scheduler/installers
   Authorization: Bearer {token}
   ```
   - Verify at least one installer is available

4. **Assign Order to Installer**
   - Drag and drop order to installer slot in calendar
   - Or use assign button/dialog
   - Select installer and confirm

5. **Verify Assignment**
   ```http
   GET http://localhost:5000/api/scheduler/slots?date={appointmentDate}
   Authorization: Bearer {token}
   ```
   - Verify ScheduledSlot created
   - Verify slot has OrderId and ServiceInstallerId

6. **Verify Order Updated**
   ```http
   GET http://localhost:5000/api/orders/{orderId}
   Authorization: Bearer {token}
   ```
   - Verify `AssignedSiId` is set
   - Verify `Status: "Assigned"`

**Or use monitoring script:**
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/check_scheduler_assignments.sql
```

**Success Criteria:**
- ✅ Unassigned orders appear in scheduler
- ✅ Service installers are listed and available
- ✅ Assignment creates ScheduledSlot
- ✅ Order status updates to "Assigned"
- ✅ Order.AssignedSiId is set

---

## Monitoring During Test

### Backend Logs

Monitor these log messages:
- `Email ingestion scheduler service started`
- `Scheduled email ingestion job for account...`
- `Processing email ingest job`
- `Email ingestion completed`
- `Processing email: {Subject}`
- `Created parse session {SessionId}`
- `Parsed order draft created`
- `Order created from draft {DraftId}`
- `Schedule slot created`

### Database Monitoring

Run monitoring scripts periodically:
```bash
# Check background jobs
psql -h localhost -U postgres -d cephasops -f scripts/test/check_background_jobs.sql

# Monitor email flow
psql -h localhost -U postgres -d cephasops -f scripts/test/monitor_email_flow.sql

# Check scheduler assignments
psql -h localhost -U postgres -d cephasops -f scripts/test/check_scheduler_assignments.sql
```

---

## Troubleshooting

### Scheduler Not Creating Jobs

1. Check `EmailAccount.PollIntervalSec` and `LastPolledAt`
2. Verify scheduler service is running (check logs)
3. Check database connectivity
4. Verify email account is active

### Email Not Parsing

1. Check ParserTemplate matches email sender
2. Verify Excel file format matches template
3. Check ParseSession status in database
4. Review backend logs for parsing errors

### Draft Approval Fails

1. Verify required fields (ServiceId, CustomerName, Address)
2. Check OrderType exists
3. Verify building was created/selected
4. Review backend logs for order creation errors

### Order Not Appearing in Scheduler

1. Check order status (should be "Pending" or "Scheduled")
2. Verify appointment date is in future
3. Check order has appointment time
4. Verify department matches scheduler filter

### Assignment Fails

1. Verify ServiceInstaller exists and is active
2. Check installer availability for appointment date
3. Verify no slot conflicts
4. Check backend logs for assignment errors

---

## Success Checklist

- [ ] Scheduler automatically creates email ingestion jobs
- [ ] Emails are ingested and parsed into drafts
- [ ] Drafts can be reviewed and approved
- [ ] Orders are created from approved drafts
- [ ] Orders can be assigned to service installers
- [ ] All data flows correctly through each stage
- [ ] No errors in logs (except expected warnings)

---

## Next Steps

After successful test:

1. Document any issues found
2. Verify performance (email ingestion frequency)
3. Test with multiple emails
4. Test edge cases (duplicate orders, missing data, etc.)
5. Verify scheduler handles account deactivation

---

## Test Scripts Reference

**Updated from:** `scripts/test/README.md`

This section documents the SQL test scripts available in `scripts/test/` for verifying prerequisites and monitoring test execution.

### Prerequisites Verification Scripts

#### 1. Verify Test Prerequisites
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/verify_test_prerequisites.sql
```

This script checks if all required test data exists:
- Departments
- Partners
- OrderTypes
- Buildings
- ServiceInstallers
- ParserTemplates
- EmailAccounts

**Expected Output:** All checks should return `true` or count >= 1

#### 2. Create Test Data (if missing)
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/create_test_data.sql
```

This script creates minimum required test data:
- GPON Department
- TIME FTTH Partner
- Activation OrderType
- Test Building
- Test ServiceInstaller
- TIME ParserTemplate

**Note:** Run this only if prerequisites are missing. The script uses `ON CONFLICT DO NOTHING` to avoid duplicates.

### Monitoring Scripts

#### 3. Monitor Background Jobs
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/check_background_jobs.sql
```

Use this to monitor:
- Recent EmailIngest jobs
- Job statistics (queued, running, succeeded, failed)
- Failed jobs with errors
- Currently running jobs

**Expected Results:**
- Should see `EmailIngest` jobs being created every 30 seconds (if accounts need polling)
- Jobs should transition: Queued → Running → Succeeded
- Failed jobs should have error messages in `LastError`

#### 4. Monitor Email Flow
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/monitor_email_flow.sql
```

Use this to track emails through the pipeline:
- Recent email messages
- Parse sessions
- Parsed order drafts
- Drafts ready for review
- Orders created from drafts
- Email account polling status

**Expected Results:**
- EmailMessages should appear after email ingestion
- ParseSessions should be created for each email
- ParsedOrderDrafts should contain extracted data
- Orders should appear after draft approval

#### 5. Check Scheduler Assignments
```bash
psql -h localhost -U postgres -d cephasops -f scripts/test/check_scheduler_assignments.sql
```

Use this to monitor:
- Unassigned orders
- Assigned orders
- Scheduled slots
- Service installer availability
- Orders by status

**Expected Results:**
- Unassigned orders should appear if orders exist
- Assigned orders should show installer names
- ScheduledSlots should be created after assignment

### Using with pgAdmin

1. Open pgAdmin
2. Connect to your database
3. Right-click on database → Query Tool
4. Open and run the SQL files
5. Review results

### Using with psql Command Line

```bash
# Set connection string
export PGHOST=localhost
export PGPORT=5432
export PGDATABASE=cephasops
export PGUSER=postgres
export PGPASSWORD=J@saw007

# Run scripts
psql -f verify_test_prerequisites.sql
psql -f create_test_data.sql
psql -f check_background_jobs.sql
```

---

## Comprehensive Test Suite Summary

**Updated from:** `backend/tests/COMPREHENSIVE_TEST_SUMMARY.md` and `backend/tests/COMPREHENSIVE_TEST_PLAN.md`

### Test Files Created

#### New Test Files
1. **MaterialTemplateServiceTests.cs** - 22 tests ✅
2. **ServiceInstallerServiceTests.cs** - 17 tests ✅
3. **WorkflowEngineServiceTests.cs** - 10 tests ✅
4. **RateEngineServiceTests.cs** - 13 tests ✅
5. **NotificationServiceTests.cs** - 8 tests ✅
6. **BuildingServiceTests.cs** - 6 tests ✅
7. **DepartmentServiceTests.cs** - 4 tests ✅

**Total New Tests**: 80 test methods

#### Existing Test Files
1. **AddressParserTests.cs** - 6 tests ✅
2. **PhoneNumberUtilityTests.cs** - Tests ✅
3. **AppointmentWindowParserTests.cs** - Tests ✅
4. **DocumentGenerationServiceTests.cs** - Tests ✅
5. **OrderCreationFromDraftTests.cs** - Tests ✅
6. **EmailRuleEvaluationServiceTests.cs** - Tests ✅
7. **CompanyServiceTests.cs** - Tests ✅

### Test Coverage by Module

#### ✅ Fully Tested
- Material Templates (CRUD) - 22 tests
- Service Installer Deduplication - 17 tests
- Workflow Engine - 10 tests
- Rate Engine - 13 tests
- Address Parsing - 6 tests
- Notifications - 8 tests
- Buildings - 6 tests
- Departments - 4 tests

#### ⚠️ Partially Tested
- Order Service - Some integration tests exist
- Parser Service - Email rule evaluation tested

#### ❌ Needs Testing
- OrderService (full CRUD, status management)
- InventoryService (stock movements)
- BillingService (invoice operations)
- SchedulerService (slot management)
- ParserService (Excel parsing)
- PayrollService (payroll calculations)
- RMAService (RMA processing)

### Test Statistics

- **Total Test Files**: 14
- **Total Test Methods**: 80+ new + existing
- **Test Framework**: xUnit, FluentAssertions, Moq
- **Database**: In-Memory EF Core

### Running Tests

```bash
cd backend
dotnet test tests/CephasOps.Application.Tests/CephasOps.Application.Tests.csproj
```

### Test Implementation Strategy

1. **Unit Tests**: Test individual service methods in isolation
2. **Integration Tests**: Test service interactions with database
3. **End-to-End Tests**: Test complete flows (email → order → assignment)

### Next Steps for Test Coverage

1. Fix any failing tests
2. Add tests for remaining critical services
3. Add integration tests for controllers
4. Achieve 80%+ code coverage

