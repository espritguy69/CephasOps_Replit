# Email Parser Setup Guide

This guide explains how to set up and configure the Email Parser module in CephasOps. The email parser automatically ingests emails from configured mailboxes and converts them into orders.

---

## Overview
In the current phase, all active email accounts are used for the GPON department. Future departments (CWO, NWO) can either share existing accounts or have their own dedicated EmailAccounts, but the setup process remains identical.

The Email Parser consists of:
1. **EmailAccount** - Stores mailbox configuration (non-sensitive data)
2. **CompanySetting** - Stores secure credentials (passwords, etc.)
3. **Email Ingestion Worker** - Polls mailboxes periodically
4. **Parser Engine** - Processes emails and creates orders

---

## Step-by-Step Setup

### Step 1: Create Email Account

Create an email account record using the API:

```http
POST /api/companies/{companyId}/email-accounts
Content-Type: application/json

{
  "name": "Cephas Orders Mailbox",
  "provider": "POP3",
  "host": "mail.cephas.com.my",
  "username": "admin@cephas.com.my",
  "isActive": true,
  "pollIntervalSec": 60
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "companyId": "company-guid-here",
  "name": "Cephas Orders Mailbox",
  "provider": "POP3",
  "host": "mail.cephas.com.my",
  "username": "admin@cephas.com.my",
  "isActive": true,
  "pollIntervalSec": 60,
  "lastPolledAt": null,
  "createdAt": "2025-01-20T10:00:00Z",
  "updatedAt": "2025-01-20T10:00:00Z"
}
```

**Note:** Save the `id` from the response - you'll need it for Step 2.

---

### Step 2: Store Email Account Credentials

Store the password and additional connection settings securely using CompanySetting:

```http
POST /api/companies/{companyId}/settings
Content-Type: application/json

{
  "key": "email.account.550e8400-e29b-41d4-a716-446655440000.credentials",
  "value": "{\"password\":\"C3ph@s123\",\"pop3Port\":110,\"smtpPort\":25,\"useSsl\":false}"
}
```

**Key Format:**
- Pattern: `email.account.{emailAccountId}.credentials`
- Replace `{emailAccountId}` with the actual EmailAccount ID from Step 1

**Value Format (JSON):**
```json
{
  "password": "your-email-password",
  "pop3Port": 110,        // Default: 110 (standard), 995 (SSL)
  "smtpPort": 25,         // Default: 25 (standard), 587 (TLS), 465 (SSL)
  "useSsl": false,        // Use SSL/TLS for POP3
  "useTls": false         // Use TLS for SMTP
}
```

**Common Port Configurations:**

| Provider | POP3 Port | SMTP Port | SSL/TLS |
|----------|-----------|-----------|---------|
| Standard POP3/SMTP | 110 | 25 | No |
| POP3S/SMTPS | 995 | 465 | Yes |
| POP3 with TLS | 110 | 587 | TLS |
| Gmail IMAP | 993 | 465 | Yes |
| Outlook 365 | 995 | 587 | TLS |

---

### Step 3: Verify Setup

Retrieve the email account to verify:

```http
GET /api/companies/{companyId}/email-accounts/550e8400-e29b-41d4-a716-446655440000
```

Retrieve credentials (for verification purposes):

```http
GET /api/companies/{companyId}/settings/key/email.account.550e8400-e29b-41d4-a716-446655440000.credentials
```

---

## Complete Example

### Example: Setting up Cephas Mailbox

**1. Create Email Account:**
```http
POST /api/companies/123e4567-e89b-12d3-a456-426614174000/email-accounts

{
  "name": "Cephas Orders Mailbox",
  "provider": "POP3",
  "host": "mail.cephas.com.my",
  "username": "admin@cephas.com.my",
  "isActive": true,
  "pollIntervalSec": 60
}
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  ...
}
```

