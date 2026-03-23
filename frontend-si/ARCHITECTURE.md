# CephasOps SI-App Architecture
Version: 1.0  
Status: Design Reference  
Audience: Product, Engineering, UX, Operations  

---

# 1. Purpose

This document defines the **Service Installer (SI) mobile application architecture** for CephasOps.

The SI App is a **mobile-first field operations tool** used by service installers to:

- Receive assigned jobs
- Navigate to customer sites
- Perform installations or assurance repairs
- Track material usage and device serials
- Capture job proof and documentation
- Handle blockers and reschedules
- Record extra customer-paid work
- Track earnings and job history

The design principle is inspired by **Uber Driver style workflow**:

> One job → guided steps → large actions → minimal typing.

---

# 2. Design Principles

The SI App must follow these core UX rules:

### Simplicity
Installers should never feel confused about the next action.

### Guided Workflow
The app controls the job flow using step-based progression.

### Minimal Typing
Prefer:
- scanning
- dropdowns
- numeric entry
- photos

### Fast Field Use
All actions must be usable **with one hand**.

### Robust Backend Tracking
Even though the UX is simple, the system must capture:

- material movement
- serial usage
- removed devices
- proof photos
- customer approvals
- job history

---

# 3. App Navigation Structure

The SI App uses **5 primary tabs**.

| Home | Jobs | Scan | Earnings | Profile |

Additionally, when a job is active, a persistent **Current Job bar** appears.

```
Current Job: Mr Lim – 2.4 km away
[OPEN]
```

---

# 4. Screen Architecture

```
AUTHENTICATION
├── Login
├── OTP Verification
└── Password Reset

APP SHELL
├── Home
├── Jobs
├── Scan
├── Earnings
└── Profile

JOB FLOW
├── Job Assignment View
├── Job Detail
├── On The Way
├── Met Customer
├── Start Work
├── Materials & Device Scan
├── Assurance Replacement
├── Extra Work
├── Proof Capture
├── Problem / Reschedule
├── Completion Review
└── Job Submitted

INVENTORY
├── My Inventory
├── Scan Stock
├── Return Item
├── Faulty Item
└── Transfer Stock

SUPPORT
├── Help Center
├── Call Admin
├── Technical Support
└── Report App Issue
```

---

# 5. Home Screen

The **Home screen** shows the installer's daily overview.

### Information Displayed

- Good Morning, Amir
- Today's Jobs: 6
- Completed: 2
- Remaining: 4
- Problems: 1

### Active Job Card

- **Current Job**
- Mr Lim
- Subang Jaya
- 10:00 AM
- Assurance

- [OPEN JOB] [NAVIGATE] [CALL]

### Earnings Summary

- Today Earnings: RM145
- Month Earnings: RM4280
- Next Bonus Tier: 2 jobs remaining

### Quick Actions

- Scan Device
- My Inventory
- Report Problem
- Help

---

# 6. Jobs Screen

The Jobs screen shows all assignments.

### Tabs

- Today
- Upcoming
- History

### Job Card Example

- 10:00 AM
- Mr Lim
- Subang Jaya
- Assurance
- Estimated payout: RM45
- [MAP] [CALL] [OPEN]

### Job Status Colors

| Status       | Color  |
|-------------|--------|
| Assigned    | Grey   |
| On The Way  | Blue   |
| Met Customer| Orange |
| Complete    | Green  |
| Problem     | Red    |

---

# 7. Job Detail Screen

The **Job Detail screen** is the core operational screen.

### Sections

- Customer Info
- Job Info
- Materials
- Device Replacement
- Extra Work
- Proof Capture
- Notes
- Completion Summary

---

# 8. Installer Status Workflow

Installer jobs follow a simplified status model.

```
ASSIGNED
   ↓
ON_THE_WAY
   ↓
MET_CUSTOMER
   ↓
START_WORK
   ↓
COMPLETE
```

Exception paths:

- PROBLEM
- RESCHEDULE

These statuses are intentionally limited to avoid operational confusion.

---

# 9. Job Info Section

Displays important job metadata.

### Fields

- Order ID
- Job Type
- Appointment Window
- Service ID
- Customer Address
- Building Type
- Installation Method
- Partner (TIME / CelcomDigi / etc)
- Admin Notes

For assurance jobs:

- Existing installed device serial
- Previous installation reference

---

# 10. Material Scanning

The system supports **two material categories**.

---

## Serialized Devices

Examples:

- ONT
- Router
- Mesh Unit

### Workflow

