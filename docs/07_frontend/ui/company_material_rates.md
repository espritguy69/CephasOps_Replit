\# UI — Company Material Rates



This document defines the UI for managing \*\*company-specific material rates\*\*

that sit on top of \*\*global Material Templates\*\*.



Users:



\- Finance / P\&L team

\- Operations admins

\- Company admins



---



\## 1. Navigation \& Scope



Path pattern:



\- \*\*Company Settings → Material Rates\*\*

\- URL: `/companies/:companyId/settings/material-rates`



Context:



\- Always scoped by `companyId`.

\- Optionally filtered by `rateProfileCode` and `category`.



---



\## 2. Main Screen: Company Material Rates List



\### 2.1 Filters



At the top of the page:



\- \*\*Rate Profile\*\* (dropdown)

&nbsp; - Values: `DEFAULT`, `TIME\_FTTH`, `TIME\_ASSURANCE`, etc.

&nbsp; - Default = `DEFAULT`.

\- \*\*Category\*\* (dropdown)

&nbsp; - Values from MaterialTemplate.Category (e.g. ONT, CABLE, ACCESSORY).

\- \*\*Text search\*\* (input)

&nbsp; - Search by material code or name.



\### 2.2 Table Columns



| Column              | Description                                         |

|---------------------|-----------------------------------------------------|

| Material Code       | From MaterialTemplate.Code                          |

| Name                | From MaterialTemplate.Name                          |

| Category            | From MaterialTemplate.Category                      |

| Unit                | From MaterialTemplate.UnitOfMeasure                 |

| Client Price        | CompanyMaterialRate.ClientPrice (editable)          |

| Internal Cost       | CompanyMaterialRate.InternalCost (editable)         |

| Installer Payout    | CompanyMaterialRate.InstallerPayout (editable)      |

| Taxable             | Checkbox                                            |

| Active              | Toggle                                              |

| Notes               | Icon / hover / inline small text                    |

| Actions             | Edit (modal) / Reset (to default)                   |



\### 2.3 Behaviours



\- Inline editing for numeric fields (price, cost, payout).

\- Auto-format as currency based on company currency.

\- Dirty state indicator until Save is pressed.

\- Pagination or infinite scroll for large material catalogues.

\- “Only show materials with custom rates” toggle (for power users).



\### 2.4 Actions



\- \*\*Save Changes\*\* (applies all pending edits).

\- \*\*Discard Changes\*\* (reverts to last loaded values).

\- \*\*Export to CSV/Excel\*\* (for finance review).

\- \*\*Import CSV\*\* (optional, for bulk updates).



---



\## 3. Rate Edit Modal



Clicking “Edit” in Actions opens a modal for the selected material \& profile.



Fields:



\- Material (read-only)

&nbsp; - Code + Name + Unit

\- Rate Profile (dropdown)

\- Client Price (currency input)

\- Internal Cost (currency input)

\- Installer Payout (currency input)

\- Taxable (checkbox)

\- Active (toggle)

\- Notes (textarea)



Buttons:



\- \*\*Save\*\*

\- \*\*Cancel\*\*

\- \*\*Reset to defaults\*\* (optional)

&nbsp; - Pulls from:

&nbsp;   - `DEFAULT` profile for that company, or

&nbsp;   - Global default logic if allowed.



Validation:



\- Price / cost / payout ≥ 0.

\- Optionally: payout ≤ client price (with warning).



---



\## 4. Rate Profiles Selector



On top of main list:



\- A Rate Profile dropdown + “Manage Profiles…” link.



Click \*\*“Manage Profiles…”\*\* → navigates to:



\- `/companies/:companyId/settings/rate-profiles`



Short UI spec:



\- List of profiles (Code, Name, Description, Default?, Active?).

\- Actions:

&nbsp; - Set as default (enforces single default).

&nbsp; - Activate / deactivate.

&nbsp; - Create new profile (Code + Name).



---



\## 5. Integration with Orders / Projects



The UI should indicate how rates are used:



\- If a rate is tied to a specific partner or order type (via profile),

&nbsp; show badges like:

&nbsp; - `TIME FTTH`

&nbsp; - `ASSURANCE`

&nbsp; - `ECO SHOP`



On hover or side info panel:



\- Show:

&nbsp; - “Used for: FTTH orders from TIME”

&nbsp; - “Used for: Assurance orders”

&nbsp; - etc. (based on future integrations)



---



\## 6. Error Handling UX



When saving:



\- If backend detects configuration conflict (e.g. invalid combination):

&nbsp; - Show error toast + inline field error.

\- If `MaterialRateStrictMode` is enabled:

&nbsp; - Warn users that removing a rate while material is actively used

&nbsp;   may cause billing failures.



---



\## 7. Permissions



\- Only users with roles like:

&nbsp; - `ROLE\_COMPANY\_ADMIN`

&nbsp; - `ROLE\_FINANCE\_ADMIN`

&nbsp; can edit rates.

\- Others:

&nbsp; - Can view (read-only grid) but not edit.



If user lacks permission:



\- Inputs are disabled.

\- An info banner explains:

&nbsp; - “You have read-only access to company material rates.”



---



\## 8. Storybook Components



Create stories for:



\- `<CompanyMaterialRatesTable />`

&nbsp; - Default data

&nbsp; - Empty state (no rates yet)

&nbsp; - Read-only state

\- `<MaterialRateEditModal />`

&nbsp; - New rate

&nbsp; - Existing rate with notes

\- `<RateProfileSelector />`

&nbsp; - With default profile

&nbsp; - With multiple profiles



Mock data must include:



\- MaterialTemplate fields (Code, Name, Category, UnitOfMeasure)

\- CompanyMaterialRate fields (ClientPrice, InternalCost, InstallerPayout, Taxable, Active)

\- RateProfileCode for context.



---



\## 9. Non-Goals



This screen does \*\*not\*\*:



\- Manage stock (quantities, locations, serials).

\- Define global material templates (separate screen).

\- Directly modify invoices or payouts (those use rates via services).



Its purpose:



> To give finance/ops a clear place to control \*\*how each company values each material\*\* across different profiles, in a way that flows cleanly into Billing, P\&L, and Installer payouts.



