# Tenant Safety Intentional Drift Log

**Purpose:** This file is the **only** way to override the CI failure when the tenant safety score drops. If the tenant-safety workflow fails with "Architecture drift: tenant safety score dropped", you must either fix the causes (preferred) or document an intentional acceptance of the lower score by updating this file in the same PR.

**Override rule:** CI allows a score drop only if this file is **modified in the same PR** and the change includes a **valid new table row**: a date (YYYY-MM-DD), a score transition that **exactly matches the drop CI detected** (e.g. if CI says "score dropped from 100 to 95", the row must contain `100→95` or `100->95`), and a non-empty Reason/PR. CI validates the format and that the score transition matches the actual previous→current; cosmetic or mismatched entries will not pass.

---

## How to document intentional drift

1. In the same PR where the score dropped, **add a row** to the **Entries** table below (new line starting with `|`).
2. Set **Date** to YYYY-MM-DD, **Score (before → after)** to the **exact transition CI reported** (e.g. if CI says "Score DROPPED from 100 to 95", use `100→95` or `100->95`—no other pair is accepted), and **Reason / PR** to a short justification or PR link (non-empty).
3. Commit the change. CI will pass only if it finds an **added** table row with that exact score transition plus date and reason; otherwise it fails with "no valid matching entry found".
4. Do not use this to bypass fixing real violations; prefer fixing manual scope, executor gaps, or doc issues.

---

## Entries

| Date | Score (before → after) | Reason / PR |
|------|------------------------|-------------|
| *(none yet)* | | |

---

*Do not remove this file. CI requires an added table row with date, score transition matching the detected drop (previous→current or previous->current), and non-empty reason when the score drops.*