```
Scan Serial
   ↓
Validate Inventory
   ↓
Attach to Order
```

### Validation Rules

- Serial exists in inventory
- Serial belongs to installer or order
- Serial not already installed elsewhere

---

## Non-Serialized Materials

Examples:

- LAN Cable
- Clips
- Casing

Installer enters quantity.

Example:

- LAN Cable Used: 25m
- Clips Used: 12 pcs
- Casing Used: 10 pcs

---

# 11. Assurance Replacement Flow

Used for repair jobs where devices are replaced.

```
Existing Device
   ↓
Scan Removed Device
   ↓
Record Condition
   ↓
Scan Replacement Device
   ↓
Complete Job
```

### Removed Device Conditions

- FAULTY
- DOA
- WORKING_REPLACED
- PHYSICAL_DAMAGE

### Backend Records

- DeviceReplacementRecord
- RemovedDeviceRecord
- InventoryMovement

---

# 12. Extra Work Flow

Extra work refers to services outside the partner job scope.

Examples:

- Repull LAN cable
- Router relocation
- Install casing
- Additional LAN point

### Workflow

```
Select Service
   ↓
Enter Quantity
   ↓
Calculate Price
   ↓
Customer Approval
   ↓
Perform Work
   ↓
Upload Proof
   ↓
Record Payment
   ↓
Generate Receipt
```

---

# 13. Proof Capture

Proof photos are required before completion.

### Example Proof Checklist

**Installation Job**

- ONT Serial Photo
- Router Installed Photo
- Final Setup Photo

**Assurance Replacement**

- Old Device Serial
- New Device Serial
- Installed Replacement Photo

**Extra Work**

- Before Photo
- Work Route Photo
- After Photo

---

# 14. Problem / Reschedule

Installer may report issues during job execution.

### Categories

- Building Issue
- Customer Issue
- Technical Issue
- Reschedule

### Required Fields

- Category
- Reason
- Photo evidence
- Optional remarks

Example:

- Problem Type: Customer Issue
- Reason: Customer not at home
- Photo: uploaded
- Remark: Phone unreachable

---

# 15. Completion Review

Before submission the system verifies all required actions.

Example:

```
Completion Review

Status Steps:
✓ On the Way
✓ Met Customer
✓ Start Work

Materials:
✓ Router scanned
✓ Cable usage entered

Proof:
✓ Final setup photo
✓ Serial photo

Customer Confirmation:
✓ Captured
```

Installer can:

- **Submit Complete**
- **Return to Fix Missing Items**

---

# 16. Job Submitted Screen

Displays confirmation and earnings summary.

Example:

- Job Submitted Successfully
- Estimated payout: RM55
- Extra Work Collected: RM75
- Receipt Sent: Yes
- [Next Job] [View Summary]

---

# 17. Earnings Screen

Tracks installer income.

### Tabs

- Today
- Week
- Month
- History

### Earnings Breakdown

- Base Job Earnings
- Extra Work Earnings
- Bonus Incentives
- Pending Approval

Example:

- Today Earnings: RM145
- Jobs Completed: 3
- Extra Work: RM35
- Pending Approval: RM20

---

# 18. Inventory Screen

Displays installer stock.

### Serialized Devices

- ONT SN: HWT12345
- Router SN: RT56789

### Non-Serialized Materials

- LAN Cable: 80m
- Clips: 20 pcs
- Casing: 10 pcs

### Actions

- Scan Item
- Return Item
- Mark Faulty
- Request Stock

---

# 19. Profile Screen

Installer account information.

Includes:

- Installer Name
- Partner / Team
- Contact Details
- Help Center
- Support Hotline
- App Version
- Logout

---

# 20. Notifications

Critical alerts appear as banners.

Examples:

- You have a job starting in 30 minutes
- Customer approval required for extra work
- Serial not found in inventory
- Pending job submission

---

# 21. Future Enhancements

Planned improvements:

- Offline job support
- Automatic GPS arrival detection
- Voice notes for problems
- Installer performance badges
- AI assisted troubleshooting
- Material forecasting

---

# 22. Related Documents

- docs/business/si_app_journey.md
- docs/business/process_flows.md
- docs/architecture/data_model_overview.md
- docs/business/inventory_ledger_summary.md
- docs/business/payroll_rate_overview.md

---

# 23. Summary

The CephasOps SI App architecture focuses on:

- **Speed**
- **Clarity**
- **Traceability**
- **Installer motivation**

By simplifying the installer workflow while maintaining robust backend tracking, the platform stays efficient, auditable, and easy for installers to use in the field.
