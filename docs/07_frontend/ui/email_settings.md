# Email Settings UI

## Overview

The Email Settings UI allows administrators and operations staff to manage email mailboxes, routing rules, and VIP email lists used by the email parser and notification system.

## Access Control

- **Permissions Required**:
  - `settings.manage_email` for mailboxes and rules
  - `settings.manage_vip_emails` for VIP email list
- **Roles**: Admin, Operations Manager

## Mailboxes Screen

### Route
`/settings/email/mailboxes` or `/companies/{companyId}/settings/email/mailboxes`

### Features

**Mailbox List**:
- Table/grid showing all email accounts
- Columns:
  - Name
  - Email Address
  - Provider (IMAP, POP3, O365, Gmail)
  - Host (if applicable)
  - Active status (toggle)
  - Last Polled At
  - Actions (Edit, Delete, Test Connection)

**Add/Edit Mailbox Dialog**:
- Fields:
  - Name (required)
  - Email Address (required)
  - Provider (dropdown: IMAP, POP3, O365, Gmail)
  - Host (required for IMAP/POP3)
  - Port (required for IMAP/POP3)
  - Use SSL/TLS (checkbox)
  - Username (required)
  - Password (masked input)
  - Poll Interval (minutes, default from GlobalSettings)
  - Active (checkbox)
- Validation:
  - Email format validation
  - Host format validation
  - Port range validation (1-65535)
- Test Connection button (validates credentials without saving)

**Actions**:
- Add new mailbox
- Edit existing mailbox
- Delete mailbox (with confirmation)
- Toggle active status
- Test connection
- View polling history (future)

**API Endpoints**:
- `GET /api/companies/{companyId}/email-accounts`
- `POST /api/companies/{companyId}/email-accounts`
- `PUT /api/companies/{companyId}/email-accounts/{id}`
- `DELETE /api/companies/{companyId}/email-accounts/{id}`
- `POST /api/companies/{companyId}/email-accounts/{id}/test-connection`

**Component**: `EmailMailboxesPage`

## Email Rules Screen

### Route
`/settings/email/rules` or `/companies/{companyId}/settings/email/rules`

### Features

**Rules List**:
- Table/grid showing all email rules
- Columns:
  - Priority (sortable)
  - From Address Pattern
  - Domain Pattern
  - Subject Contains
  - Is VIP (badge)
  - Action Type
  - Target (Department/User)
  - Active status (toggle)
  - Actions (Edit, Delete, Duplicate)

**Add/Edit Rule Dialog**:
- Fields:
  - Email Account (dropdown, optional - "All Mailboxes")
  - From Address Pattern (optional, supports wildcards)
  - Domain Pattern (optional, e.g., "@time.com.my")
  - Subject Contains (optional, case-insensitive)
  - Is VIP (checkbox)
  - Action Type (dropdown):
    - Mark VIP Only
    - Route to Department
    - Route to User
    - Ignore
  - Target Department (if action = Route to Department)
  - Target User (if action = Route to User)
  - Priority (number, higher = higher priority)
  - Active (checkbox)
  - Description (optional)
- Validation:
  - At least one pattern field must be filled
  - Priority must be unique or warn about conflicts
  - Target must be selected if action requires it
- Preview: Shows example emails that would match this rule

**Actions**:
- Add new rule
- Edit existing rule
- Delete rule (with confirmation)
- Duplicate rule
- Toggle active status
- Reorder priority (drag-and-drop or up/down buttons)
- Test rule against sample email (future)

**Rule Evaluation Order**:
- Rules are evaluated in priority order (higher number first)
- First matching rule wins
- Show warning if multiple rules have same priority

**API Endpoints**:
- `GET /api/companies/{companyId}/email-rules`
- `POST /api/companies/{companyId}/email-rules`
- `PUT /api/companies/{companyId}/email-rules/{id}`
- `DELETE /api/companies/{companyId}/email-rules/{id}`
- `POST /api/companies/{companyId}/email-rules/{id}/test` (future)

**Component**: `EmailRulesPage`

## VIP Email List Screen

### Route
`/settings/email/vip` or `/companies/{companyId}/settings/email/vip`

### Features

**VIP Email List**:
- Table/grid showing all VIP email entries
- Columns:
  - Email Address
  - Display Name
  - Notify User (name)
  - Notify Role
  - Active status (toggle)
  - Actions (Edit, Delete)

**Add/Edit VIP Email Dialog**:
- Fields:
  - Email Address (required, exact match)
  - Display Name (required, e.g., "CEO", "Director")
  - Notify User (dropdown, optional)
  - Notify Role (dropdown, optional)
  - Notes (optional)
  - Active (checkbox)
- Validation:
  - Email format validation
  - Either Notify User or Notify Role must be selected
- Help text: Explains that notifications will be sent to the selected user/role when emails from this address are received

**Actions**:
- Add new VIP email
- Edit existing VIP email
- Delete VIP email (with confirmation)
- Toggle active status
- Bulk import from CSV (future)

**Integration**:
- VIP emails are checked during email rule evaluation
- If email matches VIP list, `IsVip = true` is set
- Notifications are sent based on `NotifyUserId` or `NotifyRole`

**API Endpoints**:
- `GET /api/companies/{companyId}/vip-emails`
- `POST /api/companies/{companyId}/vip-emails`
- `PUT /api/companies/{companyId}/vip-emails/{id}`
- `DELETE /api/companies/{companyId}/vip-emails/{id}`

**Component**: `VipEmailsPage`

## UI Components

### EmailAccountForm
- Add/Edit mailbox form
- Provider selection
- Credential inputs (with show/hide password)
- Connection test button
- Validation feedback

### EmailRuleForm
- Add/Edit rule form
- Pattern inputs with examples
- Action type selection
- Target selection (department/user dropdowns)
- Priority input with conflict warning
- Rule preview/test section

### VipEmailForm
- Add/Edit VIP email form
- User/role selection
- Help text for notification behavior

### PatternInput
- Input with pattern examples
- Wildcard help text
- Pattern validation feedback

## State Management

- Mailbox list state
- Rule list state
- VIP email list state
- Selected item state
- Form state
- Validation state
- Loading states
- Error states

## Responsive Design

- Mobile-friendly tables (card view on mobile)
- Collapsible form sections on mobile
- Touch-friendly action buttons
- Optimized for tablet and desktop

## Security Considerations

- Password fields are masked
- Credentials are never displayed after save
- Test connection validates without saving credentials
- Delete actions require confirmation
- Audit log for all changes (future)

