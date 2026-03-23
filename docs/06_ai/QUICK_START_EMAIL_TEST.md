# Quick Start: Live Email Test

## 🚀 Ready to Test!

All code changes are complete and the system is ready for live email testing.

## ⚡ Quick Verification (2 minutes)

### 1. Check Database Migration
```powershell
# Run from backend directory
.\scripts\verify-migration.ps1 -ConnectionString "Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASS"
```

Or manually:
```sql
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Orders' 
AND column_name IN ('PackageName', 'Bandwidth', 'OnuSerialNumber', 'VoipServiceId');
```

### 2. Verify Email Account
- Go to Email Accounts page in UI
- Ensure at least one account is **Active**
- Test connection if available

### 3. Start Services
```powershell
# Backend
cd backend
.\start.ps1

# Frontend (separate terminal)
cd frontend
npm run dev
```

## 🧪 Test Steps (5 minutes)

### Step 1: Send Test Email
Send an email to your configured mailbox with:
- **Subject:** Test Order - M1234567 (or any TIME format)
- **Attachment:** Excel file (.xls or .xlsx) with TIME work order format
- **Content:** Can be minimal, attachments will be processed

### Step 2: Trigger Email Poll
**Option A - Via API:**
```bash
POST http://localhost:5000/api/email-accounts/{emailAccountId}/poll
```

**Option B - Via UI:**
- Go to Email Accounts page
- Click "Poll Now" or similar action button

**Option C - Wait:**
- If background job is configured, wait for automatic poll

### Step 3: Check Results
1. **Backend Logs:** Look for:
   ```
   Processing email: Test Order - M1234567
   Found 1 attachments in email, processing...
   Parsing Excel attachment: filename.xlsx
   Extracted: Customer=..., ServiceId=TBBN...
   ```

2. **Frontend:** 
   - Go to **Parser → Parse Sessions**
   - Find the new session
   - Open it and check draft details

3. **Verify Technical Details:**
   - Open draft modal
   - Look for **Technical Details** section (blue)
   - Look for **Fiber Internet** section (green) if present
   - Look for **VOIP** section (purple) if present
   - Test password Show/Hide buttons

### Step 4: Approve & Verify Order
1. Click **Approve** on the draft
2. Check **Orders** list - new order should appear
3. Open order details
4. Verify technical fields are populated:
   - PackageName
   - Bandwidth
   - OnuSerialNumber
   - VoipServiceId
   - PartnerNotes (contains full remarks with technical details)

## ✅ Success Indicators

- ✅ Email ingested successfully
- ✅ Excel attachment parsed automatically
- ✅ Parse session created with status "Completed"
- ✅ Draft shows technical details in separate sections
- ✅ Passwords are masked (Show/Hide works)
- ✅ Order created with all technical fields
- ✅ PartnerNotes contains structured technical details

## 🔍 Troubleshooting

### Email Not Ingested
```sql
-- Check email account
SELECT * FROM "EmailAccounts" WHERE "IsActive" = true;
```

### Attachments Not Processed
- Check backend logs for errors
- Verify attachment is .xls or .xlsx
- Check file size (should be < 50MB)

### Technical Details Missing
- Check `ParsedOrderDrafts.Remarks` field in database
- Verify Excel file contains expected labels
- Check `TimeExcelParserService` logs

### Frontend Not Showing Details
- Check browser console for errors
- Verify `parseTechnicalDetails` utility is loaded
- Check network tab for API responses

## 📋 Full Checklist

See `LIVE_EMAIL_TEST_CHECKLIST.md` for complete detailed checklist.

## 🎯 What's New

### Automatic Attachment Processing
- Excel attachments are now **automatically parsed** when email is ingested
- No manual upload needed!
- Technical details extracted and stored in structured format

### Enhanced Technical Details Display
- **3 separate sections:**
  - Technical Details (basic credentials)
  - Fiber Internet (network config)
  - VOIP (voice network config)
- **Password masking** with Show/Hide toggle
- **Color-coded** sections for easy identification

### Complete Data Flow
```
Email → Attachments Extracted → Excel Parsed → Technical Details Extracted 
→ Draft Created → Order Created → All Fields Mapped
```

---

**Ready to test?** Follow the steps above and check the results! 🚀

