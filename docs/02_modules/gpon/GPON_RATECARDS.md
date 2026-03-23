# GPON_RATECARDS.md — GPON Rate Cards & Payout Logic (Production)

This document defines the **rate card logic** for the **GPON Department** under the **ISP Vertical** in CephasOps.

It covers:

- GPON job **revenue** from main contractor / partners.
- GPON job **payouts** to Service Installers (SIs) — Employees and Subcons.
- How rates are keyed by **Order Type + Order Category + Installation Method** (“paid based on order category ID”).
- How to support **junior / senior / custom** SI levels without changing code.

---

## 1. Scope

This module applies to:

- **Vertical:** `ISP`
- **Department:** `GPON`
- **Order Types:** Activation, Modification, Assurance, Value Added Services (VAS) — starting with Activation & Assurance.
- **Order Categories:** FTTH, FTTO, FTTR, FTTC (service/technology categories).
- **Installation Methods:** Prelaid, Non-Prelaid, SDU, RDF Pole, FTTR packages, FTTC, Assurance variants.

It supports:

- Partner rate cards (what Cephas gets paid).
- SI rate cards (what Cephas pays SIs).
- SI custom overrides (special deals for specific SIs).

---

## 2. Master References

### 2.1 Order Types

Order Types are defined globally (Settings → Order Types) and reused across ISP.

| ID | Name                  | Code                  | Status |
|----|-----------------------|-----------------------|--------|
| 1  | Activation            | `ACTIVATION`          | Active |
| 2  | Modification Indoor   | `MODIFICATION_INDOOR` | Active |
| 3  | Modification Outdoor  | `MODIFICATION_OUTDOOR`| Active |
| 4  | Assurance             | `ASSURANCE`           | Active |
| 5  | Value Added Service   | `VALUE_ADDED_SERVICE` | Active |

> For GPON rate cards, the initial focus is **ACTIVATION** and **ASSURANCE**.

---

### 2.2 Order Categories

Order Categories represent the **service/technology category** (FTTH, FTTO, FTTR, FTTC).

Defined in Settings → Order Categories (previously known as Order Categorys):

| ID | Name       | Code        | Status |
|----|------------|------------|--------|
| 1  | FTTH       | `FTTH`     | Active |
| 2  | FTTO       | `FTTO`     | Active |
| 3  | FTTR       | `FTTR`     | Active |
| 4  | FTTC       | `FTTC`     | Active |
| 5  | SDU        | `SDU`      | Active |
| 6  | RDF Pole   | `RDF_POLE` | Active |

> Note: SDU & RDF_POLE appear as installation methods, not order categories. For rate cards, we always use **OrderType + OrderCategory + InstallationMethod**.

---

### 2.3 Installation Methods

Installation Methods define **how** the job is executed (site conditions like Prelaid, Non-Prelaid).  
These are maintained in Settings → Installation Methods.

Existing records (FTTH linked):

| # | Name         | Code         | Description                                                                                       | Status |
|---|--------------|-------------|-------------------------------|---------------------------------------------------------------------------------------------------|--------|
| 1 | Prelaid      | `PRELAID`   | Fibre already laid by building/management. Minimal materials.                                    | Active |
| 2 | Non-Prelaid  | `NON_PRELAID`| Non-prelaid building type.                                                                       | Active |
| 3 | SDU          | `SDU`       | Surface/pole building type.                                                                      | Active |
| 4 | RDF Pole     | `RDF_POLE`  | RDF pole building type.                                                                          | Active |

**To support your rate table fully**, additional methods are required (even if some are hidden in UI):

Recommended extra methods:

| Name                | Code                | Notes                             |
|---------------------|---------------------|-----------------------------------|
| Assurance Repull    | `ASSURANCE_REPULL`  | Assurance repull jobs             |
| SDU Shoplot         | `SDU_SHOPLOT`       | SDU shoplot variant               |
| FTTR 1+1            | `FTTR_1_1`          | FTTR package 1+1                  |
| FTTR 1+2            | `FTTR_1_2`          | FTTR package 1+2                  |
| FTTR 1+3            | `FTTR_1_3`          | FTTR package 1+3                  |
| FTTC Base           | `FTTC_BASE`         | Base FTTC job                     |
| Assurance Base      | `ASSURANCE_BASE`    | Generic assurance job (RM 80)     |

> These methods are crucial to let the system key rates cleanly by **Method** instead of hardcoding logic.

---

## 3. GPON Job Rate Concept

All GPON job rates are keyed by:

> **Order Type + Order Category + Installation Method**

