# Time Slot Settings Page - Visibility Fix

## Issue
TimeSlotSettingsPage was not visible in the UI - it was not listed in the sidebar navigation menu, even though the page component existed and was partially configured in routes.

## Changes Made

### 1. Added to Settings Routes (`frontend/src/routes/settingsRoutes.tsx`)
- Added lazy import for `TimeSlotSettingsPage`
- Added route configuration: `{ path: 'time-slots', element: <TimeSlotSettingsPage /> }`
- Added to `settingsNavigation.systemReports` array for navigation structure

### 2. Added to Sidebar Navigation (`frontend/src/components/layout/Sidebar.tsx`)
- Added to Workflow section in `SETTINGS_SUB_ITEMS`:
  ```typescript
  { path: '/settings/time-slots', label: 'Time Slots', icon: Clock }
  ```
- Positioned after "Business Hours & Holidays" and before "Escalation Rules"

### 3. Verified Existing Configuration
- Confirmed route already exists in `App.tsx` (line 322)
- Confirmed import already exists in `App.tsx` (line 79)
- Route path: `/settings/time-slots`

## Navigation Path
Users can now access Time Slot Settings via:
1. **Sidebar**: Settings → Workflow → Time Slots
2. **Direct URL**: `/settings/time-slots`

## Location in Sidebar
The Time Slots page appears in the **Workflow** section of the Settings submenu, alongside:
- Order Statuses
- Workflow Definitions
- Guard Condition Definitions
- Side Effect Definitions
- SLA Configuration
- Automation Rules
- Approval Workflows
- Business Hours & Holidays
- **Time Slots** ← (newly added)
- Escalation Rules

## Testing
To verify the page is accessible:
1. Navigate to Settings in the sidebar
2. Expand the "Workflow" section
3. Click on "Time Slots"
4. Verify the page loads with drag-and-drop functionality for reordering time slots

