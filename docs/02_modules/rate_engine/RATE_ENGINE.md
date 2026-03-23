✅ RATE\_ENGINE.md — Universal Rate Engine (Production Specification)


Applies to all Departments, Verticals, Companies



1\. Purpose



The Rate Engine is the central pricing \& payout brain of CephasOps.



It manages all revenue and payout logic for:



GPON Department (Activation/Modification/Assurance)



NWO Department (Fibre pull, chambers, manholes, ducts)



CWO Department (Enterprise jobs)



Barbershop (Cut, shave, colour, commission)



Travel \& Tours (Packages, pax)



Spa (Treatments)



HQ (internal costing)



It guarantees:



✔ No hard-coded rates

✔ Future-proof, no backend changes when pricing changes

✔ Multi-department, multi-company, multi-vertical

✔ Correct revenue → P\&L → SI/Staff/Subcon payouts

✔ Flexible enough for any rate structure including % commission



2\. Core Concepts



The Rate Engine is built on 3 layers:



Revenue Rate

→ What Cephas earns (from TIME, customers, partners, etc.)



Payout Rate

→ What Cephas pays staff or subcontractors (PU \& commissions)



Custom Override Rate

→ Special rates for special installers/stylists or special projects



Every final rate is resolved using this priority:



1️⃣ Custom Rate (if exists)

2️⃣ Payout Rate (default)

3️⃣ Revenue Rate (for P\&L margin and revenue calculation)



3\. Rate Engine Data Model

3.1 RateCard (Header)



Defines the context and type of rate.



RateCard

--------

id

companyId

verticalId            // ISP, BARBERSHOP, TRAVEL, SPA, HQ

departmentId          // GPON, NWO, CWO, BARBER\_OPS, TRAVEL\_OPS, etc.

rateContext           // GPON\_JOB, NWO\_SCOPE, BARBER\_SERVICE, TRAVEL\_PACKAGE, etc.

rateKind              // REVENUE, PAYOUT, BONUS, COMMISSION

name                  // "GPON Revenue v1", "Barber Payout 2025"

description

validFrom

validTo

isActive



3.2 RateCardLine (Detail Items)



Each row defines one rate, keyed by up to 4 flexible dimensions.



RateCardLine

------------

id

rateCardId

dimension1

dimension2

dimension3

dimension4

rateAmount

unitOfMeasure        // JOB, METER, SERVICE, PAX, SESSION, DEVICE, etc.

currency             // MYR (default)

payoutType           // 'RM' or '%' (commission model)

extraJson            // optional metadata

isActive





Dimensions are flexible.



Examples:



GPON → OrderType + InstallationType + InstallationMethod



NWO → ScopeType + Complexity



Barbershop → ServiceCode + BarberLevel



Travel → PackageCode + Season



The Rate Engine does NOT assume meaning — department modules define them.



3.3 CustomRate (Per Staff / Per Subcon Override)

CustomRate

-----------

id

userId               // employee / subcon / barber / agent

departmentId

verticalId

dimension1

dimension2

dimension3

dimension4

customRateAmount

unitOfMeasure

currency

validFrom

validTo

isActive



4\. Rate Resolution Logic (Critical)



The Rate Engine returns the correct rate using 4-step resolution:



4.1 Step 1 — Resolve applicable RateCards

rateCards = RateCard

&nbsp;   .where(companyId = order.companyId)

&nbsp;   .where(departmentId = order.departmentId)

&nbsp;   .where(verticalId = order.verticalId)

&nbsp;   .where(context matches order context)

&nbsp;   .where(isActive = true)



4.2 Step 2 — Resolve CustomRate (highest priority)

customRate = CustomRate.find(

&nbsp;   userId = assigneeId,

&nbsp;   departmentId = order.departmentId,

&nbsp;   dimension1 = order.dimension1,

&nbsp;   dimension2 = order.dimension2,

&nbsp;   dimension3 = order.dimension3,

&nbsp;   dimension4 = order.dimension4

)





If found:



return customRateAmount



4.3 Step 3 — Resolve Payout Rate

payoutRate = RateCardLine for rateKind = PAYOUT





Found:



return payoutRate.rateAmount



4.4 Step 4 — Resolve Revenue Rate (fallback)

return revenueRate.rateAmount



5\. Department-Level Implementations



(This shows how each department should use the Rate Engine)



5.1 GPON Department (ISP Vertical)



Rates defined by:



Dimension	Meaning

dimension1	OrderType (ACTIVATION, ASSURANCE, etc.)

dimension2	InstallationType (FTTH, FTTO, FTTR, FTTC)

dimension3	InstallationMethod (PRELAID, NON\_PRELAID, SDU, etc.)

dimension4	PartnerGroup (TIME, CELCOM\_DIGI, etc.) (optional)



Units:



JOB (always 1 per job)



Supports:



Employee vs Subcon levels



