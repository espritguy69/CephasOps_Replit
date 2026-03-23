# External Portals & Favorites Module

CephasOps External Portals & Favorites System – Full Specification

**Date:** December 8, 2025  
**Status:** 📋 Specification Complete - Ready for Implementation  
**Module:** User Preferences & Quick Access

---

## 1. Purpose

The External Portals & Favorites Module provides a personalized bookmark system for CephasOps users (clerks, admins, managers) to quickly access external portals and tools they use in their daily work. This eliminates the need for browser bookmarks and provides integrated access to partner portals, government systems, and other external tools directly from within CephasOps.

**Use Cases:**
- Clerks need quick access to TIME Portal, MyInvois, Digi Partner Portal
- Managers need access to reporting dashboards, partner systems
- Admins need access to configuration portals, partner admin panels
- Users want personalized shortcuts to their most-used external tools

---

## 2. Overview

The External Portals system supports:
- **Personal Favorites**: Each user maintains their own list of favorite external links
- **Categorization**: Organize links by category (Partner Portals, Government, Tools, etc.)
- **Quick Access**: Sidebar widget for instant access to most-used portals
- **Context-Aware Links**: Future enhancement to open portals with pre-filled data (e.g., Order ID)
- **Flexible Organization**: Drag-to-reorder, categories, search/filter

---

## 3. Data Model

### 3.1 ExternalPortalLink Entity

Represents a user's favorite external portal link.

**Location:** `backend/src/CephasOps.Domain/Users/Entities/ExternalPortalLink.cs`

**Key Fields:**
- `Id` (Guid) - Primary key
- `UserId` (Guid) - Owner of this favorite (required, user-scoped)
- `Name` (string) - Display name (e.g., "TIME Portal", "MyInvois")
- `Url` (string) - Full URL (required, validated)
- `Icon` (string?) - Icon identifier (lucide-react icon name or custom URL)
- `Category` (string?) - Category for grouping (e.g., "Partner Portals", "Government", "Tools")
- `Description` (string?) - Optional notes/description
- `DisplayOrder` (int) - Sort order (default: 0, lower = higher priority)
- `IsActive` (bool) - Whether this link is active (default: true)
- `ClickCount` (int) - Usage tracking (default: 0, for future "most used" feature)
- `LastClickedAt` (DateTime?) - Last time this link was clicked
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp

**Business Rules:**
1. Each user can have unlimited favorites
2. URLs must be valid HTTP/HTTPS URLs
3. DisplayOrder determines sort order (ascending)
4. ClickCount and LastClickedAt are updated automatically when link is accessed
5. Soft delete support (IsActive flag, not hard delete)

**Indexes:**
- `IX_ExternalPortalLinks_UserId_IsActive_DisplayOrder` - For efficient querying of user's active favorites
- `IX_ExternalPortalLinks_UserId_Category` - For category filtering

**See:** `docs/05_data_model/entities/external_portal_link_entity.md` (to be created)

---

## 4. Backend Architecture

### 4.1 Domain Layer

**Entity:**
- `ExternalPortalLink.cs` - Domain entity (user-scoped)

**Value Objects:**
- None required (simple entity)

**Domain Events:**
- None required for MVP (future: `ExternalPortalLinkCreated`, `ExternalPortalLinkClicked`)

### 4.2 Application Layer

**Services:**
- `IExternalPortalService.cs` - Service interface
- `ExternalPortalService.cs` - Service implementation

**DTOs:**
- `ExternalPortalLinkDto.cs` - Full portal link data
- `CreateExternalPortalLinkDto.cs` - Create request
- `UpdateExternalPortalLinkDto.cs` - Update request
- `ReorderExternalPortalLinksDto.cs` - Bulk reorder request

**Service Methods:**
```csharp
Task<List<ExternalPortalLinkDto>> GetUserFavoritesAsync(Guid userId, string? category = null, CancellationToken cancellationToken = default);
Task<ExternalPortalLinkDto?> GetFavoriteByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
Task<ExternalPortalLinkDto> CreateFavoriteAsync(CreateExternalPortalLinkDto dto, Guid userId, CancellationToken cancellationToken = default);
Task<ExternalPortalLinkDto> UpdateFavoriteAsync(Guid id, UpdateExternalPortalLinkDto dto, Guid userId, CancellationToken cancellationToken = default);
Task DeleteFavoriteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
Task ReorderFavoritesAsync(List<ReorderExternalPortalLinksDto> reorderItems, Guid userId, CancellationToken cancellationToken = default);
Task RecordClickAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
```

### 4.3 Infrastructure Layer

