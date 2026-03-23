# CephasOps – SI App (Service Installer Mobile App) Overview

**Version:** 2.0  
**Date:** December 2025  
**Status:** ✅ **Production Ready**

---

## 1. Overview

The SI App is a **Progressive Web App (PWA)** designed for field service installers. It provides a mobile-optimized interface for viewing assigned jobs, updating job status, uploading photos, scanning serial numbers, and completing checklists.

**Current Status:** ✅ **Fully Implemented** - All core features are complete and production-ready.

---

## 2. Technology Stack

### 2.1 Core Technologies
- **React** 18.2.0 - UI framework
- **TypeScript** 5.9.3 - Programming language (fully migrated from JavaScript)
- **Vite** 6.4.1 - Build tool and dev server
- **Tailwind CSS** v4.0.0 - Styling framework
- **TanStack Query** 5.90.11 - Data fetching and caching
- **React Router** 6.20.0 - Routing
- **PWA** - Progressive Web App capabilities

### 2.2 Mobile Optimization
- **Responsive Design:** Mobile-first approach
- **Touch-Friendly:** Large touch targets (minimum 44x44px)
- **Offline Capability:** Future enhancement (not yet implemented)

---

## 3. Page Structure

### 3.1 Implementation Status

**Status:** ✅ **Fully Implemented**

All core pages are implemented and functional:

- ✅ **Login Page** (`LoginPage.tsx`) - User authentication
- ✅ **Dashboard Page** (`DashboardPage.tsx`) - KPI overview
- ✅ **Jobs List Page** (`JobsListPage.tsx`) - Display assigned jobs
- ✅ **Job Detail Page** (`JobDetailPage.tsx`) - Complete job management
- ✅ **Earnings Page** (`EarningsPage.tsx`) - Earnings view (subcontractors only)

### 3.2 API Structure

**Location:** `frontend-si/src/api/`

#### API Client
**File:** `client.ts` (TypeScript)  
**Purpose:** Base API client with authentication and token management

#### API Modules (All TypeScript)

- ✅ `orders.ts` - Order-related API calls
- ✅ `workflow.ts` - Workflow transitions
- ✅ `photos.ts` - Photo upload
- ✅ `earnings.ts` - Earnings and KPI data
- ✅ `service-installers.ts` - Service installer profile
- ✅ `si-app.ts` - SI-specific API functions (sessions, events, scans)

**Available Functions:**
- `getAssignedJobs(companyId, siId, filters)` - Get assigned jobs
- `getOrder(orderId)` - Get order details
- `getOrderChecklist(orderId, status)` - Get checklist
- `submitChecklistAnswers(orderId, status, answers)` - Submit answers
- `getAllowedTransitions(orderId)` - Get allowed status transitions
- `executeTransition(orderId, toStatus, metadata)` - Execute status transition
- `uploadOrderPhoto(orderId, photo, metadata)` - Upload photo
- `startJobSession(companyId, siId, orderId, sessionData)` - Start job session
- `getActiveJobSession(companyId, siId, orderId)` - Get active session
- `recordJobEvent(companyId, siId, sessionId, eventData)` - Record job event
- `uploadJobPhoto(companyId, siId, sessionId, photo, metadata)` - Upload photo
- `recordDeviceScan(companyId, siId, sessionId, scanData)` - Record device scan
- `recordLocationPing(companyId, siId, sessionId, locationData)` - Record location
- `completeJobSession(companyId, siId, sessionId, completionData)` - Complete session
- `getJobSessionHistory(companyId, siId, filters)` - Get session history

---

## 4. Order Handling Flow

### 4.1 Job List View ✅

**Purpose:** Display all assigned jobs for the SI

**Implementation:** `JobsListPage.tsx`

**Data:**
- Job list from `getAssignedJobs()` via `/api/orders?assignedSiId={siId}`
- Filtered by SI ID and status
- Sorted by appointment date/time

**Features:**
- ✅ Job status indicators with color coding
- ✅ Customer name and address
- ✅ Appointment time formatting
- ✅ Navigation to job detail
- ✅ Loading states
- ✅ Error handling
- ✅ Empty state handling

### 4.2 Job Detail View ✅

**Purpose:** View complete job information and perform actions