**2. Store Credentials:**
```http
POST /api/companies/123e4567-e89b-12d3-a456-426614174000/settings

{
  "key": "email.account.550e8400-e29b-41d4-a716-446655440000.credentials",
  "value": "{\"password\":\"C3ph@s123\",\"pop3Port\":110,\"smtpPort\":25,\"useSsl\":false}"
}
```

**3. Verify:**
```http
GET /api/companies/123e4567-e89b-12d3-a456-426614174000/email-accounts
```

---

## Provider-Specific Configurations

### POP3/SMTP (Standard)
```json
{
  "provider": "POP3",
  "host": "mail.example.com",
  "pop3Port": 110,
  "smtpPort": 25,
  "useSsl": false
}
```

### POP3S/SMTPS (SSL)
```json
{
  "provider": "POP3",
  "host": "mail.example.com",
  "pop3Port": 995,
  "smtpPort": 465,
  "useSsl": true
}
```

### Gmail IMAP
```json
{
  "provider": "IMAP",
  "host": "imap.gmail.com",
  "pop3Port": 993,
  "smtpPort": 465,
  "useSsl": true
}
```

### Microsoft 365 (Exchange)
```json
{
  "provider": "O365",
  "host": "outlook.office365.com",
  "pop3Port": 995,
  "smtpPort": 587,
  "useTls": true
}
```

---

## Managing Email Accounts

### List All Email Accounts
```http
GET /api/companies/{companyId}/email-accounts
```

### Update Email Account
```http
PUT /api/companies/{companyId}/email-accounts/{id}

{
  "name": "Updated Mailbox Name",
  "provider": "POP3",
  "host": "mail.cephas.com.my",
  "username": "admin@cephas.com.my",
  "isActive": true,
  "pollIntervalSec": 120
}
```

### Update Credentials
```http
POST /api/companies/{companyId}/settings

{
  "key": "email.account.{emailAccountId}.credentials",
  "value": "{\"password\":\"NewPassword123\",\"pop3Port\":110,\"smtpPort\":25}"
}
```

### Deactivate Email Account
```http
PUT /api/companies/{companyId}/email-accounts/{id}

{
  "isActive": false,
  ...
}
```

### Delete Email Account
```http
DELETE /api/companies/{companyId}/email-accounts/{id}
```

**Note:** This does NOT delete credentials. Delete credentials separately:
```http
DELETE /api/companies/{companyId}/settings/key/email.account.{emailAccountId}.credentials
```

---

## Security Best Practices

1. **Never store passwords in EmailAccount table** - Always use CompanySetting
2. **Use strong, unique passwords** for email accounts
3. **Rotate credentials regularly** - Update via CompanySetting API
4. **Use SSL/TLS** when available for secure connections
5. **Restrict access** to CompanySetting endpoints via RBAC
6. **Audit credential changes** - Monitor CompanySetting updates

---

## Troubleshooting

### Email Account Not Polling

1. **Check if account is active:**
   ```http
   GET /api/companies/{companyId}/email-accounts/{id}
   ```
   Verify `isActive: true`

2. **Verify credentials exist:**
   ```http
   GET /api/companies/{companyId}/settings/key/email.account.{id}.credentials
   ```

3. **Check credentials format:**
   Ensure JSON is valid and contains required fields (`password`)

4. **Verify connection settings:**
   - Host is correct
   - Ports are correct for provider
   - SSL/TLS settings match provider requirements

### Connection Errors

- **POP3 Port 110 blocked:** Use port 995 with SSL
- **SMTP Port 25 blocked:** Use port 587 with TLS
- **Authentication failed:** Verify username and password
- **SSL/TLS handshake failed:** Check `useSsl`/`useTls` settings

### Email Not Processing

1. Check if emails are being ingested:
   ```http
   GET /api/companies/{companyId}/email-messages
   ```

2. Check parser status:
   ```http
   GET /api/companies/{companyId}/email-messages?parserStatus=Pending
   ```

3. Review parser errors:
   ```http
   GET /api/companies/{companyId}/email-messages?parserStatus=Error
   ```

