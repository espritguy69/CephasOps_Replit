✅ Understanding partnerGroup and partnerId in CephasOps

In CephasOps, PARTNERS are structured in two levels:

1. PARTNER GROUP (Top Level)

Represents the main principal or umbrella provider.

Example partner groups:

TIME
CELCOM_DIGI
U_MOBILE
MAXIS
YES
TM
THIRD_PARTY

Why partner groups exist:

They define billing rules, parser rules, ratecards, job types, etc.

They represent the main owner of the job (most important).

Many sub-brands belong to the same group.

Example:
CELCOM_DIGI covers both Celcom Fibre and Digi Fibre.

2. PARTNER (Child Level — Optional)

Represents specific channels or sub-brands under the group.

Example partners under TIME:

Direct TIME
TIME Retail
TIME VIP
TIME Corporate
TIME Reseller
TIME Business


Example partners under CELCOM_DIGI:

Celcom Fibre
Digi Fibre
Celcom Enterprise
Digi SME
Telemarketer Channel

Why partnerId exists:

To allow different rates, different invoice formats, different parser rules, or different operational rules for sub-brands.

Some companies have:

Different payout structures

Different invoice submission processes

Different SLA rules

Different Excel structures

3. How CephasOps Uses These Two IDs
Case A — The job only comes from TIME (Activation)

Only the partnerGroup matters.

partnerGroup = TIME
partnerId = null

Case B — The partner has multiple channels with different rates

You set:

partnerGroup = CELCOM_DIGI
partnerId = Celcom Fibre   // one of the child partners


This allows:

Different ratecards for Celcom Fibre vs Digi SME

Different email parser profiles

Different invoice templates

Different SI payout rules

Different building types

4. Where Partner ID is Used in the System
4.1 Rate Engine

Rate lookup uses:

rate = RateCardLine where
partnerGroupId = order.partnerGroupId
AND (partnerId = order.partnerId OR partnerId IS NULL)


Meaning:

If a specific rate exists for Celcom Fibre, system uses it

Else fallback to the group default (Celcom Digi)

4.2 Email Parser

Each partner or group may have:

Different subject line patterns

Different Excel header rows

Different TTKT formats

Different PDF structures

partnerId allows:

Multiple parser templates under one group

4.3 Billing & Invoice Templates

TIME invoices require special:

Submission ID

Template format

Portal upload steps

Other partners might use:

Different portals

Direct billing

Email submission

So invoice template selection =
partnerGroup + partnerId

4.4 GPON RateCards

GPON has different rates depending on:

Whether job is TIME direct

Or partner reseller job

Or Digi/Celcom (different pricing)

Each RateCardLine can include:

dimension4 = partnerGroup OR partnerId


Which means:

If partner-specific rate exists → use it

Otherwise fallback to group default

5. Recommended Structure (Production)
partners.json (seed data for Cursor)
{
  "partnerGroups": [
    { "id": "TIME", "name": "TIME dotCom" },
    { "id": "CELCOM_DIGI", "name": "Celcom Digi" },
    { "id": "U_MOBILE", "name": "U Mobile" },
    { "id": "MAXIS", "name": "Maxis" }
  ],
  "partners": [
    { "id": "TIME_DIRECT", "groupId": "TIME", "name": "TIME Direct" },
    { "id": "TIME_RETAIL", "groupId": "TIME", "name": "TIME Retail" },
    { "id": "CELCOM_FIBRE", "groupId": "CELCOM_DIGI", "name": "Celcom Fibre" },
    { "id": "DIGI_FIBRE", "groupId": "CELCOM_DIGI", "name": "Digi Fibre" }
  ]
}

6. How to Explain to Cursor (paste this in .cursor/rules)
In CephasOps, partner structure is two-level:

1. partnerGroup = main principal (e.g. TIME, CELCOM_DIGI)
2. partnerId = sub-brand or channel under the group (optional)

Rules:
- Every order must have partnerGroupId.
- partnerId is optional unless the group has multiple sub-channels.
- Rate Engine must resolve rates using partnerGroup first, then partnerId override.
- Parsers, invoice templates, ratecards, and workflows may differ by partnerId.
- If partnerId-specific rate exists, use it.
- If not, fallback to partnerGroup default.

7. Simple Example to Help You Visualise
Example Order
partnerGroup = CELCOM_DIGI
partnerId     = DIGI_FIBRE
orderType     = ACTIVATION
installationType = FTTH
installationMethod = PRELAID


Rate Engine will:

Try to find a DIGI_FIBRE-specific rate

If none, use CELCOM_DIGI default

If none, return system-wide default (if defined)

8. Summary (Use this version for communication)

partnerGroup = who owns the job (big umbrella)

partnerId = which channel the job came from (optional)

All RateCards, Parser Configs, Invoice Templates, Payouts can vary by either.

If partnerId exists, system applies channel-specific logic.

If not, system applies group-level logic.