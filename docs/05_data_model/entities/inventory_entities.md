\# Inventory \& RMA Entities  

CephasOps – Inventory Domain Data Model  

Version 1.0



This file defines the core entities for the \*\*Inventory \& RMA\*\* domain:



\- Material

\- StockLocation

\- StockBalance

\- SerialisedItem

\- StockMovement

\- RmaRequest (code entity name; some docs may reference as RmaTicket)

\- RmaRequestItem (code entity name; some docs may reference as RmaItem)



All entities are \*\*company-scoped\*\* via `companyId`.



---



\## 1. Material



Represents a catalogue item (ONU, router, cable, trunking, etc.).



\### 1.1 Table: `Materials`



| Field           | Type     | Required | Description                                               |

|-----------------|----------|----------|-----------------------------------------------------------|

| id              | uuid     | yes      | Primary key.                                              |

| companyId       | uuid     | yes      | FK → Companies.id.                                        |

| code            | string   | yes      | Internal material code (unique within company).           |

| name            | string   | yes      | Human-readable name.                                      |

| category        | string   | yes      | E.g. `ONU`, `Router`, `Cable`, `Faceplate`, `Splitter`.   |

| uom             | string   | yes      | Unit of measure (`pcs`, `m`, etc.).                       |

| isSerialised    | boolean  | yes      | True if tracked per serial (ONU/router).                  |

| defaultCost     | decimal  | no       | Default unit cost (used when detailed GRN not available). |

| isActive        | boolean  | yes      | Soft enable/disable for future use.                       |

| notes           | text     | no       | Extra description.                                        |

| createdAt       | datetime | yes      | Created timestamp.                                        |

| createdByUserId | uuid     | yes      | FK → Users.id.                                            |

| updatedAt       | datetime | yes      | Last update timestamp.                                    |



\### 1.2 Indexes



\- Unique: `(companyId, code)`

\- Search: `(companyId, category, isActive)`



---



\## 2. StockLocation



Represents a physical or logical place where stock is held.



\### 2.1 Table: `StockLocations`



| Field        | Type    | Required | Description                                          |

|--------------|---------|----------|------------------------------------------------------|

| id           | uuid    | yes      | Primary key.                                         |

| companyId    | uuid    | yes      | FK → Companies.id.                                   |

| name         | string  | yes      | E.g. `Main Warehouse`, `SI Bag – Klavin`, `RMA Bin`. |

| code         | string  | no       | Short internal code.                                 |

| locationType | enum    | yes      | `Warehouse`, `SIBag`, `Customer`, `RMA`, `Transit`.  |

| ownerSiId    | uuid    | no       | FK → ServiceInstallers.id for SI bags.               |

| isActive     | boolean | yes      | Active/inactive.                                     |

| createdAt    | datetime| yes      | Created timestamp.                                   |



---



\## 3. StockBalance



Represents quantity on hand \*\*per material per location\*\* (non-serialised).



\### 3.1 Table: `StockBalances`



| Field         | Type    | Required | Description                                |

|---------------|---------|----------|--------------------------------------------|

| id            | uuid    | yes      | Primary key.                               |

| companyId     | uuid    | yes      | FK → Companies.id.                         |

| locationId    | uuid    | yes      | FK → StockLocations.id.                    |

| materialId    | uuid    | yes      | FK → Materials.id.                         |

| quantity      | decimal | yes      | On-hand quantity.                          |

| lastUpdatedAt | datetime| yes      | When balance was last recalculated.        |



> Balances are derived from `StockMovements` but may be cached here for performance.



---



\## 4. SerialisedItem



Tracks individual devices by serial number.



\### 4.1 Table: `SerialisedItems`



| Field             | Type     | Required | Description                                                          |

|-------------------|----------|----------|----------------------------------------------------------------------|

| id                | uuid     | yes      | Primary key.                                                         |

| companyId         | uuid     | yes      | FK → Companies.id.                                                   |

| materialId        | uuid     | yes      | FK → Materials.id.                                                   |

| serialNo          | string   | yes      | Manufacturer or partner serial number.                              |

| altSerialNo       | string   | no       | Secondary serial (MAC, barcode alternate, etc.).                     |

| status            | enum     | yes      | `InStock`, `Installed`, `Faulty`, `RMA`, `Scrapped`.                 |

| currentLocationId | uuid     | yes      | FK → StockLocations.id.                                              |

| currentOrderId    | uuid     | no       | FK → Orders.id (if installed at customer).                           |

