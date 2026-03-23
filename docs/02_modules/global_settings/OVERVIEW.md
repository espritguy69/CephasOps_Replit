\# COMPANY\_MATERIAL\_RATES\_MODULE.md

\## Company Material Rates Module – Functional Specification



This module defines how each \*\*Company\*\* in CephasOps attaches its own

\*\*prices, costs, and rules\*\* to the \*\*global Material Templates\*\*.



\- Global: `MaterialTemplate` = WHAT the item is (code, name, unit, type).

\- Company: `CompanyMaterialRate` = HOW the company values/uses the item

&nbsp; (client price, internal cost, installer payout, markup, etc.).



---



\## 1. Purpose



Different companies (or legal entities) can:



\- Buy materials at different costs,

\- Sell materials at different prices,

\- Pay installers different rates for the same item.



The \*\*Company Material Rates Module\*\* provides:



\- Per-company pricing for each material template,

\- Rules for when/how materials are billable or payout-eligible,

\- Integration into Billing, P\&L, and Installer payout logic.



---



\## 2. Scope



\### Included



\- Maintain company-specific material rates for each `MaterialTemplate`

\- Per-company overrides of:

&nbsp; - Client billing price

&nbsp; - Internal cost

&nbsp; - Installer payout

&nbsp; - Active/Inactive flags

&nbsp; - Min/max quantities / default quantities

\- Rate profiles / groups per company (optional)

\- API and service layer used by:

&nbsp; - Billing \& invoicing

&nbsp; - P\&L / cost reports

&nbsp; - Installer payout calculations

&nbsp; - Material usage screens



\### Not Included



\- Global material definitions (belongs to Material Template module)

\- Inventory quantities / stock levels (belongs to Inventory/Materials module)

\- UI layout (belongs in `07\_frontend/ui`)



---



\## 3. Key Concepts



\### 3.1 Global Material vs Company Rate



\- `MaterialTemplate` (Global)

&nbsp; - e.g. `ONT001` = ONT Modem, `FIBER10M` = 10m Fiber Patchcord



\- `CompanyMaterialRate` (Company-specific)

&nbsp; - e.g. For Cephas:

&nbsp;   - ONT001:

&nbsp;     - Client price: RM 350

&nbsp;     - Company cost: RM 280

&nbsp;     - Installer payout: RM 40

&nbsp; - For Menorah:

&nbsp;   - ONT001:

&nbsp;     - Client price: RM 380

&nbsp;     - Company cost: RM 290

&nbsp;     - Installer payout: RM 50



\### 3.2 Rate Profiles (Optional)



A company can define different \*\*rate profiles\*\*, e.g.:



\- `DEFAULT`

\- `TIME\_FTTH`

\- `TIME\_ASSURANCE`

\- `ECO\_SHOP\_PROJECTS`

\- `KINGS\_MAN\_BARBERSHOP`



Each profile can adjust the price/cost/payout for a material, even within

the same company.



---



\## 4. Responsibilities



\### 4.1 CRUD for Company Material Rates



\- Create/Update/Delete `CompanyMaterialRate` for a given `(companyId, materialTemplateId, rateProfileCode)`.

\- Deactivate (soft delete) rates without losing history.

\- Ensure a \*\*default\*\* rate profile exists per company.



\### 4.2 Rate Resolution



Given:



\- `companyId`

\- `materialTemplateId` (or material code)

\- optional `rateProfileCode` (e.g. from Order, Partner, or Project)



Resolve:



\- Client billing price

\- Internal cost

\- Installer payout

\- Tax flag (taxable / non-taxable)

\- Markup



Resolution order:



1\. Exact match: `(companyId, materialTemplateId, rateProfileCode)`

2\. Fallback: `(companyId, materialTemplateId, DEFAULT)`

3\. If still not found:

&nbsp;  - Use global default from `GlobalSetting` (e.g. default markup) OR

&nbsp;  - Throw/flag as configuration error based on strictness setting.



\### 4.3 Validation Rules



\- A company cannot have duplicate active rates for the same `(materialTemplateId, rateProfileCode)`.

\- Negative prices are not allowed.

\- Payout must be ≤ client price in most cases (configurable).



\### 4.4 Integration with Other Modules



\- \*\*Billing Module\*\*

&nbsp; - When creating invoice lines from material usage:

&nbsp;   - Use CompanyMaterialRate to compute billable amount.

\- \*\*P\&L / Finance\*\*

&nbsp; - Cost and margin calculations use:

&nbsp;   - Internal cost vs billed price.

&nbsp; - Cost centre allocation uses:

