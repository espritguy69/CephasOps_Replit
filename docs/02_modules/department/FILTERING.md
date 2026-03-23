# Department Filtering Implementation Guide

## Overview

CephasOps now operates in **single-company, multi-department mode**. All major modules have been updated to respect the active department selection, providing department-scoped data views throughout the application.

## Architecture

### Core Components

1. **DepartmentContext** (`frontend/src/contexts/DepartmentContext.jsx`)
   - Manages active department state
   - Persists selection in localStorage
   - Provides `useDepartment()` hook for components
   - Handles department list loading and caching

2. **Global Department Selector** (`frontend/src/components/layout/TopNav.jsx`)
   - Dropdown in top navigation bar
   - Shows current active department
   - Allows switching between departments
   - "All Departments" option for cross-department views

3. **API Client Integration** (`frontend/src/api/client.js`)
   - Automatically includes `departmentId` in API requests when available
   - Supports both header-based and query parameter-based department filtering
   - Graceful fallback when no department is selected

## Implementation Pattern

### Standard Pattern for Pages

```javascript
import { useDepartment } from '../../contexts/DepartmentContext';

const MyPage = () => {
  const { activeDepartment } = useDepartment();
  
  useEffect(() => {
    loadData();
  }, [activeDepartment]); // Re-fetch when department changes
  
  const loadData = async () => {
    const filters = {
      ...otherFilters,
      ...(activeDepartment ? { departmentId: activeDepartment.id } : {})
    };
    const data = await getData(filters);
    // ...
  };
};
```

### Standard Pattern for API Calls

```javascript
// In API functions
export const getData = async (filters = {}) => {
  // departmentId is automatically included if activeDepartment is set
  const response = await apiClient.get('/endpoint', { params: filters });
  return response;
};
```

## Modules Updated

### Phase 1: Core Infrastructure ✅
- [x] DepartmentContext + Provider
- [x] Global department selector in TopNav
- [x] Landing page preference system
- [x] API client department integration

### Phase 2: High-Usage Modules ✅
- [x] **Orders**
  - OrdersListPage - filters by department
  - OrderDetailPage - shows department badge
  - CreateOrderPage - defaults to active department
  - OrderFilters - includes department dropdown
  
- [x] **Scheduler**
  - CalendarPage - filters slots by department
  - SIAvailabilityPage - filters SI availability by department
  
- [x] **Dashboard**
  - DashboardPage - all stats and orders filtered by department

### Phase 3: Additional Modules ✅
- [x] **Inventory**
  - InventoryListPage - materials and stock filtered by department
  - InventoryDashboardPage - all KPIs and movements scoped to department
  
- [x] **Assets**
  - AssetsListPage - assets filtered by department
  - AssetsDashboardPage - summary and maintenance filtered by department
  - Asset creation defaults to active department

### Phase 4: Financial & Operational ✅
- [x] **P&L**
  - PnlSummaryPage - financial summary filtered by department
  - PnlOrdersPage - order-level P&L filtered by department
  
- [x] **Billing**
  - InvoicesListPage - invoices filtered by department
  
- [x] **RMA**
  - RMAListPage - RMA requests filtered by department

## Testing Guide

### Manual Testing Checklist

1. **Department Selection**
   - [ ] Select a department from top nav dropdown
   - [ ] Verify selection persists after page refresh
   - [ ] Verify "All Departments" option works (shows all data)

2. **Orders Module**
   - [ ] Navigate to Orders list - should show only selected department's orders
   - [ ] Create new order - should default to active department
   - [ ] Check OrderFilters - department dropdown should show current selection
   - [ ] View order detail - should display department badge

3. **Dashboard**
   - [ ] Verify all KPI cards reflect selected department
   - [ ] Verify recent orders table shows only selected department
   - [ ] Change department - stats should update immediately

4. **Inventory**
   - [ ] Materials list filtered by department
   - [ ] Stock levels filtered by department
   - [ ] Stock movements scoped to department
   - [ ] Dashboard KPIs reflect department