This represents:
- **Order Type**: Activation, Modification, Assurance, etc.
- **Order Category**: FTTH, FTTO, FTTR, FTTC (service/technology category)
- **Installation Method**: Prelaid, Non-Prelaid, SDU, etc. (site condition)

In CephasOps, we formalise it as the **GPON Job Rate**.

---

## 4. GPON Job Rate Tables

There are **two core tables**:

1. **Partner GPON Job Rate** – what Cephas earns from TIME/partner (revenue).
2. **SI GPON Job Rate** – what Cephas pays to SI (payout).

Later, P&L will use the difference as **margin**.

---

### 4.1 Partner GPON Job Rate (Revenue)

**Table:** `GponPartnerJobRate`

Purpose: define **revenue per job** from TIME/partner using your rate list.

**Fields (minimum):**

- `id`
- `partnerGroupId` (e.g. TIME, CELCOM_DIGI)
- `partnerId` (optional, for specific channels; can be null)
- `departmentId` = `GPON`
- `orderTypeId` (e.g. `ACTIVATION`, `ASSURANCE`)
- `orderCategoryId` (`FTTH`, `FTTO`, `FTTR`, `FTTC`)
- `installationMethodId` (e.g. `PRELAID`, `NON_PRELAID`, `FTTR_1_1`)
- `rateAmount` (RM)
- `currency` (default `MYR`)
- `isActive`
- `validFrom`, `validTo` (optional)

---

### 4.2 SI GPON Job Rate (Default Payout)

**Table:** `GponSiJobRate`

Purpose: define **base payout per job** for SIs (Employees & Subcons) by level.

**Fields:**

- `id`
- `installerType` (`EMPLOYEE` / `SUBCON`)
- `siLevel` (`JUNIOR` / `SENIOR` / `SUPERVISOR` / etc.)
- `departmentId` = `GPON`
- `orderTypeId`
- `orderCategoryId`
- `installationMethodId`
- `defaultRateAmount` (RM)
- `currency` (MYR)
- `isActive`
- `validFrom`, `validTo` (optional)

> Business can decide:  
> - Initially, SI rate = same as partner rate, or  
> - Use a lower/higher rate for SIs while margin goes to company.

---

### 4.3 SI GPON Custom Rate (Overrides)

**Table:** `GponSiCustomRate`

Purpose: handle **special SI deals**, overriding default SI rates.

**Fields:**

- `id`
- `installerId` (SI user ID)
- `departmentId` = `GPON`
- `orderTypeId`
- `orderCategoryId`
- `installationMethodId`
- `customRateAmount` (RM)
- `currency`
- `validFrom`, `validTo`
- `isActive`

**Resolution order at payroll time:**

1. If **custom rate** exists → use `customRateAmount`.
2. Else → use `defaultRateAmount` from `GponSiJobRate`.

---

## 5. Base GPON Rate Cards (from your table)

This section encodes your current rate structure into the **Partner GPON Job Rate** format.

> These are **example rows**, assuming partnerGroupId = `TIME`.  
> For Celcom, Digi, etc. you can either reuse these or create new rows per partner.

---

### 5.1 Activation – FTTH

OrderType: `ACTIVATION`  
OrderCategory: `FTTH`

| OrderType   | Inst.Type | Inst.Method        | Method Code          | Rate (RM) |
|-------------|-----------|--------------------|----------------------|-----------|
| ACTIVATION  | FTTH      | Prelaid            | `PRELAID`            | 150.00    |
| ACTIVATION  | FTTH      | Non-Prelaid        | `NON_PRELAID`        | 330.00    |
| ACTIVATION  | FTTH      | SDU                | `SDU`                | 430.00    |
| ACTIVATION  | FTTH      | RDF Pole           | `RDF_POLE`           | 330.00    |
| ACTIVATION  | FTTH      | Assurance Repull   | `ASSURANCE_REPULL`   | 300.00    |

---

### 5.2 Activation – FTTO

OrderType: `ACTIVATION`  
InstallationType: `FTTO`

| OrderType   | Inst.Type | Inst.Method        | Method Code          | Rate (RM) |
|-------------|-----------|--------------------|----------------------|-----------|
| ACTIVATION  | FTTO      | Prelaid            | `PRELAID`            | 150.00    |
| ACTIVATION  | FTTO      | Non-Prelaid        | `NON_PRELAID`        | 500.00    |
| ACTIVATION  | FTTO      | SDU Shoplot        | `SDU_SHOPLOT`        | 500.00    |
| ACTIVATION  | FTTO      | RDF Pole           | `RDF_POLE`           | 330.00    |
| ACTIVATION  | FTTO      | Assurance Repull   | `ASSURANCE_REPULL`   | 500.00    |