&nbsp;   - `DepartmentMaterialCostCenter` (primary)

&nbsp;   - `CompanyMaterialRate.DefaultCostCenterCode` (fallback)

&nbsp;   - `GlobalSetting.DefaultCostCenterCode` (final fallback)

\- \*\*Installer Payout\*\*

&nbsp; - Payout per material is fetched from company rate.

\- \*\*Inventory / Materials\*\*

&nbsp; - Material usage screens show:

&nbsp;   - Company-specific value (cost/price) per line.



---



\## 5. Backend Services



\### 5.1 CompanyMaterialRateService



Responsibilities:



\- CRUD operations

\- Rate resolution

\- Default profile enforcement

\- Validation



Example methods:



```csharp

Task<CompanyMaterialRateDto?> GetRateAsync(

&nbsp;   Guid companyId,

&nbsp;   Guid materialTemplateId,

&nbsp;   string? rateProfileCode,

&nbsp;   CancellationToken cancellationToken = default);



Task UpsertRateAsync(

&nbsp;   CompanyMaterialRateUpsertCommand command,

&nbsp;   CancellationToken cancellationToken = default);



Task<List<CompanyMaterialRateDto>> GetRatesForCompanyAsync(

&nbsp;   Guid companyId,

&nbsp;   string? rateProfileCode,

&nbsp;   CancellationToken cancellationToken = default);

```

5.2 RateProfileService (Optional / if implemented)



Manages named rate profiles:



DEFAULT, TIME\_FTTH, TIME\_ASSURANCE, etc.



Ensures DEFAULT exists per company.



6\. Global Settings Integration



GlobalSettings provide defaults and guardrails.



Recommended keys:



MaterialRateStrictMode (Bool)



If true:



Throw when no rate is found for a material used in billing/payout.



If false:



Allow zero/placeholder pricing but log warning.



DefaultMaterialMarginPercent (Decimal)



Used if company doesn’t define client price for a material but defines cost.



DefaultRateProfileCode (String)



Fallback profile code if none is attached to an order.



Resolution logic:



Try company rate with order’s rateProfileCode.



Fallback to DEFAULT profile.



If still missing:



Use cost + global DefaultMaterialMarginPercent or



Throw based on MaterialRateStrictMode.



7\. API Endpoints



Base path:



/api/companies/{companyId}/material-rates



7.1 List Rates

GET /api/companies/{companyId}/material-rates?rateProfileCode=TIME\_FTTH





Returns:



Material template basic info



Company rates (price, cost, payout)



Active flag



7.2 Get Single Rate

GET /api/companies/{companyId}/material-rates/{materialTemplateId}?rateProfileCode=DEFAULT



7.3 Upsert Rate

PUT /api/companies/{companyId}/material-rates/{materialTemplateId}

Content-Type: application/json



{

&nbsp; "rateProfileCode": "TIME\_FTTH",

&nbsp; "clientPrice": 350.00,

&nbsp; "internalCost": 280.00,

&nbsp; "installerPayout": 40.00,

&nbsp; "taxable": true,

&nbsp; "active": true

}



7.4 Bulk Import / Export (Optional)



CSV/Excel upload of company material rates.



Export for finance review.



8\. Billing \& Payout Usage Examples

8.1 Billing Line Creation



When generating an invoice line:



Order has companyId, rateProfileCode.



Material usage line has materialTemplateId and quantity.



Call CompanyMaterialRateService.GetRateAsync.



Compute:



lineAmount = rate.clientPrice \* quantity



If no rate found and MaterialRateStrictMode = true, stop and flag configuration error.



8.2 Installer Payout



When computing payout for a job:



Loop all material usage lines for that job.



For each line, fetch:



installerPayout from CompanyMaterialRate.



Sum payouts per installer / per job.



9\. Error Handling \& Logging



Missing rate:



Logged with companyId, materialTemplateId, rateProfileCode.



Conflicting definitions:



When more than one active rate for the same (materialTemplateId, rateProfileCode) is found:



Logged and the operation should fail in strict mode.



10\. Dependencies

Depends On



Material Template Module



To know the universe of materials.



Company Module



For scoping.



Global Settings Module



Strict vs relaxed behaviour, default profiles, margin rules.



Billing Module / Payout Module



For use of rates.



11\. Non-Goals



The Company Material Rates Module does not:



Manage stock levels or serial numbers (Inventory module).



Define global material types/templates.



Handle tax/reporting logic in detail (belongs to Finance/Tax modules).



Its purpose is:



To provide a clean, company-scoped pricing and costing layer built on top of global material templates, powering Billing, P\&L, and Installer payouts.