**EF Core Configuration:**
- `ExternalPortalLinkConfiguration.cs` - Entity configuration
  - Table name: `external_portal_links` (snake_case)
  - Indexes as defined above
  - URL validation constraints

**DbContext:**
- Add `DbSet<ExternalPortalLink> ExternalPortalLinks` to `ApplicationDbContext`

### 4.4 API Layer

**Controller:**
- `ExternalPortalsController.cs` - REST API endpoints

**Endpoints:**
```
GET    /api/external-portals/my                    → Get current user's favorites
GET    /api/external-portals/my?category={cat}     → Get favorites by category
GET    /api/external-portals/{id}                   → Get single favorite
POST   /api/external-portals                       → Create new favorite
PUT    /api/external-portals/{id}                  → Update favorite
DELETE /api/external-portals/{id}                  → Delete favorite
PATCH  /api/external-portals/reorder               → Bulk reorder favorites
POST   /api/external-portals/{id}/click            → Record click (usage tracking)
```

**Response Format:**
- Follows standard CephasOps API response envelope:
```json
{
  "success": true,
  "message": "Favorites retrieved successfully",
  "data": [...],
  "errors": []
}
```

---

## 5. Frontend Architecture

### 5.1 Pages

**Main Favorites Page:**
- **Route:** `/external-portals` or `/portals`
- **File:** `frontend/src/pages/portals/ExternalPortalsPage.tsx`
- **Features:**
  - Grid/card layout showing all favorites
  - Category tabs/filters
  - "Add New Portal" button
  - Drag-to-reorder (Phase 2)
  - Search/filter (Phase 2)
  - Quick actions: Edit, Delete, Open in New Tab

**Add/Edit Modal:**
- **File:** `frontend/src/pages/portals/AddEditPortalModal.tsx`
- **Fields:**
  - Name (required, text input)
  - URL (required, validated URL input)
  - Category (dropdown with common categories + "Custom")
  - Icon (icon picker using lucide-react icons)
  - Description (optional, textarea)

### 5.2 Components

**Quick Links Widget:**
- **File:** `frontend/src/components/portals/QuickLinksWidget.tsx`
- **Location:** Sidebar (System section) or Header
- **Features:**
  - Shows top 5-8 most-used favorites (by ClickCount or DisplayOrder)
  - "View All" link to main page
  - Click to open in new tab
  - Collapsible section
  - Empty state when no favorites

**Portal Card:**
- **File:** `frontend/src/components/portals/PortalCard.tsx`
- **Features:**
  - Icon display
  - Name and description
  - Category badge
  - Click to open in new tab
  - Hover actions: Edit, Delete

### 5.3 API Integration

**API Client:**
- **File:** `frontend/src/api/externalPortals.ts`
- **Functions:**
  - `getUserFavorites(category?)` - GET /api/external-portals/my
  - `getFavorite(id)` - GET /api/external-portals/{id}
  - `createFavorite(data)` - POST /api/external-portals
  - `updateFavorite(id, data)` - PUT /api/external-portals/{id}
  - `deleteFavorite(id)` - DELETE /api/external-portals/{id}
  - `reorderFavorites(items)` - PATCH /api/external-portals/reorder
  - `recordClick(id)` - POST /api/external-portals/{id}/click

**React Hooks:**
- **File:** `frontend/src/hooks/useExternalPortals.ts`
- **Hooks:**
  - `useExternalPortals(category?)` - TanStack Query hook
  - `useCreateExternalPortal()` - Mutation hook
  - `useUpdateExternalPortal()` - Mutation hook
  - `useDeleteExternalPortal()` - Mutation hook
  - `useReorderExternalPortals()` - Mutation hook
  - `useRecordPortalClick()` - Mutation hook

### 5.4 Navigation Integration

**Sidebar Menu:**
- Add to "System" section:
  ```tsx
  {
    title: 'External Portals',
    path: '/external-portals',
    icon: ExternalLink,
    permission: null // Available to all authenticated users
  }
  ```

**Quick Access Widget:**
- Add `QuickLinksWidget` component to Sidebar
- Position: Below main navigation, above Settings
- Or: Add to Header as dropdown menu

---

## 6. UI/UX Design

### 6.1 Main Page Layout

