# SI Operational Intelligence — Data Inventory

**Purpose:** Document data sources used for the Service Installer (SI) operational intelligence layer and what is fully vs partially supported. This is a reporting/analytics layer only; no enforcement.

**Endpoint:** `GET /api/admin/operations/si-insights?companyId={guid}&windowDays=90` (company-scoped; optional `companyId` for SuperAdmin).

---

## 1. Data sources available

| Source | Content | Used for |
|--------|---------|----------|
| **OrderStatusLog** | OrderId, FromStatus, ToStatus, TransitionReason, TriggeredBySiId, TriggeredByUserId, Source, CreatedAt | Completion time (Assigned→OrderCompleted), reschedule reasons, blocker reasons, transition churn |
| **Order** | Status, AssignedSiId, BuildingId, BuildingName, OrderTypeId, Issue, RescheduleCount, CreatedAt, UpdatedAt | Stuck orders, assurance filter, building hotspots |
| **OrderMaterialReplacement** | OrderId, ReplacementReason, ReplacedBySiId, RecordedAt | Replacement patterns, by-installer replacement counts |
| **OrderType** | Id, Code (e.g. ASSURANCE) | Filter assurance orders |
| **ServiceInstaller** | Id, Name, CompanyId | Display names for SI breakdowns |
| **OrderMaterialUsage** | OrderId, MaterialId, RecordedBySiId | Not yet used in insights; available for future material-usage trends |

---

## 2. Insights computed today

