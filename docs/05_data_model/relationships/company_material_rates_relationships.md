# COMPANY_MATERIAL_RATES_RELATIONSHIPS.md
## Company Material Rates – Relationships

This document shows how `CompanyMaterialRate` relates to:

- Company
- MaterialTemplate (global)
- RateProfile
- Billing / Payout / Inventory usage

---

## 1. High-Level Diagram

```text
        +-----------------+
        |    Company      |
        +--------+--------+
                 |
                 | 1 - N
                 |
        +--------v-----------------+
        |     RateProfile          |
        +--------+-----------------+
                 |
                 | 1 - N (by Code)
                 |
        +--------v--------------------------+
        |      CompanyMaterialRate         |
        +--------+-------------------------+
                 |
                 | N - 1
                 |
        +--------v-----------------+
        |    MaterialTemplate      |
        +--------------------------+

2. Company → CompanyMaterialRate

Relationship:

Company 1 - N CompanyMaterialRate

Rules:

Every CompanyMaterialRate must belong to one Company.

Different companies can have different prices/costs for the same MaterialTemplate.

Examples:

Company A (Cephas):

ONT001 @ RM350 client / RM280 cost

Company B (Menorah):

ONT001 @ RM380 client / RM290 cost

3. MaterialTemplate → CompanyMaterialRate

Relationship:

MaterialTemplate 1 - N CompanyMaterialRate

Rules:

A global material can be rated differently per company and per rate profile.

If a MaterialTemplate is deactivated:

Rates may remain for history but should not be used for new usage.

4. Company → RateProfile → CompanyMaterialRate

If RateProfile is implemented as entity:

Company 1 - N RateProfile

RateProfile 1 - N CompanyMaterialRate (via RateProfileCode + CompanyId)

Typical logic:

A Company has:

DEFAULT profile.

Additional profiles e.g. TIME_FTTH, TIME_ASSURANCE.

Each CompanyMaterialRate references:

CompanyId

RateProfileCode (which matches RateProfile.Code for that company)

In some designs, CompanyMaterialRate also has RateProfileId.
For simplicity and easier imports, using RateProfileCode is acceptable
as long as (CompanyId, RateProfileCode) is unique.

5. Relationships with Billing

Billing module uses CompanyMaterialRate when generating invoice lines.

Example:

InvoiceLine

CompanyId

MaterialTemplateId

Quantity

RateProfileCode (optional; from order or project)

UnitPrice (resolved)

LineAmount (UnitPrice * Quantity)

Relationship:

InvoiceLine depends on CompanyMaterialRate at creation time but usually stores UnitPrice as a snapshot (no FK).

Sequence:

Billing requests rate:

(companyId, materialTemplateId, rateProfileCode?)

CompanyMaterialRate returned.

Billing writes UnitPrice / LineAmount into InvoiceLine.

6. Relationships with Installer Payout

Payout module uses CompanyMaterialRate for InstallerPayout.

Example:

InstallerPayoutItem

CompanyId

MaterialTemplateId

Quantity

RateProfileCode

PayoutRate (snapshot from CompanyMaterialRate.InstallerPayout)

PayoutAmount

Relationship:

Similar to Billing:

Payout module calls the rate resolution service.

Stores actual payout values in its own table.

7. Relationships with Inventory / Material Usage

Material usage table (for projects/orders) might look like:

MaterialUsage

Id

CompanyId

OrderId / ProjectId

MaterialTemplateId

Quantity

RateProfileCode (optional)

ResolvedClientPrice (optional)

ResolvedInternalCost (optional)

Relationships:

Company 1 - N MaterialUsage

MaterialTemplate 1 - N MaterialUsage

MaterialUsage uses CompanyMaterialRate for price/cost, then snapshots.

8. Resolution Priority

When resolving a rate for billing/payout:

Try CompanyMaterialRate where:

CompanyId = X

MaterialTemplateId = Y

RateProfileCode = requestedProfile

If not found, try:

RateProfileCode = DEFAULT

If still not found:

Use global defaults + MaterialTemplate

OR fail (depending on GlobalSetting MaterialRateStrictMode).

This resolution logic is implemented in the service layer, not enforced
by the relational model itself.

9. Deletion & History

CompanyMaterialRate.IsActive = false should be used instead of hard delete.

Invoice / payout / usage tables store snapshot values so that historical
reports are not affected by future rate changes.