```
┌─────────────────────────────────────────────────────────┐
│ External Portals & Favorites                           │
│ [+ Add New Portal]                    [Search...]       │
├─────────────────────────────────────────────────────────┤
│ [All] [Partner Portals] [Government] [Tools] [Custom]  │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐   │
│ │ 🌐 TIME      │ │ 📄 MyInvois  │ │ 📱 Digi       │   │
│ │ Portal       │ │ Portal       │ │ Portal        │   │
│ │              │ │              │ │              │   │
│ │ portal.time  │ │ myinvois...  │ │ partner.digi │   │
│ │ .com.my      │ │ hasil.gov.my │ │ .com         │   │
│ │              │ │              │ │              │   │
│ │ [Open] [⚙️]  │ │ [Open] [⚙️]  │ │ [Open] [⚙️]  │   │
│ └──────────────┘ └──────────────┘ └──────────────┘   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 6.2 Quick Links Widget (Sidebar)

```
┌─────────────────────┐
│ 🌐 Quick Links      │
├─────────────────────┤
│ 🔗 TIME Portal      │
│ 📄 MyInvois         │
│ 📱 Digi Portal      │
│ 🔧 Tools Portal     │
│ ...                 │
│                     │
│ [View All →]        │
└─────────────────────┘
```

### 6.3 Add/Edit Modal

```
┌─────────────────────────────────┐
│ Add External Portal              │
├─────────────────────────────────┤
│ Name *                           │
│ [TIME Portal____________]        │
│                                  │
│ URL *                            │
│ [https://portal.time.com.my]     │
│                                  │
│ Category                         │
│ [Partner Portals ▼]              │
│                                  │
│ Icon                             │
│ [🌐] [Select Icon...]            │
│                                  │
│ Description (optional)            │
│ [Quick access to TIME...]        │
│                                  │
│ [Cancel] [Save]                  │
└─────────────────────────────────┘
```

---

## 7. Implementation Phases

### Phase 1: MVP (Minimum Viable Product)

**Backend:**
- ✅ Create `ExternalPortalLink` entity
- ✅ Create EF Core configuration
- ✅ Create Application service (CRUD operations)
- ✅ Create API controller with basic endpoints
- ✅ Add to DbContext and create migration

**Frontend:**
- ✅ Create main External Portals page
- ✅ Create Add/Edit modal
- ✅ Create API client and hooks
- ✅ Create PortalCard component
- ✅ Add to navigation menu
- ✅ Basic category filtering

**Testing:**
- ✅ Test CRUD operations
- ✅ Test URL validation
- ✅ Test user isolation (users can only see their own favorites)
- ✅ Test opening links in new tab

**Timeline:** 2-3 days

---

### Phase 2: Enhanced Features

**Features:**
- 🔄 Drag-to-reorder favorites
- 🔄 Usage tracking (ClickCount, LastClickedAt)
- 🔄 "Most Used" sorting option
- 🔄 Search/filter functionality
- 🔄 Bulk operations (delete multiple, reorder)
- 🔄 Icon picker with lucide-react icons
- 🔄 Pre-configured portal templates (TIME, MyInvois, etc.)
- 🔄 URL validation and favicon fetching

**Timeline:** 2-3 days

---

### Phase 3: Advanced Features

**Features:**
- 🔄 Quick Links Widget in Sidebar
- 🔄 Context-aware links (e.g., "Open Order #123 in TIME Portal")
- 🔄 Share favorites with team (optional company-wide favorites)
- 🔄 Import/Export favorites (JSON)
- 🔄 Bookmark sync across devices (future)
- 🔄 Analytics dashboard (most-used portals, click statistics)

**Timeline:** 3-5 days

---

## 8. Pre-Configured Portal Templates

For convenience, provide pre-configured templates that users can add with one click:

**Partner Portals:**
- TIME Portal: `https://portal.time.com.my`
- Digi Partner Portal: `https://partner.digi.com`
- Celcom Partner Portal: `https://partner.celcom.com.my`
- Maxis Partner Portal: `https://partner.maxis.com.my`

**Government:**
- MyInvois: `https://myinvois.hasil.gov.my`
- LHDN Portal: `https://www.hasil.gov.my`
- SSM Portal: `https://www.ssm.com.my`

**Tools:**
- Google Maps: `https://maps.google.com`
- WhatsApp Web: `https://web.whatsapp.com`
- Email (Gmail/Outlook): User-specific

---

## 9. Security Considerations

1. **URL Validation:**
   - Only allow HTTP/HTTPS URLs
   - Validate URL format before saving
   - Prevent XSS attacks (sanitize URLs)

2. **User Isolation:**
   - Users can only see/modify their own favorites
   - API endpoints must verify `UserId` matches authenticated user
   - No cross-user data access

3. **Rate Limiting:**
   - Limit click tracking to prevent abuse
   - Limit number of favorites per user (optional, e.g., 50 max)

4. **External Link Warnings:**
   - Show warning when opening external links
   - Use `target="_blank" rel="noopener noreferrer"` for security

---

## 10. Testing Checklist

### Backend Testing:
- [ ] Create favorite (valid URL)
- [ ] Create favorite (invalid URL - should fail)
- [ ] Get user's favorites (should only return current user's)
- [ ] Update favorite
- [ ] Delete favorite
- [ ] Reorder favorites
- [ ] Record click (usage tracking)
- [ ] Category filtering
- [ ] User isolation (user A cannot see user B's favorites)

### Frontend Testing:
- [ ] Main page loads and displays favorites
- [ ] Add new portal modal works
- [ ] Edit portal modal works
- [ ] Delete portal (with confirmation)
- [ ] Open link in new tab
- [ ] Category filtering
- [ ] Search/filter (Phase 2)
- [ ] Drag-to-reorder (Phase 2)
- [ ] Quick Links Widget displays correctly
- [ ] Empty state when no favorites
- [ ] URL validation in form

### Integration Testing:
- [ ] End-to-end: Add favorite → See in list → Click to open
- [ ] End-to-end: Add favorite → Edit → Save → Verify changes
- [ ] End-to-end: Add favorite → Delete → Verify removed
- [ ] Quick Links Widget updates when favorites change

---

## 11. Future Enhancements

### 11.1 Context-Aware Links
Allow links to include placeholders that get replaced with context data:
- `https://portal.time.com.my/orders/{orderId}` → Opens with Order ID pre-filled
- `https://myinvois.hasil.gov.my/invoice/{invoiceId}` → Opens with Invoice ID

**Implementation:**
- Add `UrlTemplate` field to entity
- Add `ContextVariables` JSON field
- Frontend replaces placeholders before opening

### 11.2 Team/Company Favorites
Allow admins to create company-wide favorites that all users can see:
- Add `IsCompanyWide` flag
- Add `CompanyId` field (nullable)
- Show both personal and company favorites

### 11.3 Portal Analytics
Track and display usage statistics:
- Most-used portals
- Click frequency
- Time of day usage patterns
- User-specific analytics dashboard

### 11.4 Smart Suggestions
Suggest portals based on:
- User's role (clerks → partner portals, admins → admin portals)
- User's department (GPON → TIME portal, CWO → different portals)
- Most-used by similar users

### 11.5 Mobile App Integration
- Sync favorites to mobile app
- Quick access from mobile
- Offline access to favorite list

---

## 12. Related Documentation

- **Data Model:** `docs/05_data_model/entities/external_portal_link_entity.md` (to be created)
- **API Contracts:** `docs/04_api/external_portals_api.md` (to be created)
- **Frontend UI:** `docs/07_frontend/ui/external_portals_ui.md` (to be created)
- **User Preferences:** `docs/02_modules/notifications/OVERVIEW.md` (similar user-scoped pattern)

---

## 13. Implementation Status

| Component | Status | Completeness |
|-----------|--------|--------------|
| Specification | ✅ Complete | 100% |
| Domain Entity | ⏳ Pending | 0% |
| Application Service | ⏳ Pending | 0% |
| API Controller | ⏳ Pending | 0% |
| Frontend Page | ⏳ Pending | 0% |
| Quick Links Widget | ⏳ Pending | 0% |
| Testing | ⏳ Pending | 0% |

**Overall Status:** 📋 Specification Complete - Ready for Implementation

---

## 14. Next Steps

1. **Review & Approval:**
   - Review this specification with stakeholders
   - Confirm requirements and priorities
   - Approve implementation approach

2. **Implementation Planning:**
   - Create implementation task list
   - Assign priorities (MVP vs Enhancements)
   - Estimate timeline

3. **Begin Implementation:**
   - Start with Phase 1 (MVP)
   - Follow Clean Architecture patterns
   - Create database migration
   - Build backend API
   - Build frontend UI
   - Test thoroughly

4. **Documentation Updates:**
   - Create entity documentation
   - Create API documentation
   - Create UI wireframes (if needed)
   - Update navigation documentation

---

## 15. Questions & Decisions Needed

1. **Route Path:**
   - `/external-portals` or `/portals`? (Recommend: `/external-portals`)

2. **Widget Location:**
   - Sidebar or Header? (Recommend: Sidebar, System section)

3. **Category System:**
   - Pre-defined categories or free-form? (Recommend: Pre-defined + "Custom")

4. **Icon System:**
   - Only lucide-react icons or allow custom URLs? (Recommend: lucide-react only for MVP)

5. **Usage Tracking:**
   - Enable from Phase 1 or Phase 2? (Recommend: Phase 2)

6. **Maximum Favorites:**
   - Unlimited or limit? (Recommend: 50 per user)

---

**Document Version:** 1.0  
**Last Updated:** December 8, 2025  
**Author:** CephasOps Development Team  
**Status:** 📋 Ready for Implementation

