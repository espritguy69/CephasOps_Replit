\### `inventory\_relationships.md`



```md

\# Inventory \& RMA – Relationships  

CephasOps Data Model – Inventory Relationships  

Version 1.0



This document describes relationships inside the \*\*Inventory \& RMA\*\* domain and how they connect to Orders.



---



\## 1. High-Level Overview



Key relationships:



\- Company 1—\* Materials

\- Company 1—\* StockLocations

\- (Company, Location, Material) 1—1 StockBalances

\- Material 1—\* SerialisedItems

\- Material 1—\* StockMovements

\- StockLocation 1—\* StockMovements

\- SerialisedItem 1—\* StockMovements

\- Order 1—\* OrderMaterialUsage → \* StockMovements

\- RmaTicket 1—\* RmaItems → SerialisedItems



---



\## 2. ERD (Inventory Scope)



```mermaid

erDiagram

&nbsp;   Company ||--o{ Material : defines

&nbsp;   Company ||--o{ StockLocation : has

&nbsp;   Material ||--o{ SerialisedItem : includes

&nbsp;   StockLocation ||--o{ StockMovement : source\_or\_dest

&nbsp;   Material ||--o{ StockMovement : moved\_as

&nbsp;   SerialisedItem ||--o{ StockMovement : tracked\_by

&nbsp;   Company ||--o{ StockBalance : caches

&nbsp;   StockLocation ||--o{ StockBalance : at

&nbsp;   Material ||--o{ StockBalance : for

&nbsp;   RmaTicket ||--o{ RmaItem : contains

&nbsp;   SerialisedItem ||--o{ RmaItem : may\_be

3\. Material Relationships

Material.id connects to:



StockBalances.materialId



StockMovements.materialId



SerialisedItems.materialId



OrderMaterialUsage.materialId



Implication:



Material is the reference for costing and categorisation.



4\. StockLocation \& StockBalance

StockLocation.id → StockMovements.fromLocationId / toLocationId



(companyId, locationId, materialId) uniquely identifies StockBalances.



Flow:



StockMovements are the source of truth.



StockBalances are derived \& cached for fast lookups.



5\. SerialisedItem Lifecycle

SerialisedItems.materialId → Material



SerialisedItems.currentLocationId → StockLocation



SerialisedItems.currentOrderId → Orders (if installed)



Movement path:



Warehouse → SI Bag → Installed (customer) → (optional) RMA → Scrap or Replaced.



Each step is a StockMovement row.



6\. StockMovement Bridges

StockMovements link:



Material → Location → SerialisedItem → Order → RmaTicket.



Key FKs:



StockMovements.materialId → Materials



StockMovements.serialisedItemId → SerialisedItems



StockMovements.relatedOrderId → Orders



StockMovements.relatedRmaTicketId → RmaTickets



These are critical for:



COGS calculations



SI bag reconciliation



Faulty device tracking



7\. RMA Relationships

RmaTickets.id → RmaItems.rmaTicketId



RmaItems.serialisedItemId → SerialisedItems



RmaItems.relatedOrderId → Orders (where fault was found)



RMA flow:



Fault identified at order site → RmaItem created.



SerialisedItem status set to Faulty or RMA.



StockMovement to RMA location recorded.



Ticket resolved as Approved / Rejected / Replaced.



8\. Order → Inventory

Order.id → OrderMaterialUsage.orderId



OrderMaterialUsage.materialId → Materials



OrderMaterialUsage.serialisedItemId → SerialisedItems



OrderMaterialUsage.stockMovementId → StockMovements



This chain allows:



Tracking which materials were consumed for which jobs.



Feeding job-level COGS into P\&L.



End of Inventory \& RMA Relationships

