# Global Styles Alignment Summary

**Goal:** Normalize padding rhythm, typography scale, and default element styles across Admin and SI via CSS variables and base styles only (no component edits).

---

## Files Changed

| File | Changes |
|------|--------|
| `frontend/src/index.css` | Added typography/spacing tokens to `:root`; set `body` font-size and line-height; base styles for `h1`/`h2`/`h3`; `.page-pad` utility. |
| `frontend-si/src/index.css` | Added Public Sans font import; same typography/spacing tokens; same `body` and `h1`/`h2`/`h3` base styles; same `.page-pad` utility; aligned `body` font-family with Admin. |

---

## Tokens Added (both apps)

In `:root` only (shared values; no dark overrides):

| Token | Value | Purpose |
|-------|--------|--------|
| `--font-size-xs` | 0.75rem | Extra small text |
| `--font-size-sm` | 0.875rem | Small text |
| `--font-size-base` | 0.875rem | Body text (14px) |
| `--line-height-base` | 1.5 | Body line height |
| `--font-size-lg` | 1rem | Large text |
| `--font-size-xl` | 1.125rem | XL text |
| `--font-size-2xl` | 1.25rem | 2XL text |
| `--heading-1` | 1.125rem | Default h1 size |
| `--heading-2` | 1rem | Default h2 size |
| `--heading-3` | 0.875rem | Default h3 size |
| `--page-pad` | 1rem | Page padding (mobile) |
| `--page-pad-md` | 1.5rem | Page padding (md+) |

Existing tokens unchanged: `--border`, `--input`, `--radius` (already used by `* { border-color }` and Tailwind).

---

## Base Styles Applied

- **body:** `font-size: var(--font-size-base); line-height: var(--line-height-base);` (Admin already had font-family; SI now uses same Public Sans stack.)
- **h1, h2, h3:** `color: var(--foreground); font-weight: 600;` plus font-size/line-height from `--heading-1` / `--heading-2` / `--heading-3`. Component classes (e.g. `text-xl font-bold`) still override where used.
- **Borders:** Unchanged; both apps already use `* { border-color: hsl(var(--border)) }`.
- **.page-pad:** `padding: var(--page-pad);` with `@media (min-width: 768px) { padding: var(--page-pad-md); }` (equivalent to Tailwind `p-4 md:p-6`). Optional for use on wrappers; no existing class names changed.

---

## Visual Differences That Should Disappear

| Before | After |
|--------|--------|
| SI body text could feel larger (system font, browser default size) | Both apps use 14px (0.875rem) body and same line-height. |
| SI different font (system stack) vs Admin (Public Sans) | Both use Public Sans with same fallback stack. |
| Inconsistent default heading sizes where no class applied | Unstyled h1/h2/h3 get shared scale (heading-1/2/3) and weight. |
| No shared token for page padding | `.page-pad` available in both; same rhythm as p-4 md:p-6. |
| Border/radius already token-driven | No change; remains consistent via existing --border and --radius. |

---

## Quick Route Checks

1. **Admin /orders** — List and filters: body text and headings use base size/weight; borders and spacing unchanged; no layout shift.
2. **Admin /inventory/ledger** — Table and filters: same base typography; contrast and density unchanged.
3. **SI main list page (e.g. /jobs)** — List and cards: body and headings match Admin scale; primary font Public Sans; buttons/inputs align visually (they already use Tailwind tokens; base font-size now matches).

---

## Contrast & Layout

- Text: `--foreground` on `--background` unchanged; contrast unchanged.
- Headings use `--foreground`; no new colors.
- No changes to component-level button/input heights; alignment comes from shared base font-size and font-family so SI no longer feels “bigger” than Admin.