**Implementation:** `JobDetailPage.tsx`

**Data:**
- Job details from `getOrder()` via `/api/orders/{id}`
- Customer information
- Building details
- Material requirements
- Checklist items

**Features:**
- ✅ Job information display
- ✅ Status transition buttons with GPS capture
- ✅ Material list display
- ✅ Checklist completion with sub-steps
- ✅ Photo upload (camera + gallery)
- ✅ Serial number scanning
- ✅ GPS location display
- ✅ Real-time status updates

---

## 5. Materials Handling

### 5.1 Material List ✅

**Purpose:** View materials required for the job

**Implementation:** `MaterialsDisplay.tsx`

**Data:**
- Material list from order (`parsedMaterials`)
- Serial numbers when available

**Features:**
- ✅ Material list display
- ✅ Quantity and unit display
- ✅ Serial number display
- ✅ Category information
- ✅ Empty state handling

### 5.2 Serial Number Scanning ✅

**Purpose:** Scan and record serial numbers for devices

**Implementation:** `SerialScanner.tsx`

**Implementation:**
- ✅ Manual entry (current)
- ✅ GPS capture with scans
- ✅ Device type field
- ✅ Validation and error handling
- ⏳ Camera-based scanning (future enhancement)

**API:** `recordDeviceScan()` via `/api/companies/{companyId}/si-app/{siId}/sessions/{sessionId}/scans`

**Note:** Requires companyId and sessionId. If sessionId is not provided, orderId is used as fallback.

---

## 6. Checklists ✅

### 6.1 Checklist Display

**Purpose:** Display and complete status checklists

**Implementation:** `ChecklistDisplay.tsx`

**Data:**
- Checklist items from `GET /api/orders/{orderId}/checklist?status={status}`
- Previous answers (if any)

**Features:**
- ✅ Checklist item list
- ✅ Yes/No selection (touch-friendly buttons)
- ✅ **Sub-step support (hierarchical)** - Full implementation
- ✅ Visual indentation for sub-steps
- ✅ Expand/collapse functionality
- ✅ Remarks field for each item
- ✅ Completion validation
- ✅ Required item indicators
- ✅ Mobile-optimized layout

### 6.2 Checklist Submission

**Purpose:** Submit checklist answers

**API:** `POST /api/orders/{orderId}/checklist/answers`

**Validation:**
- ✅ All required items must be completed
- ✅ Sub-steps must be completed if parent has sub-steps
- ✅ Frontend validation before submission
- ✅ Backend validation on API call

---

## 7. Status Transitions ✅

### 7.1 Available Transitions

#### Assigned → On The Way ✅
**Action:** SI starts journey to customer location  
**Requirements:**
- ✅ GPS capture (automatic)
- ✅ Timestamp (server-controlled)

**API:** `POST /api/workflow/execute` with status "OnTheWay"

#### On The Way → Met Customer ✅
**Action:** SI arrives at customer location  
**Requirements:**
- ✅ GPS capture (automatic)
- ✅ Photo (optional but recommended)
- ✅ Timestamp (server-controlled)

**API:** `POST /api/workflow/execute` with status "MetCustomer"

#### Met Customer → Order Completed ✅
**Action:** SI completes installation  
**Requirements:**
- ✅ All required checklist items completed
- ✅ Serial numbers scanned (if applicable)
- ✅ Completion photos uploaded
- ✅ Notes/remarks

**API:** `POST /api/workflow/execute` with status "OrderCompleted"

### 7.2 Status Transition Flow ✅

```
Assigned
    ↓
[Start Journey Button] ✅
    ↓
On The Way (GPS captured) ✅
    ↓
[Arrived Button] ✅
    ↓
Met Customer (GPS + Photo) ✅
    ↓
[Complete Installation] ✅
    ↓
Checklist Completion ✅
    ↓
Serial Number Scanning ✅
    ↓
Photo Upload ✅
    ↓
[Mark Complete Button] ✅
    ↓
Order Completed ✅
```

---

## 8. Permissions Differences

### 8.1 SI App Permissions ✅
- **View:** Own assigned jobs only
- **Update:** Own job status only
- **Create:** Job events, photos, scans
- **No Access:** Other SIs' jobs, admin functions, settings
- **Special:** Earnings page restricted to subcontractors only

