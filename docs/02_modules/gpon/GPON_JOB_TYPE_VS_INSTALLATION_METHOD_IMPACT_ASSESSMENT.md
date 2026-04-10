# GPON: “Job Type” vs “Installation Method” — Impact Assessment

**Date:** 2026-03-08  
**Scope:** Whether visible UI wording “Job Type” can be replaced with “Installation Method” in relevant places.  
**Constraint:** Analysis and recommendation only; no code or display changes in this deliverable.

---

## 1. Executive summary

**Conclusion: Do not replace “Job Type” with “Installation Method”.**

- **Order Type** and **Installation Method** are **different concepts** in CephasOps. They are separate dimensions used together in orders, rate keying, workflows, and building/default materials.
- **“Job Type”** in the UI was a legacy label for **Order Type** (Activation, Modification, Assurance, VAS). That label has already been standardized to **“Order Type”** in GPON screens.
- **Installation Method** is its own entity and UI concept: site condition (Prelaid, Non-Prelaid, SDU, RDF Pole). It appears on the same screens as Order Type (e.g. Partner Rates, SI Rate Plans) as a **separate** column/field.
- Replacing “Job Type” (meaning Order Type) with “Installation Method” would **conflate two dimensions**, confuse users, and conflict with the existing Installation Method field on the same forms.

**Recommendation: C — Do not replace at all; they are different concepts.**  
Keep “Order Type” where the underlying concept is Order Type (OrderTypeId). Keep “Installation Method” / “Site Condition” where the underlying concept is Installation Method (InstallationMethodId).

---

## 2. Concept comparison

### 2.1 Order Type

| Aspect | Detail |
|--------|--------|
| **Definition** | Type of work: Activation, Modification (Indoor/Outdoor), Assurance, Value Added Service. |
| **Backend** | Entity `OrderType`; FK `OrderTypeId` on Order, GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, BillingRatecard, BuildingDefaultMaterial, workflow scope (OrderTypeCode), etc. |
| **UI label (current)** | “Order Type” / “Order Types” (after terminology pass). |
| **UI usage** | Order Types settings page, Partner Rates (column + filter + form), SI Rate Plans, Rate Engine Management, Building default materials (per-order-type), Order create/edit, workflow definition scope, KPI profiles, automation/escalation/SLA rules, PnL, payroll, billing, dashboards. |
| **API/contracts** | `OrderTypesController`, `orderTypeId`, `orderTypeCode`, DTOs with OrderTypeId/OrderTypeName. |
| **Workflows** | Workflow definition scope uses `OrderTypeCode` (from Order’s OrderType). |
| **Reporting / PnL** | PnL and order profitability use OrderTypeId / order type. |
| **Rates/pricing** | **Required** dimension for Partner and SI rates; optional for Billing. Rate key = OrderType + OrderCategory + InstallationMethod + … |
| **Filtering/search** | Filters by Order Type on rates, orders, PnL. |
| **Validations** | Order requires OrderTypeId; rate forms require Order Type selection where applicable. |
| **Integrations** | Parser templates, import/export, deployment DTOs reference order type. |

### 2.2 Job Type (historical / display only)

| Aspect | Detail |
|--------|--------|
| **Definition** | In CephasOps, “Job Type” in the UI was **not** a separate backend concept. It was a **display label** for **Order Type** (and in API names, “Job” in “GponPartnerJobRate” / “GponSiJobRate” means “GPON job” i.e. work order, not a type dimension). |
| **Backend** | No entity or FK named “JobType”. Order Type is stored as `OrderTypeId`. |
| **UI label (current)** | Replaced with “Order Type” on GPON screens (Order Types page, Partner Rates, SI Rate Plans, Building materials, Rate Engine Management). |
| **Conclusion** | “Job Type” = Order Type for display purposes. No separate “Job Type” concept to rename to Installation Method. |

### 2.3 Installation Method

| Aspect | Detail |
|--------|--------|
| **Definition** | How/where installation is done: Prelaid, Non-Prelaid, SDU, RDF Pole, etc. (site condition). |
| **Backend** | Entity `InstallationMethod`; FK `InstallationMethodId` on Order, Building, GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, BillingRatecard, SiRatePlan, KpiProfile, MaterialTemplate. |
| **UI label (current)** | “Installation Method” or “Site Condition (Installation Method)” / “Site Condition” on rate screens. |
| **UI usage** | Installation Methods settings page, Partner Rates (column “Site Condition” + form “Site Condition (Installation Method)”), SI Rate Plans (Site Condition tab/labels), Rate Engine Management (Installation Methods checkbox group + filters), Building/Building detail, Order create/edit, PnL drilldown. |
| **API/contracts** | `InstallationMethodsController`, `installationMethodId`, DTOs with InstallationMethodId/name. |
| **Workflows** | Not used as workflow definition scope (workflow uses OrderTypeCode, Partner, Department). |
| **Reporting / PnL** | PnL detail can show Installation Method. |
| **Rates/pricing** | **Optional** dimension for Partner and SI rates (null = “all methods”). Same rate key as above. |
| **Filtering/search** | Filter by Installation Method on rates and in Rate Engine. |
| **Validations** | Required in some rate forms (e.g. “At least one Installation Method must be selected” for SI/custom rate plans). |
| **Integrations** | Building and order import/export; deployment DTOs. |