| Insight | Support | Notes |
|---------|---------|------|
| **Average time Assigned → OrderCompleted** | Full | Uses last Assigned log before first OrderCompleted per order; reschedules can shorten apparent duration. |
| **Average by installer** | Full | Uses TriggeredBySiId at completion; SI display name from ServiceInstaller.Name. |
| **Orders stuck in status** | Full | Orders in active status (Pending, Assigned, OnTheWay, MetCustomer, Blocker, ReschedulePendingApproval) with UpdatedAt older than threshold (default 7 days). |
| **Top reschedule reasons** | Full | OrderStatusLog where ToStatus=ReschedulePendingApproval; group by TransitionReason. |
| **Top blocker reasons** | Full | OrderStatusLog where ToStatus=Blocker; group by TransitionReason. |
| **Orders with high transition churn** | Full | Orders with ≥5 status log entries in window; reschedule and blocker counts. |
| **Top replacement reasons** | Full | OrderMaterialReplacement; group by ReplacementReason. |
| **Replacements by installer** | Full | OrderMaterialReplacement.ReplacedBySiId. |
| **Orders with multiple replacements** | Full | Count of orders with >1 OrderMaterialReplacement in window. |
| **Assurance completed in window** | Full | Orders with OrderType.Code=ASSURANCE that have OrderCompleted in window. |
| **Assurance with replacement** | Full | Those assurance orders that have at least one OrderMaterialReplacement. |
| **Top assurance issues** | Full | Order.Issue for assurance orders (parser/email-derived). |
| **Buildings with most disruptions** | Full | Reschedule + blocker log entries joined to Order.BuildingId/BuildingName; top N by total count. |
| **Building Reliability Score** | Full | Per-building band (Low/Moderate/High Risk) and contributing factors: reschedule count, blocker count, high-churn order count, stuck order count, assurance-with-replacement count, orders-with-replacements count. Same building set as Operational Hotspots. For prioritization only; not for automated enforcement. See [Building Reliability Score](#building-reliability-score) below. |

---

## Building Reliability Score

**Purpose:** Help operators identify buildings that repeatedly cause service installer disruption. Lightweight, explainable band (Low / Moderate / High Risk) with contributing factors.

**What it uses (same window as SI insights):**
- Reschedule and blocker event counts (from OrderStatusLog → Order.BuildingId)
- High-churn order count (orders with ≥5 transitions at that building)
- Stuck order count (orders currently stuck at that building)
- Assurance-with-replacement count (ASSURANCE orders completed in window with at least one material replacement at that building)
- Orders-with-replacements count (orders at that building with at least one OrderMaterialReplacement in window)

**Scoring (in code; reviewable):**
- **High Risk:** (reschedule + blocker) ≥ 10, OR stuck ≥ 2, OR high-churn orders ≥ 3
- **Moderate Risk:** (reschedule + blocker) ≥ 4, OR stuck ≥ 1, OR high-churn ≥ 1, OR assurance-with-replacement ≥ 2, OR orders-with-replacements ≥ 3
- **Low Risk:** otherwise (building has some disruption but below moderate thresholds)

**What it does not use:** ML, trend engines, project type, area code, scheduled vs actual timing. No new tables or background jobs.

**Limitations:** Same as Operational Hotspots (BuildingId/BuildingName only; area/project type not consistently available). Score is for review and prioritization only; data may be partial.

---

## Order Failure Pattern Detection

**Purpose:** Surface recurring operational/technical patterns behind failed, disrupted, or unstable orders. For visibility and prioritization only; not for blame or automated punishment.

**Pattern catalog (explainable rules in code):**

| Pattern | Description | Strength |
|---------|-------------|----------|
| Blocker + reschedule on same order | Order had at least one Blocker and one ReschedulePendingApproval in window | Strong Signal |
| Replacement-heavy assurance orders | Assurance orders with 2+ material replacements in window | Strong Signal |
| High-churn orders concentrated in building | Buildings with 2+ high-churn orders (≥5 transitions) | Strong Signal |
| Orders stuck after multiple transition attempts | Orders that appear in both stuck list and high-churn list | Strong Signal |
| Repeated replacement-heavy activity at same building | Buildings with 3+ orders that had at least one replacement in window | Strong Signal |
| Same assurance issue across multiple orders | Order.Issue value appearing on 2+ assurance orders (from TopAssuranceIssues) | Strong Signal |
| Disruption concentrated by order type | Order type with most reschedule/blocker events in window (volume may drive count) | Review Needed |
| Disruption concentrated by installer | Installers with ≥5 reschedule/blocker events in window (TriggeredBySiId). Association only. | Review Needed |
| Replacement activity concentrated by installer | Installers with ≥3 replacements in window (from MaterialReplacementPatterns.ByInstaller). | Review Needed |

**Classification:** Strong Signal = high-confidence rule; Review Needed = heuristic or volume-driven (including installer-concentration; installer may be reporter not cause); Partial Coverage = where data is incomplete (not yet used).

**What it does not use:** ML, trend engines, or causal inference. Patterns are for operational review only and do not imply cause.

---

## Pattern cluster detection

**Purpose:** Detect buildings where multiple operational signals align (e.g. high reliability risk + high-churn orders + replacement-heavy orders) so operators can prioritise potential root-cause clusters.

**Rules:** A cluster is created only when a building has **at least two** of: high-churn orders present, replacement-heavy orders present, stuck orders present (in addition to being in the building reliability list). No cluster for a single signal.

- **Possible Infrastructure Issue:** Building has High building reliability risk and at least two of the above signals.
- **Operational Cluster:** Building has Moderate building reliability risk and at least two of the above signals.

Each cluster includes: buildingId, buildingName, signals present, sample order IDs (from churn/replacement at that building), interpretation text, classification, limitations. Clusters do not prove root cause.

---

## 3. Partially supported / limitations

| Area | Limitation |
|------|------------|
| **Completion time** | Uses last Assigned before first OrderCompleted; reschedules can make duration appear shorter. No distinction by area/project type beyond what BuildingId provides. |
| **Area / project type** | No dedicated area or project-type dimension in the current model; hotspots use BuildingId and BuildingName only. Coverage note is included in the API response. |
| **Repeat visits** | Not directly modelled. Approximated by transition churn (many status changes) and Order.RescheduleCount; no explicit "same customer/building repeat completion" metric. |
| **Material usage (non-replacement)** | OrderMaterialUsage is available but not yet aggregated in si-insights; replacement-only for now. |

---

## 4. Not available / data gaps

- **Project type** or **area code** as a first-class dimension (unless encoded in BuildingName or other free text).
- **Scheduled slot vs actual** timing (ScheduledSlots exist but are not yet used in this report).
- **RMA/replacement approval workflow** counts (approval state is on OrderMaterialReplacement; not aggregated).
- **Quality scoring** or **ML-based** insights (explicitly out of scope; truthful about missing data instead).

---

## 5. API response and machine-readable output

The endpoint returns **SiOperationalInsightsDto** (JSON): GeneratedAtUtc, CompanyId, WindowDays, DataQualityNote, CompletionPerformance, RescheduleBlockerPatterns, MaterialReplacementPatterns, AssuranceRework, OperationalHotspots, **BuildingReliability** (Buildings, InterpretationNote), **OrderFailurePatterns** (Patterns, InterpretationNote), **PatternClusters** (Clusters, InterpretationNote), DataGaps. All lists are capped (e.g. top 10 reasons, top 20 installers, top 20 buildings, max 50 stuck orders, pattern sample IDs up to 5, cluster sample orders up to 5) to keep the payload small.

---

## 6. Related

- [PLATFORM_SAFETY_HARDENING_INDEX.md](PLATFORM_SAFETY_HARDENING_INDEX.md) — Platform guards (no change to SI intelligence).
- [SI_APP_WORKFLOW_HARDENING_REPORT.md](SI_APP_WORKFLOW_HARDENING_REPORT.md) — Workflow and reschedule reason enforcement (source of TransitionReason).
- Control plane: `GET /api/admin/control-plane` lists "SI operational insights" under operations.