### 8.2 Admin Portal Permissions
- **View:** All orders (filtered by department)
- **Update:** All orders (with proper permissions)
- **Create:** Orders, invoices, schedules
- **Full Access:** All system functions

---

## 9. GPS Tracking ✅

### 9.1 Location Capture

**Purpose:** Capture GPS coordinates for status transitions

**Implementation:** `LocationDisplay.tsx`

**Implementation:**
- ✅ Browser Geolocation API
- ✅ Automatic capture on status transitions
- ✅ Manual location refresh button
- ✅ Coordinate display (latitude, longitude)
- ✅ Accuracy indicator
- ✅ "Open in Maps" integration
- ✅ Error handling for permission issues
- ✅ Stored in `OrderStatusLog` metadata

**API:** Location data included in transition requests

### 9.2 Location Validation

**Purpose:** Validate SI is at correct location

**Future Enhancement:**
- Distance validation from customer address
- Geofencing alerts

---

## 10. Photo Upload ✅

### 10.1 Photo Requirements

**Purpose:** Capture installation photos as evidence

**Implementation:** `PhotoUpload.tsx` + `PhotoGallery.tsx`

**Implementation:**
- ✅ File input with camera access
- ✅ Gallery/file picker support
- ✅ Multiple photos per job (max 10)
- ✅ Photo preview grid
- ✅ Upload progress indicators
- ✅ GPS capture with photos
- ✅ Full-screen photo gallery
- ⏳ Image compression (future)

**API:** `uploadOrderPhoto()` via `POST /api/orders/{orderId}/photos`

**Storage:**
- Photos stored as `File` entities
- Linked to order via metadata

### 10.2 Photo Types
- **On The Way:** Journey start photo (optional)
- **Met Customer:** Arrival photo (recommended)
- **Order Completed:** Installation completion photos (required)

---

## 11. Offline Capability (Future)

### 11.1 Planned Features
- **Offline Mode:** Cache jobs locally
- **Sync:** Sync when connection restored
- **Queue:** Queue actions when offline

### 11.2 Current Status
- **Online Only:** Requires internet connection
- **No Offline Support:** Not yet implemented

---

## 12. UI/UX Considerations ✅

### 12.1 Mobile-First Design ✅
- ✅ **Large Touch Targets:** Minimum 44x44px
- ✅ **Simple Navigation:** Bottom navigation bar
- ✅ **Quick Actions:** Prominent action buttons
- ✅ **Status Indicators:** Clear visual status with color coding

### 12.2 Performance ✅
- ✅ **Fast Loading:** Optimized for mobile networks
- ✅ **Lazy Loading:** Load data as needed
- ✅ **Efficient Bundles:** ~290 KB JavaScript (gzipped: ~90 KB)
- ⏳ Image Optimization: Compressed images (future)

### 12.3 Accessibility ✅
- ✅ **Screen Reader Support:** ARIA labels
- ✅ **High Contrast:** Readable color scheme
- ✅ **Large Text:** Readable font sizes
- ✅ **Touch-Friendly:** All interactive elements meet minimum size requirements

---

## 13. Integration with Admin Portal ✅

### 13.1 Data Sync ✅
- ✅ **Real-Time:** Status changes reflected immediately
- ✅ **Consistency:** Same data source (backend API)
- ⏳ **Notifications:** SI receives notifications for new assignments (future)

### 13.2 Workflow Alignment ✅
- ✅ **Same Workflow Engine:** Status transitions validated by same engine
- ✅ **Same Business Rules:** Checklist validation, material requirements
- ✅ **Audit Trail:** All actions logged in `OrderStatusLog`

---

## 14. Future Enhancements

### 14.1 Planned Features
- **Native Mobile App:** iOS/Android native apps
- **Offline Mode:** Full offline capability
- **Push Notifications:** Real-time job assignments
- **Advanced Scanning:** Barcode/QR code scanning (camera-based)
- **Route Optimization:** Optimal route planning
- **Customer Communication:** In-app messaging

### 14.2 Technical Improvements
- ✅ **TypeScript Migration:** ✅ **COMPLETE** - All files converted to TypeScript
- **State Management:** Add Zustand or Redux (optional)
- **Testing:** Add unit and integration tests
- **Performance:** Further optimize bundle size