| receivedAt        | datetime | no       | When first GRN created.                                              |

| defaultCost       | decimal  | no       | Cost for P\&L (copied from GRN or material).                          |

| notes             | text     | no       | Any remarks (e.g. MRA reference).                                    |

| createdAt         | datetime | yes      | Created timestamp.                                                   |

| createdByUserId   | uuid     | yes      | FK → Users.id.                                                       |



\### 4.2 Indexes



\- Unique: `(companyId, serialNo)`

\- Search: `(companyId, materialId, status)`



---



\## 5. StockMovement



Every movement of stock between locations.



\### 5.1 Table: `StockMovements`



| Field              | Type     | Required | Description                                                                 |

|--------------------|----------|----------|-----------------------------------------------------------------------------|

| id                 | uuid     | yes      | Primary key.                                                                |

| companyId          | uuid     | yes      | FK → Companies.id.                                                          |

| materialId         | uuid     | yes      | FK → Materials.id.                                                          |

| serialisedItemId   | uuid     | no       | FK → SerialisedItems.id (for tracked devices).                              |

| fromLocationId     | uuid     | no       | Source location; null for initial GRN.                                      |

| toLocationId       | uuid     | yes      | Destination location.                                                       |

| quantity           | decimal  | yes      | Quantity moved (1 for serialised).                                          |

| unitCost           | decimal  | no       | Cost used for P\&L tracking.                                                 |

| movementType       | enum     | yes      | `GRN`, `IssueToSI`, `ReturnFromSI`, `Install`, `ReturnToWarehouse`, `RMA`, `Scrap`. |

| relatedOrderId     | uuid     | no       | FK → Orders.id (for job-related movements).                                 |

| relatedRmaTicketId | uuid     | no       | FK → RmaTickets.id.                                                         |

| referenceNo        | string   | no       | Supplier invoice, DO number, etc.                                          |

| notes              | text     | no       | Additional info.                                                            |

| createdByUserId    | uuid     | yes      | FK → Users.id.                                                              |

| createdAt          | datetime | yes      | Created timestamp.                                                          |



---



\## 6. RmaTicket



Tracks the lifecycle of faulty equipment returns.



\### 6.1 Table: `RmaTickets`



| Field           | Type     | Required | Description                                            |

|-----------------|----------|----------|--------------------------------------------------------|

| id              | uuid     | yes      | Primary key.                                           |

| companyId       | uuid     | yes      | FK → Companies.id.                                     |

| partnerId       | uuid     | no       | FK → Partners.id (TIME, vendor, etc.).                |

| ticketNo        | string   | no       | Partner/vendor RMA number.                            |

| status          | enum     | yes      | `Open`, `SentToVendor`, `Approved`, `Rejected`, `Closed`. |

| openedByUserId  | uuid     | yes      | Who opened the ticket.                                |

| openedAt        | datetime | yes      | When created.                                         |

| closedAt        | datetime | no       | When ticket fully resolved.                           |

| notes           | text     | no       | General description and history notes.                |

| attachmentFileId| uuid     | no       | FK → Files.id (MRA PDF or email).                     |



---



\## 7. RmaItem



Links individual serialised items into an RMA ticket.



\### 7.1 Table: `RmaItems`



| Field             | Type     | Required | Description                                               |

|-------------------|----------|----------|-----------------------------------------------------------|

| id                | uuid     | yes      | Primary key.                                              |

| companyId         | uuid     | yes      | FK → Companies.id.                                        |

| rmaTicketId       | uuid     | yes      | FK → RmaTickets.id.                                       |

| serialisedItemId  | uuid     | yes      | FK → SerialisedItems.id.                                  |

| relatedOrderId    | uuid     | no       | FK → Orders.id where fault observed.                      |

| faultDescription  | text     | yes      | What was wrong (no power, LOS, etc.).                     |

| status            | enum     | yes      | `Pending`, `Accepted`, `Rejected`, `Replaced`.            |

| vendorResponse    | text     | no       | Response from vendor.                                     |

| createdAt         | datetime | yes      | When added.                                               |



---



\## 8. Cross-Module Links



\- `OrderMaterialUsage` → `StockMovement` → `SerialisedItem`  

\- `RmaItem` → `SerialisedItem` → `StockLocation (RMA)`  

\- `StockMovement` feeds \*\*COGS\*\* for P\&L.



See `relationships/inventory\_relationships.md` and `cross\_module\_relationships.md` for diagrams.



---



\# End of Inventory \& RMA Entities



