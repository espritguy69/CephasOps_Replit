\# Companies \& Partners Entities  

CephasOps – Company \& Partner Data Model  

Version 1.0



Defines:



\- Company

\- Partner

\- PartnerGroup

\- CostCentre



---



\## 1. Company



\### 1.1 Table: `Companies`



| Field           | Type     | Required | Description                       |

|-----------------|----------|----------|-----------------------------------|

| id              | uuid     | yes      | Primary key.                      |

| legalName       | string   | yes      | Official company name.            |

| shortName       | string   | yes      | E.g. `Cephas`, `CTrade`, `KSM`.   |

| registrationNo  | string   | no       | SSM number.                       |

| taxId           | string   | no       | SST/GST tax number.               |

| vertical        | string   | yes      | `ISP`, `Retail`, `Travel`, etc.   |

| address         | text     | no       | Registered address.               |

| phone           | string   | no       | Office number.                    |

| email           | string   | no       | Official contact.                 |

| isActive        | boolean  | yes      | Company enabled/disabled.         |

| createdAt       | datetime | yes      | Created timestamp.                |



---



\## 2. Partner



\### 2.1 Table: `Partners`



| Field          | Type     | Required | Description                                 |

|----------------|----------|----------|---------------------------------------------|

| id             | uuid     | yes      | Primary key.                                |

| companyId      | uuid     | yes      | FK → Companies.id (owner company).          |

| name           | string   | yes      | E.g. `TIME`, `Celcom`, `EcoShop Bahau`.     |

| partnerType    | enum     | yes      | `Telco`, `Customer`, `Vendor`, `Landlord`.  |

| groupId        | uuid     | no       | FK → PartnerGroups.id (e.g. TIME group).    |

| billingAddress | text     | no       | Default billing address.                    |

| contactName    | string   | no       | Main PIC.                                   |

| contactEmail   | string   | no       | Main email.                                 |

| contactPhone   | string   | no       | Main phone.                                 |

| isActive       | boolean  | yes      | Active/inactive.                            |

| createdAt      | datetime | yes      | Created timestamp.                          |



---



\## 3. PartnerGroup



Logical grouping (e.g. all TIME-related entities).



\### 3.1 Table: `PartnerGroups`



| Field     | Type     | Required | Description          |

|-----------|----------|----------|----------------------|

| id        | uuid     | yes      | Primary key.         |

| companyId | uuid     | yes      | FK → Companies.id.   |

| name      | string   | yes      | Group name.          |

| createdAt | datetime | yes      | Created timestamp.   |



---



\## 4. CostCentre



For overhead and P\&L segmentation.



\### 4.1 Table: `CostCentres`



| Field       | Type     | Required | Description                          |

|-------------|----------|----------|--------------------------------------|

| id          | uuid     | yes      | Primary key.                         |

| companyId   | uuid     | yes      | FK → Companies.id.                   |

| code        | string   | yes      | Short code (`ISP\_OPS`, `WAREHOUSE`). |

| name        | string   | yes      | Human-readable name.                 |

| description | string   | no       | Extended description.                |

| isActive    | boolean  | yes      | Active/inactive.                     |

| createdAt   | datetime | yes      | Created timestamp.                   |



---



\# End of Companies \& Partners Entities