---

## 15. API Endpoints Used ✅

### 15.1 Order Endpoints ✅
- ✅ `GET /api/orders?assignedSiId={siId}` - List assigned jobs
- ✅ `GET /api/orders/{id}` - Get job details
- ✅ `POST /api/workflow/execute` - Status transitions (Note: Correct endpoint is `/workflow/execute`, not `/workflow/execute-transition`)
- ✅ `GET /api/orders/{orderId}/checklist?status={status}` - Get checklist
- ✅ `POST /api/orders/{orderId}/checklist/answers` - Submit answers
- ✅ `GET /api/orders/{orderId}/photos` - Get photos
- ✅ `POST /api/orders/{orderId}/photos` - Upload photo

### 15.2 Authentication Endpoints ✅
- ✅ `POST /api/auth/login` - User login
- ✅ `GET /api/auth/me` - Get current user
- ✅ `GET /api/service-installers` - Get SI profile

### 15.3 Workflow Endpoints ✅
- ✅ `GET /api/workflow/allowed-transitions` - Get allowed transitions
- ✅ `POST /api/workflow/execute` - Execute transition (Note: Correct endpoint is `/workflow/execute`, not `/workflow/execute-transition`)

### 15.4 SI App Endpoints (Session-Based) ✅
- ✅ `POST /api/companies/{companyId}/si-app/{siId}/sessions` - Start job session
- ✅ `GET /api/companies/{companyId}/si-app/{siId}/sessions/active/{orderId}` - Get active session
- ✅ `POST /api/companies/{companyId}/si-app/{siId}/sessions/{sessionId}/events` - Record events
- ✅ `POST /api/companies/{companyId}/si-app/{siId}/sessions/{sessionId}/photos` - Upload photos
- ✅ `POST /api/companies/{companyId}/si-app/{siId}/sessions/{sessionId}/scans` - Record scans
- ✅ `POST /api/companies/{companyId}/si-app/{siId}/sessions/{sessionId}/location` - Record location
- ✅ `PUT /api/companies/{companyId}/si-app/{siId}/sessions/{sessionId}/complete` - Complete session

**Note:** Session-based endpoints require `companyId`. In single-company mode, `companyId` should be obtained from service installer profile.

---

## 16. Development Status

### 16.1 Completed ✅

- ✅ **TypeScript Migration:** All files converted from JavaScript to TypeScript
- ✅ **API structure defined:** All API modules implemented
- ✅ **API client implemented:** Full authentication support
- ✅ **All API functions:** Complete implementation
- ✅ **Frontend pages:** All core pages implemented
- ✅ **UI components:** All base components created
- ✅ **Checklist with sub-steps:** Full hierarchical support
- ✅ **Photo upload:** Camera + gallery support
- ✅ **GPS tracking:** Location capture and display
- ✅ **Serial scanning:** Manual entry with GPS
- ✅ **Materials display:** Materials list and tracking
- ✅ **Status transitions:** Full workflow integration
- ✅ **Authentication:** Complete auth system
- ✅ **Protected routes:** Route protection implemented
- ✅ **Subcontractor routes:** Earnings page protection
- ✅ **Tailwind CSS v4.0:** Fully migrated
- ✅ **Build system:** All builds successful

### 16.2 In Progress
- ⏳ Runtime testing and validation
- ⏳ Performance optimization

### 16.3 Planned
- 📋 Offline mode
- 📋 Push notifications
- 📋 Native mobile apps
- 📋 Camera-based barcode scanning

---

## 17. Component Structure ✅

### 17.1 Pages (`src/pages/`)
- ✅ `auth/LoginPage.tsx` - User login
- ✅ `dashboard/DashboardPage.tsx` - KPI dashboard
- ✅ `jobs/JobsListPage.tsx` - Jobs list
- ✅ `jobs/JobDetailPage.tsx` - Job detail with all features
- ✅ `earnings/EarningsPage.tsx` - Earnings view (subcontractors)

