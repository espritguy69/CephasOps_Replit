# SPLITTER_ENTITIES.md

Splitter Entities store **building-specific technical data** for fibre distribution:
- Building details
- Splitter layouts
- Port mapping

UI screens (e.g. Building Profile, Floor View) only read from these entities.

---

## 1. Core Entities

### 1.1 Building

- `Building`
  - `id`
  - `companyId`
  - `code` (e.g. `UNITED_POINT_TOWER_A`)
  - `name`
  - `addressLine1..country`
  - `lat` / `lng` (optional)
  - `buildingType` (`CONDO`, `OFFICE`, `RETAIL`, etc.)
  - `numBlocks` (optional)
  - `numFloors` (optional)
  - `defaultSplitterTemplateId` (FK → SplitterTemplate)
  - `isActive`
  - `createdAt`
  - `updatedAt`

### 1.2 SplitterTemplate

High-level pattern for splitter planning.

- `SplitterTemplate`
  - `id`
  - `companyId`
  - `code` (e.g. `STD_32_PORT_RISER`)
  - `displayName`
  - `description`
  - `numPorts`
  - `maxDropsPerPort`
  - `isDefault` (for company or building profile)
  - `createdAt`
  - `updatedAt`

### 1.3 BuildingSplitter

Actual splitter instance in a specific building.

- `BuildingSplitter`
  - `id`
  - `buildingId`
  - `templateId` (FK → SplitterTemplate)
  - `label` (e.g. `Riser Splitter 1`, `GF Cabinet A`)
  - `locationDescription` (e.g. `Block A, Level B1 Riser`)
  - `numPorts` (override allowed)
  - `commissionedAt` (optional)

### 1.4 BuildingSplitterPort

Ports in a specific splitter.

- `BuildingSplitterPort`
  - `id`
  - `buildingSplitterId`
  - `portNo` (1..N)
  - `status` (`FREE`, `USED`, `RESERVED`, `FAULTY`)
  - `serviceId` (optional – linked service)
  - `remarks`
  - `lastUpdatedAt`

---

## 2. Relations to Orders / UI

When creating/updating an Order:

- The system uses:
  - `companyId` → `Building` list
  - `buildingId` → `BuildingSplitter` list
  - `buildingSplitterId` → `BuildingSplitterPort` list

The installer / planner UI:

- Only **reads** available buildings + splitters + ports.
- Cannot invent new technical data on the fly.
- Any new building / splitter must be created via the **Settings / Network Planning** screens, which write to `Building`, `SplitterTemplate`, `BuildingSplitter`, `BuildingSplitterPort`.

This ensures:
- All network details are **centralised**
- No hardcoded building logic in UI or parser
