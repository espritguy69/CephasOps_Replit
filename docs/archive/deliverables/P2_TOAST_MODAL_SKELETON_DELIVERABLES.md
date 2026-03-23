# P2 Toast + Modal + Skeleton – Deliverables

**Date:** 3 February 2026  
**Scope:** Toast unification (SI ↔ Admin), SI primitives (Modal, Skeleton) – TS/TSX only, no CSS edits.

---

## 1. Files changed

| File | Change |
|------|--------|
| `frontend-si/src/components/ui/Toast.tsx` | Default duration 3000 → 5000ms to match Admin. |
| `frontend-si/src/pages/jobs/JobsListPage.tsx` | Added `useToast`, Retry button on error state using `showInfo('Retrying…')` and `refetch()`. |

**No changes:** Admin `Toast.tsx`, Admin or SI `Modal.tsx`, Admin or SI `skeleton.tsx` – SI already had the same API (Modal, Skeleton). DataTable not required for jobs list (Jobs list uses Card list, not DataTable).

---

## 2. API signatures (unified)

### Toast (Admin & SI – identical)

```ts
interface ToastContextValue {
  showToast: (message: string, type?: 'success' | 'error' | 'warning' | 'info', duration?: number) => number;
  showSuccess: (message: string, duration?: number) => number;
  showError: (message: string, duration?: number) => number;
  showWarning: (message: string, duration?: number) => number;
  showInfo: (message: string, duration?: number) => number;
  dismissToast: (id: number) => void;
}
```

- Default duration: **5000ms** (both apps).
- Usage: wrap app with `<ToastProvider>`, then `const { showSuccess, showError, showWarning, showInfo, dismissToast } = useToast();`

### Modal (Admin & SI – already aligned)

```ts
interface ModalProps {
  isOpen: boolean;
  onClose?: () => void;
  title?: string;
  children: ReactNode;
  size?: 'small' | 'medium' | 'large' | 'xl';
  closeOnOverlayClick?: boolean;
  closeOnEscape?: boolean;
  className?: string;
}
```

### Skeleton (Admin & SI – already aligned)

```ts
interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {}
// Usage: <Skeleton className="h-6 w-40" />
```

---

## 3. How to test manually (~2 min)

### SI app – Toast + Jobs list reference

1. Start SI app: `cd frontend-si && npm run dev`.
2. Log in as a service installer.
3. **Toast (default duration):** Open a page that triggers a toast (e.g. Job detail → update status, or Materials scan → record scan). Confirm toasts appear and auto-dismiss after ~5s (or use close button).
4. **Toast (reference – Jobs list):** If you can force an error (e.g. disconnect network or use a user with no SI), open **Assigned Jobs**. You should see the error EmptyState and a **Retry** button. Click **Retry** → an info toast “Retrying…” appears. Re-enable network / fix user and Retry again to see jobs load.

### Admin app – Toasts unchanged

1. Start Admin: `cd frontend && npm run dev`.
2. Log in, open any page that uses toasts (e.g. Settings → KPI Profiles, create/update). Confirm success/error toasts still appear and behave as before (default 5s).

### Builds

- `cd frontend && npm run build` → success.
- `cd frontend-si && npm run build` → success.

---

## 4. Audit summary (STEP 1)

- **Admin toast:** `frontend/src/components/ui/Toast.tsx` – ToastProvider + useToast; default duration 5000.
- **SI toast:** Same API; only difference was default duration (3000). Set to 5000.
- **Modal / Skeleton:** SI already had Modal and Skeleton with the same API as Admin; no code changes.
- **Strategy:** Unify default duration only; no new libraries; no CSS edits.
