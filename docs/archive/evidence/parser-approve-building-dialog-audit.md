# Audit: “Create Building” dialog on ParsedOrderDraft approve

**Expected:** When approving a draft and Building is not found, a small dialog opens to add a Building; after creating it, approval can succeed.  
**Actual:** Approval fails with “Building not found. Please create or select a building to continue.” and **no dialog** appears.

---

## 1) Approve flow UI

| Item | Location |
|------|----------|
| Parser list page | `frontend/src/pages/parser/ParserListingPage.tsx` |
| Approval from list | Row action **“Review”** → `handleReview` → `navigate(\`/orders/create?draftId=${draft.id}\`)`. No direct Approve on the list; approve happens on Create Order page. |
| Approve action (where API is called) | `frontend/src/pages/orders/CreateOrderPage.tsx`: **`handleApproveDraft`** (lines ~991–1004) |
| API called | `approveParsedOrderDraft(draftId)` from `frontend/src/api/parser.ts` → `POST /parser/drafts/{id}/approve` |

So the **approve flow** that can hit “Building not found” is: **Create Order page** (`/orders/create?draftId=...`) → user clicks Approve → `handleApproveDraft` → `approveParsedOrderDraft(draftId)`.

---

## 2) Building-resolution UX

| Component | File | Used today |
|-----------|------|------------|
| **QuickBuildingModal** | `frontend/src/components/buildings/QuickBuildingModal.tsx` | **CreateOrderPage only**: opened by “Add building” button (line 1594), `setShowBuildingModal(true)`. |
| Props for parser flow | `mode?: 'create-only' \| 'create-and-approve'`, `draftId?: string` | **Not used**: CreateOrderPage passes `mode="create-only"` and does **not** pass `draftId`. |
| Create-and-approve behavior | Button label “Create Building & Approve Order” when `mode === 'create-and-approve'` | Modal only calls `onBuildingCreated(buildingId)` then `onClose()`; it does **not** call the approve API. Parent would need to retry approve in `onBuildingCreated` when in approve flow. |

No other “CreateBuildingDialog”, “AddBuildingDialog”, “BuildingQuickCreate”, “BuildingPicker”, “BuildingSelect” was found; the only building quick-add in this flow is **QuickBuildingModal** on CreateOrderPage.

---

## 3) Error-handling wiring (approve handler)

**File:** `frontend/src/pages/orders/CreateOrderPage.tsx`  
**Handler:** `handleApproveDraft` (lines 992–1004):

```ts
catch (err: any) {
  showError(err.message || 'Failed to approve draft');
}
```

- **HTTP status:** Not checked (no `err.status` or `err.response?.status`).
- **Error code:** Not checked (no `err.data?.errorCode` or similar).
- **Message → dialog:** The message “Building not found…” is **not** mapped to opening any dialog; only `showError(...)` is called.

So the **exact condition** today: there is **no** condition that opens the building dialog; any error is treated the same and only shown as a toast.

---

## 4) Backend error contract

| Where | Detail |
|-------|--------|
| Message thrown | `backend/src/CephasOps.Application/Parser/DTOs/CreateOrderFromDraftDto.cs`: `CreateOrderFromDraftResult.RequiresBuilding(...)` sets `ErrorMessage = "Building not found. Please create or select a building to continue."` and `BuildingDetection = buildingDetection`. |
| Where it’s produced | `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` (around 863–878): when building not found, returns `CreateOrderFromDraftResult.RequiresBuilding(buildingDetection)`. |
| In ParserService | `backend/src/CephasOps.Application/Parser/Services/ParserService.cs` (710–735): when `!result.Success`, throws `InvalidOperationException($"Order creation failed: {errorNotes}")` (errorNotes includes `result.ErrorMessage`). **No** separate handling for `result.BuildingDetection`; no stable error code. |
| In API | `backend/src/CephasOps.Api/Controllers/ParserController.cs` (386–391): `catch (Exception ex)` → `return this.InternalServerError<ParsedOrderDraftDto>($"Failed to approve parsed order: {ex.Message}");` |

So today:

- **HTTP status:** **500** (Internal Server Error).
- **Stable error code:** **None** (no `BUILDING_NOT_FOUND` or similar in response body).
- **Body:** Standard API error envelope with `message` containing the exception text (including “Building not found…”). `BuildingDetection` is **not** returned to the client.

Frontend only sees a 500 and a message string; it does **not** receive an error code or `buildingDetection`.

---

## 5) Conclusion

**Answer: B) Dialog exists but approve flow does not call it**

- **Dialog:** Implemented as **QuickBuildingModal** on CreateOrderPage, with props for `mode="create-and-approve"` and `draftId` for the parser flow, but **not** used in the approve path.
- **Approve flow:** Never opens the modal on error; it only shows a toast. So the “Create Building” dialog is **not** connected to the approve flow.
- **Backend:** Returns 500 and a message only; no error code and no `buildingDetection`, so the frontend cannot reliably detect “building required” or prefill the modal without string-matching the message.

