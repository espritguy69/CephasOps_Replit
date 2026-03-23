# UI Theme Alignment Summary

**Goal:** Align Admin Portal and SI App theme tokens so both look like the same product (Admin purple 263 palette).  
**Scope:** CSS variable (token) edits only; no new frameworks, no business logic changes, Syncfusion unchanged.

---

## CSS Files Changed

| File | Change |
|------|--------|
| `frontend-si/src/index.css` | Updated `:root` and `.dark` CSS variables to match Admin Portal tokens; added `--brand` / `--brand-foreground`; added `--color-brand` / `--color-brand-foreground` in `@theme`. |
| `frontend/src/index.css` | **Not modified.** Admin remains the source of truth. |

---

## Token Changes (SI App — Before → After)

All values are HSL triplets (e.g. `263 55% 64%`) unless noted.

### Light mode (`:root`)

| Token | Before (SI) | After (aligned to Admin) |
|-------|-------------|---------------------------|
| `--primary` | 221.2 83.2% 53.3% (blue) | **263 55% 64%** (purple) |
| `--primary-foreground` | 210 40% 98% | **0 0% 100%** |
| `--secondary` | 210 20% 96% | **263 85% 96%** |
| `--secondary-foreground` | 222.2 47% 11% | **263 55% 24%** |
| `--muted` | 210 20% 96% | **260 20% 96%** |
| `--muted-foreground` | 215.4 16.3% 47% | **263 10% 45%** |
| `--accent` | 210 20% 96% | **160 84% 95%** |
| `--accent-foreground` | 222.2 47% 11% | **160 84% 26%** |
| `--border` | 220 13% 91% | **260 15% 91%** |
| `--input` | 220 13% 91% | **260 15% 91%** |
| `--ring` | 221.2 83.2% 53.3% | **263 55% 64%** |
| `--radius` | 0.5rem | 0.5rem (unchanged) |
| `--brand` | *(none)* | **263 52% 64%** (added) |
| `--brand-foreground` | *(none)* | **0 0% 100%** (added) |

`--background`, `--foreground`, `--card`, `--popover`, `--destructive`, `--destructive-foreground` were already aligned or left as-is.

### Dark mode (`.dark`)

| Token | Before (SI) | After (aligned to Admin) |
|-------|-------------|---------------------------|
| `--primary` | 217.2 91.2% 59.8% | **263 55% 64%** |
| `--primary-foreground` | 222.2 47% 11% | **0 0% 100%** |
| `--secondary` | 217.2 32.6% 17.5% | **263 30% 15%** |
| `--secondary-foreground` | 210 40% 98% | **263 85% 95%** |
| `--muted` | 217.2 32.6% 18% | **217.2 32.6% 17.5%** |
| `--muted-foreground` | 215 20% 65% | **215 20.2% 65.1%** |
| `--accent` | 217.2 32.6% 18% | **160 60% 20%** |
| `--accent-foreground` | 210 40% 98% | **160 84% 90%** |
| `--border` | 217.2 32.6% 22% | **217.2 32.6% 17.5%** |
| `--input` | 217.2 32.6% 22% | **217.2 32.6% 17.5%** |
| `--ring` | 224.3 76.3% 48% | **263 52% 58%** |
| `--destructive` | 0 62.8% 30.6% | **0 62.8% 50.6%** |
| `--brand` | *(none)* | **263 52% 58%** (added) |
| `--brand-foreground` | *(none)* | **0 0% 100%** (added) |

Dark `--background`, `--foreground`, `--card`, `--popover` set to Admin values (222.2 84% 4.9%, 210 40% 98%, etc.).

---

## Manual Verification Steps

### Admin Portal (`/frontend`)

1. **/orders** — Orders list: primary buttons and links are purple; borders and inputs use muted gray; text readable (foreground on background).
2. **/scheduler/timeline** (or installer scheduler route) — Scheduler toolbar and primary actions use theme; Syncfusion schedule renders; no broken focus rings or borders.
3. **/inventory/stock-summary** — Page header, buttons, cards, and table borders look correct; no contrast or readability regressions.

### SI App (`/frontend-si`)

1. **/jobs** (or main list route) — Primary nav and buttons use **purple** (same hue as Admin); cards and borders consistent; text contrast OK.
2. **Login / dashboard** — Primary actions and links are purple; focus rings use ring token.

### General

- **Readability:** Button text (primary-foreground on primary) is clear; body text (foreground on background) remains readable.
- **Borders:** Inputs and cards use border/input tokens; no missing or harsh borders.
- **Syncfusion (Admin):** Scheduler and any Grid pages still render; no new Syncfusion CSS added; only token-driven styles changed (Admin CSS untouched).

---

## Syncfusion

- **Admin:** `frontend/src/index.css` was not modified; existing Syncfusion usage and `.modern-scheduler` overrides are unchanged.
- **SI:** No Syncfusion; no Syncfusion-specific CSS.
- No new Syncfusion theme imports or heavy overrides were added.

---

*Theme alignment applied; only SI token values and @theme brand entries were changed.*