Junior / Senior mapping



Custom SI rates



Already implemented in GPON\_RATECARDS.md.



5.2 NWO (Network Work Orders)



Rates based on scope, not job.



Dimension	Meaning

dimension1	ScopeType (FIBRE\_PULL, CHAMBER, MANHOLE, JOINT, DUCT)

dimension2	Complexity (NORMAL, HARD, NIGHT, SPECIAL)

dimension3	PartnerGroup

dimension4	Region



Units:



METER



UNIT (chamber, manhole, joint closure)



JOB (full infra)



Example:



scopeType	complexity	Unit	Revenue	Payout

FIBRE\_PULL	NORMAL	METER	7.00	5.00

FIBRE\_PULL	HARD	METER	10.00	7.50

CHAMBER	NORMAL	UNIT	800.00	600.00

5.3 CWO (Customer Work Orders – Enterprise)



Enterprise job billing.



Dimension	Meaning

dimension1	EnterpriseScope (CORE\_PULL, RACK\_SETUP)

dimension2	Difficulty

dimension3	FloorCount or CabinetCount

dimension4	PartnerGroup



Unit:

JOB, UNIT, METER, etc.



5.4 Barbershop (Kingsman Classic Services)



Rates for haircut, shave, colour, perm, etc.



Dimension	Meaning

dimension1	ServiceCode (CUT, CUT\_SHAVE, COLOUR)

dimension2	BarberLevel (JUNIOR, SENIOR, MASTER)

dimension3	BranchId

dimension4	DayType (WEEKDAY / WEEKEND)



Units:



SERVICE



Revenue = retail price

Payout = RM or commission %



Commission example:



Service	Barber Level	PayoutType	Value

CUT	JUNIOR	%	35

CUT	SENIOR	%	40

COLOUR	SENIOR	RM	50



Engine logic supports both.



5.5 Travel \& Tours



Rates for:



Package price per pax



Commission per agent



Seasonal adjustments



Dimension	Meaning

dimension1	PackageCode

dimension2	Season (PEAK / OFF\_PEAK)

dimension3	RoomType (TWIN, SINGLE)

dimension4	AgentLevel



Units:



PAX



Revenue: mark-up or commission

Payout: agent commission (RM or %)



5.6 Spa \& Wellness



Rates for:



Massage



Facial



Hair spa



Packages



Dimension	Meaning

dimension1	TreatmentCode

dimension2	TherapistLevel

dimension3	Branch

dimension4	Duration



Units: SESSION, HOUR



6\. Payroll Integration



Payroll uses:



Revenue (for P\&L)



Payout (for SI/barber/agent earnings)



Custom rates (per staff)



Quantity from Jobs or Services



UnitOfMeasure to multiply correctly



Example GPON:



payout = siRate \* 1 job





Barbershop:



payout = commission(%) \* retailPrice





NWO fibre pull:



payout = ratePerMeter \* metersPulled





Spa:



payout = RM per session \* #sessions





Travel:



payout = commission per pax \* pax



7\. Admin UI Rules (Settings → Rate Engine)



Admin should be able to:



Create RateCards by department



Configure RateCardLines in grid format



Filter by:



Vertical



Department



Partner Group



Service / Job Type



Level (SI / Barber / Agent)



Upload CSV for bulk update



Export full rate structure



Edit effective dates



Disable or override old rates



8\. Change Management



When partner changes pricing:



Admin updates Revenue RateCard



SI payout unchanged unless business decides



No code changes required



When adjusting subcon/employee payout:



Update Payout RateCard



Optional CustomRate for specific people



When launching new services/jobs:



Add dimensions



Add RateCardLines



No backend code needed



9\. API Contracts (High-Level)

Resolve rate (Generic)

GET /api/rates/resolve

{

&nbsp; "companyId": "...",

&nbsp; "verticalId": "...",

&nbsp; "departmentId": "...",

&nbsp; "dimension1": "...",

&nbsp; "dimension2": "...",

&nbsp; "dimension3": "...",

&nbsp; "dimension4": "...",

&nbsp; "userId": "...",

&nbsp; "roleType": "EMPLOYEE/SUBCON/AGENT"

}



Response:

{

&nbsp; "revenueRate": 330,

&nbsp; "payoutRate": 100,

&nbsp; "customRate": null,

&nbsp; "resolvedPayout": 100,

&nbsp; "unitOfMeasure": "JOB"

}



10\. This Rate Engine Replaces Hard-Coding Forever



From now on:



GPON Activation



NWO Fibre Pull



CWO Enterprise Job



Barber haircut commissions



Travel pax commission



Spa session payout



All use the same engine, different dimensions.



Your system becomes:



✔ Future-proof

✔ Multi-company

✔ Multi-vertical

✔ No developer needed for rate changes

✔ No mistakes in P\&L or payroll



End of RATE\_ENGINE.md (Production)

