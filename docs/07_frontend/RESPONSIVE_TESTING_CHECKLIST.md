# Responsive Design Testing Checklist

## Overview
This document provides a comprehensive checklist for testing responsive design across all CephasOps frontend pages at different viewport widths.

## Test Viewport Widths
- **360px** - Small mobile (iPhone SE, small Android)
- **768px** - Tablet (iPad portrait)
- **1024px** - Small desktop / Large tablet (iPad landscape)
- **1280px** - Standard desktop

## Testing Criteria

### 1. No Horizontal Scrolling
- [ ] Page content fits within viewport width
- [ ] No horizontal scrollbar appears
- [ ] Tables use responsive card view on mobile (DataTable component)
- [ ] Fixed-width elements don't exceed viewport
- [ ] Modals are responsive and don't overflow

### 2. Proper Touch Targets
- [ ] All buttons have minimum 44x44px touch area
- [ ] Form inputs have minimum 44px height
- [ ] Links and clickable elements are easily tappable
- [ ] Spacing between interactive elements is adequate (minimum 8px)

### 3. Readable Text
- [ ] Text size is at least 14px on mobile (16px preferred)
- [ ] Line height provides adequate spacing (1.5x minimum)
- [ ] Text contrast meets WCAG AA standards
- [ ] Text doesn't overflow containers
- [ ] Long text is truncated or wrapped appropriately

### 4. Working Modals
- [ ] Modals are responsive (max-width: calc(100vw - 2rem) on mobile)
- [ ] Modal content is scrollable if needed
- [ ] Modal close button is accessible
- [ ] Modal doesn't overflow viewport
- [ ] Backdrop overlay works correctly

## Page-by-Page Testing Checklist

### Core Pages

#### Dashboard (`/dashboard`)
- [ ] Stats cards: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`
- [ ] Charts are responsive
- [ ] Recent orders table uses card view on mobile
- [ ] All touch targets ≥44px

#### Orders List (`/orders`)
- [ ] Filter grid: `grid-cols-1 md:grid-cols-2 lg:grid-cols-4`
- [ ] Search bar is full width on mobile
- [ ] Table uses DataTable mobile card view
- [ ] Action buttons stack on mobile
- [ ] Date filter is responsive

#### Order Detail (`/orders/:orderId`)
- [ ] Info grid: `grid-cols-1 md:grid-cols-2 lg:grid-cols-4`
- [ ] Tabs are scrollable on mobile if needed
- [ ] Material tables use card view
- [ ] Action buttons are accessible

#### Create Order (`/orders/create`)
- [ ] Form fields: `grid-cols-1 md:grid-cols-2`
- [ ] All inputs have min-h-[44px]
- [ ] Date picker is responsive
- [ ] Material tables are scrollable
- [ ] Save/Cancel buttons stack on mobile

#### Tasks List (`/tasks`)
- [ ] Filter grid: `grid-cols-1 md:grid-cols-2 lg:grid-cols-4`
- [ ] Table uses card view on mobile
- [ ] Create button is accessible
- [ ] Status badges are readable

### Settings Pages

#### Partners (`/settings/partners`)
- [ ] Uses DataTable with mobile card view
- [ ] Filter controls are responsive
- [ ] Create/Edit modal is responsive
- [ ] Export button is accessible

#### Service Installers (`/settings/service-installers`)
- [ ] Table uses card view on mobile
- [ ] Form fields are responsive
- [ ] Contact management is mobile-friendly

#### Buildings (`/settings/buildings`)
- [ ] List view is responsive
- [ ] Detail page uses responsive grids
- [ ] Form layouts are mobile-friendly

### Financial Pages

#### Invoices List (`/billing/invoices`)
- [ ] Table uses card view on mobile
- [ ] Filter controls are responsive
- [ ] Status badges are readable

#### P&L Summary (`/pnl/summary`)
- [ ] Stats grid: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-4`
- [ ] Charts are responsive
- [ ] Period selector is accessible

#### Payroll Runs (`/payroll/runs`)
- [ ] Table uses card view on mobile
- [ ] Filter controls are responsive
- [ ] Export functionality works

### Inventory Pages