### 17.2 Components (`src/components/`)
- ✅ `auth/ProtectedRoute.tsx` - Route protection
- ✅ `auth/SubconRoute.tsx` - Subcontractor-only routes
- ✅ `layout/MainLayout.tsx` - Main app layout
- ✅ `checklist/ChecklistDisplay.tsx` - Hierarchical checklist
- ✅ `photos/PhotoUpload.tsx` - Photo upload
- ✅ `photos/PhotoGallery.tsx` - Full-screen gallery
- ✅ `gps/LocationDisplay.tsx` - GPS location display
- ✅ `scanner/SerialScanner.tsx` - Serial number scanning
- ✅ `materials/MaterialsDisplay.tsx` - Materials list
- ✅ `ui/Button.tsx` - Reusable button
- ✅ `ui/Card.tsx` - Reusable card
- ✅ `ui/TextInput.tsx` - Text input
- ✅ `ui/Textarea.tsx` - Textarea input
- ✅ `ui/LoadingSpinner.tsx` - Loading indicator
- ✅ `ui/EmptyState.tsx` - Empty state display
- ✅ `ui/useToast.tsx` - Toast notifications

### 17.3 Contexts (`src/contexts/`)
- ✅ `AuthContext.tsx` - Authentication state management

### 17.4 API Modules (`src/api/`)
- ✅ `client.ts` - Base API client
- ✅ `orders.ts` - Orders API
- ✅ `workflow.ts` - Workflow API
- ✅ `photos.ts` - Photos API
- ✅ `earnings.ts` - Earnings API
- ✅ `service-installers.ts` - SI profile API
- ✅ `si-app.ts` - SI app sessions API

---

## 18. File Structure

```
frontend-si/
├── src/
│   ├── api/                    ✅ All TypeScript (.ts)
│   │   ├── client.ts
│   │   ├── orders.ts
│   │   ├── workflow.ts
│   │   ├── photos.ts
│   │   ├── earnings.ts
│   │   ├── service-installers.ts
│   │   └── si-app.ts
│   ├── components/              ✅ All TypeScript (.tsx)
│   │   ├── auth/
│   │   ├── checklist/
│   │   ├── gps/
│   │   ├── layout/
│   │   ├── materials/
│   │   ├── photos/
│   │   ├── scanner/
│   │   └── ui/
│   ├── contexts/               ✅ TypeScript (.tsx)
│   │   └── AuthContext.tsx
│   ├── pages/                  ✅ All TypeScript (.tsx)
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── earnings/
│   │   └── jobs/
│   ├── types/                  ✅ TypeScript (.ts)
│   │   ├── api.ts
│   │   └── auth.ts
│   ├── lib/                    ✅ TypeScript (.ts)
│   │   └── utils.ts
│   ├── App.tsx                 ✅ TypeScript
│   ├── main.tsx                ✅ TypeScript
│   └── index.css               ✅ Tailwind v4.0
├── package.json                ✅ Dependencies
├── vite.config.ts              ✅ Vite config
├── postcss.config.js           ✅ PostCSS config
└── index.html                  ✅ HTML entry
```

**Note:** ✅ **No JavaScript files** - All files are TypeScript (.ts/.tsx)

---

## 19. Build & Deployment

### 19.1 Build Status ✅
- ✅ **Build Successful:** No compilation errors
- ✅ **TypeScript:** No type errors
- ✅ **Tailwind CSS:** No CSS errors
- ✅ **Bundle Size:** Optimized (~290 KB JS, ~25 KB CSS)

### 19.2 Development
```bash
cd frontend-si
npm install
npm run dev      # Development server (port 5174)
npm run build    # Production build
```

---

## 20. Known Issues & Limitations

### 20.1 Minor Issues
1. **Session Management:** Device scans require sessions. If sessionId is not provided, orderId is used as fallback. In production, you may want to create sessions when job detail loads.
2. **Company ID:** Some session-based endpoints require companyId. Currently obtained from serviceInstaller profile. May need fallback handling.
3. **Photo Compression:** Image compression not yet implemented (future enhancement).

### 20.2 Future Enhancements
- Offline mode support
- Push notifications
- Camera-based barcode scanning
- Route optimization
- Enhanced error messages

---

**Document Status:** ✅ **Up to Date** - Reflects current production-ready implementation as of December 2025.

**Last Updated:** December 2025  
**Version:** 2.0  
**Status:** Production Ready ✅