### 2.4 Side-by-side (rate key)

From domain and `GPON_RATE_ENGINE_DIMENSIONS_AUDIT.md`:

| Dimension | Meaning | Example values |
|-----------|--------|----------------|
| **Order Type** | What work | Activation, Modification, Assurance, VAS |
| **Order Category** | Service/tech category | FTTH, FTTO, FTTR, FTTC |
| **Installation Method** | Site condition / how | Prelaid, Non-Prelaid, SDU, RDF Pole |

Partner and SI rate key = **OrderType** + OrderCategory + **InstallationMethod** + (PartnerGroup / SiLevel / etc.). So Order Type and Installation Method are **both** in the same key and are **distinct**.

---

## 3. Are “Job Type” and “Installation Method” the same concept?

**No. They are not synonyms.**

- **“Job Type”** in the UI referred to **Order Type** (what kind of work: Activation, Mod, Assurance, VAS). That UI has been aligned to “Order Type”.
- **Installation Method** is **site condition** (Prelaid, Non-Prelaid, SDU, RDF Pole). It is a separate dimension with its own entity, FK, and settings screen.
- On **Partner Rates** and **SI Rate Plans**, both appear on the same screen:
  - One column/field is **Order Type** (OrderTypeId).
  - Another column/field is **Site Condition** / **Installation Method** (InstallationMethodId).
- **Building default materials** are keyed by **Order Type** (OrderTypeId), not Installation Method (see `BuildingDefaultMaterial.OrderTypeId`).
- **Order** has both `OrderTypeId` and `InstallationMethodId`.
- **Rate resolution** takes both `OrderTypeId` and `InstallationMethodId`; they are not interchangeable.

So: **Installation Method is one dimension of the model, not a synonym for Job Type / Order Type.** Replacing “Job Type” (Order Type) with “Installation Method” would incorrectly merge two different concepts.

---

## 4. Risk of replacing the label “Job Type” with “Installation Method”

| Risk | Level | Explanation |
|------|--------|-------------|
| **Conflating two dimensions** | High | Same form would then have two fields both suggesting “Installation Method” (one would actually be Order Type). |
| **Conflict with existing Installation Method field** | High | Partner Rates / SI Rate Plans already have a separate “Site Condition (Installation Method)” field. Renaming Order Type to “Installation Method” would create two “Installation Method” concepts on one screen. |
| **Wrong mental model** | High | Users would think “Activation” vs “Modification” is an “installation method”; in the product, those are order types. Installation method is Prelaid vs Non-Prelaid, etc. |
| **Clarity** | N/A | No clarity gain; meaning would be incorrect. |
| **Consistency** | Negative | Would contradict domain, API, and existing Installation Method screens and dropdowns. |

---

## 5. Recommendation

**Recommendation: C — Do not replace at all; they are different concepts.**

- Do **not** replace “Job Type” with “Installation Method” anywhere that the underlying data is Order Type (OrderTypeId).
- Keep current usage:
  - **Order Type** / **Order Types** for OrderTypeId (including all GPON rate screens, building materials, order flows, workflow scope, reports).
  - **Installation Method** / **Site Condition** for InstallationMethodId (settings, buildings, orders, rate screens where it is already labeled).

No partial replacement is recommended: any swap of “Job Type” → “Installation Method” where the field is Order Type would be semantically wrong and confusing.

---

## 6. Mapping table (screen / concept / label / risk)

