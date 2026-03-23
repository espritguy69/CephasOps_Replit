# Order Creation Page Flow

**Date:** December 12, 2025  
**Purpose:** Visual representation of order creation page UI flow and conditional rendering based on order type

**Component:** `frontend/src/pages/orders/CreateOrderPage.tsx`  
**Route:** `/orders/create`

---

## Page Structure Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          ORDER CREATION PAGE                             │
└─────────────────────────────────────────────────────────────────────────┘
                              |
                              v
        ┌─────────────────────────────────────┐
        │     HEADER: [Cancel] [Save]         │
        └─────────────────────────────────────┘
                              |
                              v
        ┌─────────────────────────────────────┐
        │  SECTION 1: Status & SI Management  │
        │  [Status] [Service Installer]       │
        │  [Support] [Team/Crew]               │
        └─────────────────────────────────────┘
                              |
                              v
        ┌─────────────────────────────────────┐
        │  SECTION 2: Order Identification     │
        │  [Service ID/TBBN *]                 │
        │  [Order Type *] ────────────────────┐
        │  [Partner *]                        │ │
        │  [Appointment *]                    │ │
        │                                     │ │
        │  [IF ASSURANCE]                     │ │
        │  [Ticket # *] [AWO # *]            │ │
        └─────────────────────────────────────┘ │
                                                │
                    ┌───────────────────────────┘
                    │
        ┌───────────┴───────────┬───────────────┬──────────────┐
        |                       |               |              |
        v                       v               v              v
   ACTIVATION            MODIFICATION      ASSURANCE      VAS/OTHER
        |                       |               |              |
        |                       |               |              |
        v                       v               v              v
```

---

## Order Type: Activation

**All base sections visible, no additional fields**

```
✓ Status & SI Management
✓ Order Identification
  - Service ID/TBBN *
  - Order Type: "Activation" *
  - Partner *
  - Appointment *
✓ Customer & Building
  - Customer Info (Name, Contact, Address)
  - Building Info (Building, Type, Installation Method)
✓ Splitter Info (optional)
  - Splitter Number, Location, Port
✓ Customer Premise Materials
  - Auto-loaded from Building Default Materials
  - Serial numbers editable
✓ Network Info
  - Package, Bandwidth, Login ID, Password
  - WAN IP, LAN IP, Gateway, Subnet Mask
✓ VOIP (optional)
  - Service ID, Password, IP Addresses

NO ADDITIONAL SECTIONS
```

---

## Order Type: Modification

**Base sections + Modification Details section**

```
✓ Status & SI Management
✓ Order Identification
  - Service ID/TBBN *
  - Order Type: "Modification Indoor" OR "Modification Outdoor" *
  - Partner *
  - Appointment *
✓ Customer & Building
✓ Splitter Info
✓ Materials
✓ Network Info
✓ VOIP

+ ┌─────────────────────────────────────────────────────────────────────┐
  │ MODIFICATION DETAILS SECTION (NEW)                                    │
  │                                                                       │
  │ IF "Modification Outdoor":                                           │
  │   ┌─────────────────────┬─────────────────────┐                    │
  │   │ Old Address *       │ New Address *       │                    │
  │   │ (textarea)         │ (textarea)          │                    │
  │   └─────────────────────┴─────────────────────┘                    │
  │                                                                       │
  │ IF "Modification Indoor":                                            │
  │   ┌─────────────────────────────────────────────┐                  │
  │   │ Indoor Relocation Remark *                   │                  │
  │   │ (textarea)                                   │                  │
  │   └─────────────────────────────────────────────┘                  │
  └─────────────────────────────────────────────────────────────────────┘
```

---

## Order Type: Assurance

**Base sections + Assurance fields + RMA section**

```
✓ Status & SI Management
✓ Order Identification
  - Service ID/TBBN *
  - Order Type: "Assurance" *
  - Partner *
  - Appointment *
  + Ticket Number (TKT #) * ──> REQUIRED
  + AWO Number * ──> REQUIRED
✓ Customer & Building
✓ Splitter Info
✓ Materials
✓ Network Info
✓ VOIP

+ ┌─────────────────────────────────────────────────────────────────────┐
  │ RMA SECTION (NEW) - Return Material Authorization                    │
  │                                                                       │
  │ ┌───────────────────────────────────────────────────────────────┐  │
  │ │ RMA - Serialised Replacements                                  │  │
  │ │                                                                 │  │
  │ │ Row 1:                                                          │  │
  │ │ ┌──────────────┬──────────────┬──────────┬──────────┐         │  │
  │ │ │ Old Material │ New Material │ Quantity │ Remark   │ [X]    │  │
  │ │ └──────────────┴──────────────┴──────────┴──────────┘         │  │
  │ │                                                                 │  │
  │ │ [+ Add RMA] button                                             │  │
  │ └───────────────────────────────────────────────────────────────┘  │
  │                                                                       │
  │ ┌───────────────────────────────────────────────────────────────┐  │
  │ │ RMA - Non-Serialised Replacements                              │  │
  │ │ (Similar structure for non-serialized materials)               │  │
  │ └───────────────────────────────────────────────────────────────┘  │
  └─────────────────────────────────────────────────────────────────────┘
```

---

## Order Type: Value Added Service (VAS)

**Base sections only (same as Activation)**

```
✓ Status & SI Management
✓ Order Identification
✓ Customer & Building
✓ Splitter Info
✓ Materials
✓ Network Info
✓ VOIP

NO ADDITIONAL SECTIONS (Same as Activation)
```

---

## Auto-Detection & Auto-Fill Flow

### Service ID Auto-Detection

```
[User types Service ID]
         |
         v
[System detects pattern]
         |
    ┌────┴────┬──────────────┬──────────────┬──────────────┐
    |         |              |              |              |
    v         v              v              v              v
[TBBN*] [CELCOM*] [DIGI*] [UMOBILE*] [Other]
   |         |              |              |              |
   |         |              |              |              |
   v         v              v              v              v
[TIME] [Celcom] [Digi] [U Mobile] [Manual]
   |         |              |              |              |
   └─────────┴──────────────┴──────────────┴──────────────┘
         |
         v
[Auto-fills Partner dropdown]
[Auto-sets Service ID Type]
```

### Building Auto-Fill

```
[User selects Building]
         |
         v
[System loads Building details]
         |
         v
[Auto-fills:]
  - Building Type
  - Installation Method
  - Address (if empty)
         |
         v
[Triggers: Load Default Materials]
         |
         v
[API: getBuildingDefaultMaterials(buildingId, orderTypeId)]
         |
         v
[Materials section populated]
  - Materials marked as "default"
  - Serial numbers empty (user fills)
```

---

## Conditional Rendering Logic

```typescript
FORM STATE:
  orderType = watch('orderType')
  isAssurance = isAssuranceOrder(orderType)
  isModification = isModificationOrder(orderType)
  isOutdoor = isOutdoorModification(orderType)
  isIndoor = isIndoorModification(orderType)

RENDERING RULES:

1. ASSURANCE FIELDS (in Order Identification section):
   {isAssurance && (
     <div>
       <input name="ticketNumber" required />
       <input name="awoNumber" required />
     </div>
   )}

2. MODIFICATION SECTION:
   {isModification && (
     <div>
       {isOutdoor && (
         <input name="oldAddress" required />
         <input name="newAddress" required />
       )}
       {isIndoor && (
         <textarea name="indoorRemark" required />
       )}
     </div>
   )}

3. RMA SECTION:
   {isAssurance && (
     <div>
       {/* RMA Serialised Replacements */}
       {/* RMA Non-Serialised Replacements */}
     </div>
   )}
```

---

## Validation Rules Summary

**Common (All Order Types):**
- Service ID/TBBN: Required
- Order Type: Required
- Partner: Required
- Appointment: Required
- Customer Name: Required
- Contact No 1: Required
- Address: Required
- Building: Required

**Activation:**
- Common fields only

**Modification Outdoor:**
- Common fields
- Old Address: Required
- New Address: Required

**Modification Indoor:**
- Common fields
- Indoor Remark: Required

**Assurance:**
- Common fields
- Ticket Number: Required
- AWO Number: Required
- RMA rows (if added):
  - Old Material: Required
  - New Material: Required
  - Quantity: Required

**VAS:**
- Common fields only

---

## Form Submission Flow

```
[User clicks "Save"]
         |
         v
[Form Validation]
         |
    ┌────┴────┐
    |         |
    v         v
[VALID]  [INVALID]
   |         |
   |         |
   v         |
[Build Payload]
   |         |
   |         |
   v         |
[POST /api/orders]
   |         |
   |         |
   v         |
[Order Created]
   |         |
   |         |
   v         |
[Success Message]
   |         |
   |         |
   v         |
[Redirect to /orders]
```

---

## Visual Comparison Table

| SECTION                    | ACTIVATION | MODIFICATION | ASSURANCE | VAS |
|---------------------------|------------|--------------|-----------|-----|
| Status & SI Management     |     ✓      |      ✓       |     ✓     |  ✓ |
| Order Identification       |     ✓      |      ✓       |     ✓     |  ✓ |
|   - Ticket # / AWO #       |            |              |     ✓     |    |
| Customer & Building        |     ✓      |      ✓       |     ✓     |  ✓ |
| Modification Details       |            |      ✓       |           |    |
| Splitter Info              |     ✓      |      ✓       |     ✓     |  ✓ |
| Materials                  |     ✓      |      ✓       |     ✓     |  ✓ |
| RMA Section                |            |              |     ✓     |    |
| Network Info               |     ✓      |      ✓       |     ✓     |  ✓ |
| VOIP                       |     ✓      |      ✓       |     ✓     |  ✓ |

---

## Key Takeaways

1. **BASE FORM**: Always shows 9 core sections
2. **ORDER TYPE SELECTION**: Triggers conditional sections
3. **ASSURANCE**: Adds Ticket #, AWO #, and RMA section
4. **MODIFICATION**: Adds Modification Details section
   - Outdoor: Old Address + New Address
   - Indoor: Indoor Remark
5. **AUTO-DETECTION**: Service ID → Partner, Building → Materials
6. **VALIDATION**: Type-specific required fields
7. **SUBMISSION**: Payload varies by order type

---

**Last Updated:** December 12, 2025  
**Component:** `frontend/src/pages/orders/CreateOrderPage.tsx`