---

### 5.3 Others (FTTR / FTTC / Assurance)

OrderType: `ACTIVATION` or `ASSURANCE` depending on job type.

| OrderType   | Inst.Type | Inst.Method  | Method Code     | Rate (RM) |
|-------------|-----------|--------------|-----------------|-----------|
| ACTIVATION  | FTTR      | FTTR – 1 + 1 | `FTTR_1_1`      | 390.00    |
| ACTIVATION  | FTTR      | FTTR – 1 + 2 | `FTTR_1_2`      | 530.00    |
| ACTIVATION  | FTTR      | FTTR – 1 + 3 | `FTTR_1_3`      | 670.00    |
| ACTIVATION  | FTTC      | FTTC         | `FTTC_BASE`     | 500.00    |
| ASSURANCE   | FTTH      | Assurance    | `ASSURANCE_BASE`| 80.00     |

---

## 6. How Orders Use These Rates

When an order is created/cleaned, it must have:

- `orderTypeId` (e.g. `ACTIVATION`, `ASSURANCE`)
- `orderCategoryId` (e.g. `FTTH`, `FTTO`, `FTTR`, `FTTC`)
- `installationMethodId` (e.g. `PRELAID`, `NON_PRELAID`, `FTTR_1_1`)

### 6.1 Standard Resolution

**Partner Revenue:**

```pseudo
partnerRate = GponPartnerJobRate.find(
  partnerGroupId,
  partnerId (optional),
  departmentId = GPON,
  orderTypeId,
  orderCategoryId,
  installationMethodId
)
SI Base Rate:

pseudo
Copy code
siRateDefault = GponSiJobRate.find(
  installerType,     // EMPLOYEE / SUBCON
  siLevel,           // JUNIOR / SENIOR / ...
  departmentId = GPON,
  orderTypeId,
  orderCategoryId,
  installationMethodId
)
SI Custom Override (if exists):

pseudo
Copy code
siRateCustom = GponSiCustomRate.find(
  installerId,
  departmentId = GPON,
  orderTypeId,
  orderCategoryId,
  installationMethodId
)

if siRateCustom exists:
    siRate = siRateCustom.customRateAmount
else:
    siRate = siRateDefault.defaultRateAmount
Margin for P&L:

pseudo
Copy code
margin = partnerRate.rateAmount - siRate
7. Employee vs Subcon Logic
In GponSiJobRate:

installerType = EMPLOYEE

installerType = SUBCON

This allows two layers:

Subcon Example
installerType	siLevel	orderType	instType	method	rate
SUBCON	JUNIOR	ACTIVATION	FTTH	PRELAID	80
SUBCON	SENIOR	ACTIVATION	FTTH	PRELAID	100

Employee Example
installerType	siLevel	orderType	instType	method	rate
EMPLOYEE	JUNIOR	ACTIVATION	FTTH	PRELAID	50
EMPLOYEE	SENIOR	ACTIVATION	FTTH	PRELAID	70

Custom SIs (e.g. special subcon):

text
Copy code
GponSiCustomRate:
installerId: MOHAN
orderType: ACTIVATION
instType: FTTH
method: SDU
customRateAmount: 200.00
8. Integration With Payroll & P&L
Payroll Module
At billing/payment time, the Payroll module reads:

Orders completed in GPON

Installer assignments

The resolved SI rate (default or custom).

Generates:

Per-SI monthly earnings

Breakdown by partner, job type, and order category.

P&L Module
Reads:

Partner revenue (GponPartnerJobRate) × number of jobs

SI cost (GponSiJobRate / GponSiCustomRate) × number of jobs

Calculates:

Gross margin by:

Partner

Order Type

Order Category / Method

SI Type (Employee/Subcon)

Department (GPON)

9. Admin UX Expectations
Settings → GPON → Rate Cards
Admin should be able to:

View a unified list of GPON rates.

Filter by:

Partner Group

Order Type

Order Category

Installation Method

Installer Type (for SI rate cards)

SI Level

Edit rates in a grid-style UI.

Add new rows when new job types appear.

Set effective dates for changes (validFrom/validTo).

10. Change Management
Any changes in rates from TIME/partner:

Update GponPartnerJobRate only.

Any changes in SI payouts:

Update GponSiJobRate or GponSiCustomRate.

No backend code change required as long as:

OrderType, OrderCategory, InstallationMethod are mapped.

End of GPON_RATECARDS.md