5. **Assets**
   - [ ] Assets list filtered by department
   - [ ] Asset creation defaults to active department
   - [ ] Dashboard summary reflects department

6. **Financial Modules**
   - [ ] P&L summary filtered by department
   - [ ] Invoices filtered by department
   - [ ] RMA requests filtered by department

7. **Cross-Module Consistency**
   - [ ] Change department - all pages update automatically
   - [ ] Navigate between pages - department filter persists
   - [ ] Logout/login - department selection persists (if saved)

### Edge Cases to Test

1. **No Department Selected**
   - [ ] Pages should show all departments' data (or appropriate default)
   - [ ] No errors should occur

2. **Department Deleted/Inactive**
   - [ ] System should handle gracefully
   - [ ] Should fallback to "All Departments" or first available

3. **User Permissions**
   - [ ] Users should only see departments they have access to
   - [ ] RBAC should work correctly with department filtering

4. **API Errors**
   - [ ] Network failures should be handled gracefully
   - [ ] Invalid department IDs should not crash the app

## Backend Requirements

### API Endpoints

All relevant endpoints should support `departmentId` as a query parameter:

```
GET /api/orders?departmentId={guid}
GET /api/inventory/materials?departmentId={guid}
GET /api/assets?departmentId={guid}
GET /api/pnl/summary?departmentId={guid}
GET /api/billing/invoices?departmentId={guid}
GET /api/rma/requests?departmentId={guid}
```

### Backend Filtering

Backend services should:
1. Accept `departmentId` parameter
2. Filter results by department when provided
3. Return all departments' data when `departmentId` is null/empty (for "All Departments" view)
4. Respect user permissions (users may only access certain departments)

## Configuration

### Storage Keys

- `cephasops.activeDepartmentId` - Stores selected department ID
- `cephasops.landingPageRoute` - Stores user's preferred landing page

### Environment Variables

No additional environment variables required. Department filtering works with existing API configuration.

## Troubleshooting

### Department Not Filtering

1. Check that `DepartmentContext` is properly wrapped around the app
2. Verify `useDepartment()` hook is being used in the component
3. Check browser console for errors
4. Verify API endpoint accepts `departmentId` parameter
5. Check network tab to see if `departmentId` is being sent

### Department Selection Not Persisting

1. Check localStorage for `cephasops.activeDepartmentId`
2. Verify DepartmentContext is loading from localStorage on mount
3. Check browser console for storage errors

### Performance Issues

1. Department changes trigger re-fetches - this is expected
2. Consider implementing query caching (TanStack Query) for better performance
3. Large datasets may need pagination regardless of department filter

## Future Enhancements

### Potential Improvements

1. **Query Caching**
   - Implement TanStack Query for automatic caching
   - Reduce redundant API calls when switching departments

2. **Department-Specific Dashboards**
   - Customizable widgets per department
   - Department-specific KPIs

3. **Bulk Operations**
   - Department-aware bulk actions
   - Multi-department operations for admins

4. **Advanced Filtering**
   - Multi-select department filter
   - Department comparison views
   - Cross-department analytics

5. **User Preferences**
   - Per-user default department
   - Remember last viewed department per module
   - Department-specific landing pages

## Migration Notes

### For Developers

When adding new pages or modules:

1. Import `useDepartment` hook
2. Include `activeDepartment` in dependency arrays
3. Pass `departmentId` to API calls when appropriate
4. Test with different department selections
5. Handle "All Departments" case appropriately

### For Backend Developers

When creating new endpoints:

1. Accept optional `departmentId` query parameter
2. Filter by department when provided
3. Return all data when not provided (or based on user permissions)
4. Document department filtering behavior in API docs

## Support

For issues or questions:
- Check this documentation first
- Review component code in `frontend/src/contexts/DepartmentContext.jsx`
- Check API client implementation in `frontend/src/api/client.js`
- Review example implementations in updated pages

---

**Last Updated:** December 2025
**Status:** ✅ Complete - All phases implemented and tested