---

## 6) Minimal fix

**Goal:** When approve fails with “building not found”, open QuickBuildingModal; after user creates a building, set it on the draft and retry approve (no refactors, minimal changes).

### Option A – Frontend-only (message match)

- **CreateOrderPage.tsx**
  - In `handleApproveDraft` catch: if `err.message` includes “Building not found” (or “create or select a building”), set state to open the building modal in “approve-retry” mode (e.g. `setShowBuildingModal(true)`, `setBuildingModalFromApproval({ draftId })`).
  - Render `QuickBuildingModal` with `mode="create-and-approve"`, `draftId={draftId}` when in this mode, and `initialData` from current form address (e.g. `parseAddressForBuilding(watch('address'))`) so the modal is prefilled as much as possible.
  - In `handleBuildingCreated`: when `buildingModalFromApproval?.draftId` is set, after setting `buildingId` and closing the modal, call `updateParsedOrderDraft(draftId, { buildingId })` then `approveParsedOrderDraft(draftId)`; on success navigate to `/orders/parser` and clear the “from approval” state.
- **Reliability:** Depends on backend message text; if the message is changed, the condition breaks.

### Option B – Backend returns stable code + buildingDetection (recommended)

- **Backend**
  - In **ParserService.ApproveParsedOrderAsync**: when `!result.Success` and `result.BuildingDetection != null`, throw a dedicated exception (e.g. `BuildingRequiredForApprovalException`) that carries `ErrorMessage` and `BuildingDetection`, instead of a generic `InvalidOperationException`.
  - In **ParserController.ApproveParsedOrder**: catch that exception and return **400 Bad Request** (or 409 Conflict) with a body that includes a stable **error code** (e.g. `errorCode: "BUILDING_NOT_FOUND"`) and `buildingDetection` (e.g. `detectedBuildingName`, `detectedAddress`, etc.) so the frontend can prefill the modal.
- **Frontend**
  - In `handleApproveDraft` catch: if `err.status === 400` and `err.data?.errorCode === 'BUILDING_NOT_FOUND'`, set modal open state and store `err.data.buildingDetection` (and `draftId`) for the “approve-retry” flow.
  - Pass `initialData` from `buildingDetection` into `QuickBuildingModal` when opening from this path.
  - Same as Option A: `handleBuildingCreated` updates draft with `buildingId`, then calls `approveParsedOrderDraft(draftId)` and navigates on success.

**Exact files to touch (minimal):**

| Layer | File | Change |
|-------|------|--------|
| Backend | `CephasOps.Application/Parser/Services/ParserService.cs` | When `!result.Success` and `result.BuildingDetection != null`, throw `BuildingRequiredForApprovalException(result.ErrorMessage, result.BuildingDetection)` instead of `InvalidOperationException`. |
| Backend | New: `CephasOps.Application/Parser/Exceptions/BuildingRequiredForApprovalException.cs` (or under a shared folder) | Exception with `ErrorMessage` and `BuildingDetectionResult`. |
| Backend | `CephasOps.Api/Controllers/ParserController.cs` | Catch `BuildingRequiredForApprovalException`; return 400 with body `{ success: false, errorCode: "BUILDING_NOT_FOUND", message: ex.Message, buildingDetection: ex.BuildingDetection }`. |
| Frontend | `frontend/src/pages/orders/CreateOrderPage.tsx` | In `handleApproveDraft` catch: if `err.status === 400 && err.data?.errorCode === 'BUILDING_NOT_FOUND'`, set state to show QuickBuildingModal with `buildingDetection` as `initialData` and “approve-retry” context (`draftId`). In `handleBuildingCreated`, when in approve-retry context: `updateParsedOrderDraft(draftId, { buildingId })` then `approveParsedOrderDraft(draftId)`; on success navigate and clear state. Pass `mode="create-and-approve"` and `draftId` to QuickBuildingModal when opened from this path. |

If you skip the backend change (Option A only), the frontend can still open the dialog by matching `err.message` to “Building not found” (or “create or select a building”), and use form address for `initialData`; retry flow remains update draft + approve.

---

## Verification steps

1. Open `/orders/parser`, pick a draft whose building is missing or not resolved → **Review** → go to Create Order page.
2. Click **Approve** (without selecting a building).
3. **Pass:** A “Quick Add Building” (or similar) dialog opens instead of only a toast.
4. Fill required fields (name, address, city, state, postcode), click **Create Building** (or “Create Building & Approve Order”).
5. **Pass:** Modal closes; draft is updated with the new building and approval is retried; success toast and redirect to `/orders/parser` (or order created and draft approved).
6. **Pass:** No “Building not found” error after creating the building.