---

## Integration with Parser Engine

Once an email account is set up and active:

1. **Email Ingestion Worker** polls the mailbox every `pollIntervalSec` seconds
2. **New emails** are stored in `EmailMessages` table
3. **Parser Engine** processes emails based on `ParserTemplates`
4. **Orders are created** from parsed email data

See [SPECIFICATION.md](./SPECIFICATION.md) for parsing logic details.

---

## API Endpoints Summary

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/companies/{companyId}/email-accounts` | GET | List all email accounts |
| `/api/companies/{companyId}/email-accounts/{id}` | GET | Get email account by ID |
| `/api/companies/{companyId}/email-accounts` | POST | Create email account |
| `/api/companies/{companyId}/email-accounts/{id}` | PUT | Update email account |
| `/api/companies/{companyId}/email-accounts/{id}` | DELETE | Delete email account |
| `/api/companies/{companyId}/settings` | POST | Store credentials (upsert) |
| `/api/companies/{companyId}/settings/key/{key}` | GET | Get credentials by key |
| `/api/companies/{companyId}/settings/key/{key}` | DELETE | Delete credentials |

---

## Using the Credential Service

For programmatic access to credentials, use the `EmailAccountCredentialService`:

```csharp
// Inject the service
private readonly IEmailAccountCredentialService _credentialService;

// Retrieve credentials
var credentials = await _credentialService.GetCredentialsAsync(
    emailAccountId, 
    companyId, 
    cancellationToken
);

if (credentials != null)
{
    var password = credentials.Password;
    var pop3Port = credentials.Pop3Port ?? 110;
    var smtpPort = credentials.SmtpPort ?? 25;
    var useSsl = credentials.UseSsl ?? false;
}

// Store credentials
var newCredentials = new EmailAccountCredentials
{
    Password = "secure-password",
    Pop3Port = 110,
    SmtpPort = 25,
    UseSsl = false
};

await _credentialService.StoreCredentialsAsync(
    emailAccountId, 
    companyId, 
    newCredentials, 
    cancellationToken
);
```

---

## Architecture

The email parser setup uses a two-table approach for security:

1. **EmailAccount Table** - Stores non-sensitive configuration:
   - Display name
   - Provider type (POP3, IMAP, O365)
   - Host/username
   - Poll interval
   - Active status

2. **CompanySetting Table** - Stores sensitive credentials:
   - Password
   - Port numbers
   - SSL/TLS settings
   - Other connection parameters

This separation ensures:
- ✅ Credentials are never exposed in EmailAccount queries
- ✅ Credentials can be updated without changing EmailAccount
- ✅ Credentials are company-scoped for multi-tenant security
- ✅ Credentials follow the settings module pattern

---

## Related Documentation

- [SPECIFICATION.md](./SPECIFICATION.md) - Parser logic and rules
- [EMAIL_PIPELINE.md](../../01_system/EMAIL_PIPELINE.md) - Email processing pipeline
- [SETTINGS_MODULE.md](./SETTINGS_MODULE.md) - Settings module overview
- [Parser Entities](../../05_data_model/entities/parser_entities.md) - Data model

---

## Monitoring (Parser health)

The system health endpoint reports email parser status so you can monitor ingestion without logging in:

- **Endpoint:** `GET /api/admin/health` (requires auth)
- **Response:** Includes an `emailParser` object with:
  - **status:** `Healthy` (all accounts polled within 15 min), `Degraded` (one or more stale), or `NoAccounts` (no active mailboxes)
  - **activeAccountsCount**, **mostRecentPollAt**, **staleAccountsCount**
  - **accounts:** per-account last poll time and status (`Healthy` / `Stale` / `NeverPolled`)

Use this in dashboards or alerts to detect when the email ingestion scheduler or mailboxes stop polling.

---

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review email parser logs
3. Verify credentials and connection settings
4. Contact system administrator