| Screen / feature | Current label | Underlying concept | Recommended label | Risk if renamed to “Installation Method” | Notes |
|------------------|---------------|--------------------|-------------------|------------------------------------------|--------|
| Order Types page | Order Types | OrderType entity | Order Types | High | This is the Order Type entity; must not be “Installation Method”. |
| Partner Rates – column | Order Type | OrderTypeId | Order Type | High | Same row has “Site Condition” (InstallationMethodId). |
| Partner Rates – form | Order Type | OrderTypeId | Order Type | High | Form also has “Site Condition (Installation Method)”. |
| Partner Rates – filter | All Order Types | OrderTypeId | All Order Types | High | Distinct from Installation Method filter. |
| SI Rate Plans – Order Type Rates tab | Order Type Rates | OrderTypeId-based rates | Order Type Rates | High | Different from “Site Condition Rates” tab. |
| SI Rate Plans – form (order type) | Order Type | OrderTypeId | Order Type | High | Same form has Site Condition (Installation Method). |
| Rate Engine Management – Partner rate form | Order Type * | OrderTypeId | Order Type | High | Same form has “Installation Methods *” checkbox group. |
| Rate Engine Management – filters | Order Type | OrderTypeId | Order Type | High | Separate from Installation Method filter. |
| Rate Engine Management – table columns | Order Type | orderTypeName | Order Type | High | Installation method is a different column. |
| Building default materials – section | Order Types | OrderTypeId | Order Types | High | Materials are per Order Type, not per Installation Method. |
| Building default materials – form | Order Type * | OrderTypeId | Order Type | High | Entity is BuildingDefaultMaterial.OrderTypeId. |
| Order create/edit | Order Type / Installation Method | OrderTypeId / InstallationMethodId | Order Type; Installation Method | High | Two separate fields; must stay distinct. |
| Workflow definitions – scope | Order Type | OrderTypeCode | Order Type | High | Workflow scope is by order type, not installation method. |
| KPI Profiles | Order Type | OrderTypeId | Order Type | High | Installation Method is a separate optional dimension. |
| Automation / Escalation / SLA rules | Order Type | orderType | Order Type | High | Rule scope by order type. |
| PnL / Reports / Dashboards | Order Type | OrderTypeId / orderType | Order Type | High | Dimension is order type; installation method separate where used. |
| Installation Methods page | Installation Methods | InstallationMethod entity | Installation Methods | N/A | This is the correct label for the entity. |
| Partner Rates – Site Condition column | Site Condition | InstallationMethodId | Site Condition or Installation Method | N/A | Already correct; do not change to “Job Type”. |
| SI Rate Plans – Site Condition tab | Site Condition (Installation Method) | InstallationMethodId | Keep as is | N/A | Clear that it’s installation method. |

---

## 7. Exact UI places that can be renamed safely

**None.**  

There is no place where the **concept** is Installation Method but the **label** is “Job Type”. The only remaining “job type” in the frontend is an internal API comment in `buildingDefaultMaterials.ts` (excluded from UI terminology). All user-facing “Job Type” has already been aligned to “Order Type” for the correct concept (OrderTypeId). So there is no safe renaming of “Job Type” → “Installation Method”.

---

## 8. Exact UI places that must not be renamed

Do **not** change the following to “Installation Method”; they refer to **Order Type** (OrderTypeId / OrderTypeCode):

- **Order Types page:** title, breadcrumb, guide heading, “Parent Order Types”, “Edit Order Type”, “Add Parent Order Type”, empty states, toasts (e.g. “Order type created successfully”).
- **Partner Rates:** table column “Order Type”, guide “Site & Order Type”, form label “Order Type”, filter “All Order Types”.
- **SI Rate Plans:** “Order Type Rates” tab and guide step, form fields for order type.
- **Rate Engine Management:** “Order Type” filter, “Order Type *” in Partner rate form, “Order Types *” in SI/Custom rate forms, table column “Order Type”, validation messages (“At least one Order Type must be selected”), Rate Calculator “Order Type *”.
- **Building default materials (Building Detail):** section “Order Types”, “Order Type *” and “Select Order Type” in material modal, “Add materials for other order types”, empty state “No materials configured for this order type”.
- **Order create/edit:** “Order Type” dropdown/label (keep “Installation Method” as the other field).
- **Workflow definitions:** Order Type scope field/label.
- **KPI Profiles, Automation, Escalation, SLA, Approval Workflows:** “Order Type” column/label.
- **PnL, Payroll, Billing, Dashboards, Orders list/detail:** “Order Type” column/label where it shows order type.

All of the above are bound to Order Type (OrderTypeId / OrderTypeCode). Renaming any of them to “Installation Method” would be wrong and confusing.

---

## 9. Follow-up actions before any implementation

1. **No renaming:** Do not implement any replacement of “Job Type” or “Order Type” with “Installation Method” for fields that represent Order Type.
2. **Training / docs:** If needed, document for users that:
   - **Order Type** = kind of work (Activation, Modification, Assurance, VAS).
   - **Installation Method** = site condition (Prelaid, Non-Prelaid, SDU, RDF Pole).
   - Both are used together on orders and rates; they are not the same.
3. **Optional consistency:** On rate screens, you could standardize the second dimension to either “Installation Method” everywhere or “Site Condition” everywhere (with optional “(Installation Method)” where helpful). That is a separate, low-risk terminology pass and does not involve “Job Type” or Order Type.
4. **API comment:** The comment in `frontend/src/api/buildingDefaultMaterials.ts` (“per building + job type”) is internal. If desired, change it to “per building + order type” for consistency; not required for user-facing clarity.

---

## 10. Summary table

| Question | Answer |
|----------|--------|
| Are “Job Type” and “Installation Method” the same concept? | **No.** Job Type (in UI) = Order Type. Installation Method = site condition. They are separate dimensions. |
| Safe to replace “Job Type” with “Installation Method” in some UI? | **No.** It would conflate two concepts and conflict with the existing Installation Method field. |
| Recommended action | **C. Do not replace at all.** Keep “Order Type” for OrderTypeId and “Installation Method” / “Site Condition” for InstallationMethodId. |
| Places that can be renamed to “Installation Method” | **None** (no UI currently uses “Job Type” for the Installation Method concept). |
| Places that must not be renamed | All screens/controls that represent **Order Type** (see section 8). |