#### Inventory List (`/inventory/list`)
- [ ] Table uses card view on mobile
- [ ] Search and filters are responsive
- [ ] Action buttons are accessible

#### RMA List (`/rma`)
- [ ] Table uses card view on mobile
- [ ] Status filters are responsive
- [ ] Create RMA button is accessible

### Scheduler Pages

#### Calendar (`/scheduler`)
- [ ] Sidebar collapses on mobile
- [ ] Time slots grid is scrollable
- [ ] Order cards are readable
- [ ] Drag-and-drop works on touch devices

#### Installer Scheduler (`/scheduler/timeline`)
- [ ] Timeline is horizontally scrollable
- [ ] Installer list is accessible
- [ ] Order assignments work on mobile

### Parser Pages

#### Parse Session Review (`/orders/parser`)
- [ ] Email list/content split is responsive
- [ ] Draft cards are readable
- [ ] Action buttons are accessible
- [ ] Tables use card view on mobile

### Special Pages

#### Email Management (`/email`)
- [ ] Email list/content split: `grid-cols-1 lg:grid-cols-3`
- [ ] Email preview is readable
- [ ] Action buttons are accessible

#### Assets Dashboard (`/assets`)
- [ ] Stats grid: `grid-cols-1 md:grid-cols-2 lg:grid-cols-4`
- [ ] Asset list uses card view on mobile
- [ ] Filters are responsive

## Common Issues to Watch For

### Fixed Widths
- ❌ `w-[420px]` - Fixed width modals
- ❌ `min-w-[600px]` - Tables that force horizontal scroll
- ✅ Use `w-full max-w-[calc(100vw-2rem)]` for modals
- ✅ Use responsive grid classes for layouts

### Touch Targets
- ❌ Buttons smaller than 44x44px
- ❌ Inputs without min-h-[44px]
- ✅ All interactive elements ≥44px
- ✅ Adequate spacing between elements

### Text Readability
- ❌ Text smaller than 14px on mobile
- ❌ Text that overflows containers
- ✅ Use responsive text sizes: `text-xs md:text-sm`
- ✅ Truncate or wrap long text

### Overflow Issues
- ❌ Elements that cause horizontal scroll
- ❌ Tables without responsive handling
- ✅ Use `overflow-x-auto` for scrollable containers
- ✅ Use DataTable mobile card view

## Testing Tools

### Browser DevTools
1. Open Chrome/Firefox DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M / Cmd+Shift+M)
3. Select viewport width or enter custom size
4. Test at: 360px, 768px, 1024px, 1280px

### Manual Testing Steps
1. **Check for horizontal scrolling**
   - Scroll page horizontally
   - Verify no scrollbar appears
   - Check all sections and modals

2. **Test touch targets**
   - Tap all buttons and links
   - Verify they're easy to tap
   - Check spacing between elements

3. **Verify text readability**
   - Read all text content
   - Check for text overflow
   - Verify adequate line spacing

4. **Test modals**
   - Open all modals
   - Verify they fit viewport
   - Check scrollability if needed
   - Test close functionality

## Known Issues & Fixes

### Fixed Issues
- ✅ DataTable has mobile card view
- ✅ Modal uses responsive sizing
- ✅ Form inputs have min-h-[44px]
- ✅ Dashboard stats use responsive grids

### Potential Issues to Monitor
- ⚠️ CreateOrderPage has fixed-width modal (`w-[420px]`) - may need responsive sizing
- ⚠️ DataTable has `min-w-[600px]` on table element - handled by mobile card view
- ⚠️ Some pages may need additional responsive breakpoints

## Completion Status

- [x] Core layout components (MainLayout, Sidebar, TopNav, Modal)
- [x] Form components (TextInput, Select, Textarea, Input)
- [x] DataTable component with mobile card view
- [x] Dashboard pages responsive grids
- [x] List pages responsive filters
- [x] Detail pages responsive grids
- [ ] Manual testing at all viewport widths (requires browser testing)

## Notes

- Most responsive design is already implemented
- DataTable automatically switches to card view on mobile (<768px)
- Modals use responsive sizing by default
- Form components have touch-friendly heights
- Manual browser testing is required to verify all edge cases

